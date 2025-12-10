using System.Collections;
using System.Collections.Generic;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.CaptureDevices.Smartphone;
using RTReconstruct.Collectors.Interfaces;
using RTReconstruct.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using System.Globalization;
using PassthroughCameraSamples;
using RTReconstruct.CaptureDevices.MetaQuest;
using System.IO;
using Newtonsoft.Json;
using RTReconstruct.Networking;
using Meta.XR;

[System.Serializable]
public class PoseData
{
    public Position position;
    public Rotation rotation;
}

[System.Serializable]
public class Position
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class Rotation
{
    public float x;
    public float y;
    public float z;
    public float w;
}

public enum DeviceType
{
    AR,
    AR_EVAL,
    VR,
    VR_EVAL
}

public class ReconstructionManager : MonoBehaviour
{
    [SerializeField] private DeviceType deviceType;
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private PassthroughCameraAccess cameraAccess;
    [SerializeField] private Camera captureCamera;
    [SerializeField] private TMP_Text deviceInfo;
    [SerializeField] private Toggle captureToggle;
    [SerializeField] private bool drawDeviceInfo = true;
    [SerializeField] private bool drawCameraFrustrum = true;
    [SerializeField] private bool useReplay = false;

    private ICaptureDevice captureDevice;
    private IModelCollector modelCollector;

    private CaptureDeviceIntrinsics latestIntrinsics;
    private CaptureDeviceExtrinsics latestExtrinsics;

    private bool isHost = false;
    private bool isCapturing = false;
    private string currentScene = "";
    private Coroutine captureRoutine;

    private List<PoseData> poses = new();

    void Start()
    {
        // Device setup
        switch (deviceType)
        {
            case DeviceType.AR:
                captureDevice = new SmartphoneCaptureDevice(arCameraManager);
                break;
            case DeviceType.AR_EVAL:
                captureDevice = new SmartphoneEvalCaptureDevice(arCameraManager, captureCamera);
                break;
            case DeviceType.VR:
                captureDevice = new MetaQuestCaptureDevice(cameraAccess);
                break;
            case DeviceType.VR_EVAL:
                captureDevice = new MetaQuestEvalCaptureDevice(captureCamera, cameraAccess);
                break;
        }

        captureToggle.onValueChanged.AddListener(SetCaptureState);
    }

    public void InitReconstruction(string role, string scene)
    {
        Debug.Assert(role != "");
        Debug.Assert(scene != "");

        currentScene = scene;

        switch (role)
        {
            case "host":
                RegisterHost();
                break;
            case "visitor":
                RegisterVisitor();
                break;
        }

        // Start capture coroutine
        if (captureRoutine != null)
            StopCoroutine(captureRoutine);
        captureRoutine = StartCoroutine(CaptureLoop());
    }

    public void SetCollector(IModelCollector collector)
    {
        modelCollector = collector;
        Debug.Log($"Set new Collector of type: {collector.GetType()}");

        if (useReplay)
        {
            isCapturing = true;
            string path = Path.Combine(Application.streamingAssetsPath, "poses.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                poses = JsonConvert.DeserializeObject<List<PoseData>>(json);
                Debug.Log($"Loaded {poses.Count} poses");
            }
        }
    }

    public void SetCaptureState(bool state)
    {
        isCapturing = state;
        modelCollector.Clear();
        Debug.Log($"Set new capture state: {state}");
    }

    public void TransformModel()
    {
        TransformFragment fragment = new TransformFragment(
            currentScene,
            GetVector3FromInputs("Position"),
            Quaternion.Euler(GetVector3FromInputs("Rotation")),
            GetVector3FromInputs("Scale")
        );
        ReconstructionClient.Instance.EnqueueFragment(fragment);
    }

    public void SyncModelToRoomAnchor()
    {
        var anchorGO = GameObject.Find("RoomAnchor");
        TransformFragment fragment = new TransformFragment(
            currentScene,
            anchorGO.transform.position,
            anchorGO.transform.rotation,
            Vector3.one
        );
        ReconstructionClient.Instance.EnqueueFragment(fragment);
    }

    private void RegisterHost()
    {
        isHost = true;
        ReconstructionClient.Instance.Connect("host", currentScene);
    }

    private void RegisterVisitor()
    {
        ReconstructionClient.Instance.Connect("visitor", currentScene);
    }

    private void DisplayDeviceInfo()
    {
        if (deviceInfo == null) return;

        string info = "";
        info += $"Focal Length: {latestIntrinsics.FocalLength}\n";
        info += $"Principal Point: {latestIntrinsics.PrincipalPoint}\n";
        info += $"Position: {latestExtrinsics.CameraPosition}\n";
        info += $"Rotation: {latestExtrinsics.CameraRotation.eulerAngles}\n";

        deviceInfo.text = info;
    }

    private Vector3 GetVector3FromInputs(string vName)
    {
        Transform parent = GameObject.Find(vName).transform;
        float.TryParse(parent.GetChild(1).gameObject.GetComponent<TMP_InputField>().text, NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
        float.TryParse(parent.GetChild(2).gameObject.GetComponent<TMP_InputField>().text, NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
        float.TryParse(parent.GetChild(3).gameObject.GetComponent<TMP_InputField>().text, NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
        return new Vector3(x, y, z);
    }

    // 🧠 NEW: fully synchronized capture coroutine
    private IEnumerator CaptureLoop()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame(); // sync with Unity's render timing

            // Fetch intrinsics every frame (in case of runtime changes)
            latestIntrinsics = captureDevice.GetIntrinsics();

            if (useReplay)
            {
                if (poses.Count == 0)
                    continue;

                var currentPose = poses[0];
                poses.RemoveAt(0);
                latestExtrinsics = new CaptureDeviceExtrinsics()
                {
                    CameraPosition = new Vector3(currentPose.position.x, currentPose.position.y, currentPose.position.z),
                    CameraRotation = new Quaternion(currentPose.rotation.x, currentPose.rotation.y, currentPose.rotation.z, currentPose.rotation.w)
                };
            }
            else
            {
                // ⚡ Captured *after* render, synchronized with current frame
                latestExtrinsics = captureDevice.GetExtrinsics();
            }

            if (drawDeviceInfo)
                DisplayDeviceInfo();

            if (!isHost || !isCapturing || modelCollector == null)
                continue;

            uint frameIDX = (uint)Time.frameCount;
            if (!modelCollector.IsNthFrame(frameIDX))
                continue;

            if (modelCollector.ShouldCollect(latestIntrinsics, latestExtrinsics))
            {
                var frame = captureDevice.GetFrame();
                if (frame.Dimensions == Vector2.zero)
                {
                    Debug.Log("Captured Frame has Dimensions of (0, 0)");
                    continue;
                }

                modelCollector.Collect(latestIntrinsics, latestExtrinsics, frame);

                if (drawCameraFrustrum)
                {
                    var frustrumMesh = MeshUtils.CreateCameraFrustumWireframe(latestExtrinsics.CameraPosition, latestExtrinsics.CameraRotation);
                    Destroy(frustrumMesh, 5f);
                }

                if (modelCollector.IsFull())
                {
                    var fragment = modelCollector.Consume(currentScene);
                    ReconstructionClient.Instance.EnqueueFragment(fragment);
                    Debug.Log($"Created fragment: {fragment}");
                }
            }
        }
    }
}
