using System.Collections;
using System.Collections.Generic;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.CaptureDevices.Smartphone;
using RTReconstruct.Collectors.Interfaces;
using RTReconstruct.Collectors.NeuralRecon;
using RTReconstruct.Collectors.SLAM3R;
using RTReconstruct.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Assertions;
using RTReconstruct.Core.Models;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Globalization;
using PassthroughCameraSamples;
using RTReconstruct.CaptureDevices.MetaQuest;
using System.IO;
using Newtonsoft.Json;

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
    [SerializeField] private WebCamTextureManager webcamTextureManager;
    [SerializeField] private GameObject centerEyeAnchor;
    [SerializeField] private Camera captureCamera;
    [SerializeField] private TMP_Text deviceInfo;
    [SerializeField] private UnityEngine.UI.Toggle captureToggle;
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

    private List<PoseData> poses = new();

    void Start()
    {
        switch (deviceType)
        {
            case DeviceType.AR:
                captureDevice = new SmartphoneCaptureDevice(arCameraManager);
                break;
            case DeviceType.AR_EVAL:
                captureDevice = new SmartphoneEvalCaptureDevice(arCameraManager, captureCamera);
                break;
            case DeviceType.VR:
                captureDevice = new MetaQuestCaptureDevice(webcamTextureManager);
                break;
            case DeviceType.VR_EVAL:
                captureDevice = new MetaQuestEvalCaptureDevice(captureCamera, centerEyeAnchor);
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

    void LateUpdate()
    {
        latestIntrinsics = captureDevice.GetIntrinsics();
        if (useReplay)
        {
            if (poses.Count == 0)
                return;

            var currentPose = poses[0];
            poses.RemoveAt(0);
            var currentPosition = currentPose.position;
            var currentRotation = currentPose.rotation;
            latestExtrinsics = new CaptureDeviceExtrinsics()
            {
                CameraPosition = new Vector3(currentPosition.x, currentPosition.y, currentPosition.z),
                CameraRotation = new Quaternion(currentRotation.x, currentRotation.y, currentRotation.z, currentRotation.w)
            };

            captureCamera.transform.position = latestExtrinsics.CameraPosition;
            captureCamera.transform.rotation = latestExtrinsics.CameraRotation;
        }
        else
        {
            latestExtrinsics = captureDevice.GetExtrinsics();
        }

        if (drawDeviceInfo)
        {
            DisplayDeviceInfo();
        }

        if (!isHost || !isCapturing)
        {
            return;
        }

        uint frameIDX = (uint)Time.frameCount;
        if (!modelCollector.IsNthFrame(frameIDX))
        {
            return;
        }

        if (modelCollector.ShouldCollect(latestIntrinsics, latestExtrinsics))
        {
            var frame = captureDevice.GetFrame();
            if (frame.Dimensions == Vector2.zero)
            {
                Debug.Log("Captured Frame has Dimensions of (0, 0)");
                return;
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
