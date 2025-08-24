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

public enum DeviceType
{
    AR,
    VR
}

public class ReconstructionManager : MonoBehaviour
{
    [SerializeField] private DeviceType deviceType;
    [Header("AR Settings")]
    [SerializeField] private ARCameraManager arCameraManager;
    [Header("VR Settings")]
    [SerializeField] private WebCamTextureManager webcamTextureManager;
    [SerializeField] private GameObject centerEyeAnchor;
    [Header("UI Settings")]
    [SerializeField] private TMP_Text deviceInfo;
    [SerializeField] private UnityEngine.UI.Toggle captureToggle;
    [Header("Debug Settings")]
    [SerializeField] private bool drawDeviceInfo = true;
    [SerializeField] private bool drawCameraFrustrum = true;

    private ICaptureDevice captureDevice;
    private IModelCollector modelCollector;

    private CaptureDeviceIntrinsics latestIntrinsics;
    private CaptureDeviceExtrinsics latestExtrinsics;

    private bool isHost = false;
    private bool isCapturing = false;
    private string currentScene = "";

    void Start()
    {
        switch (deviceType)
        {
            case DeviceType.AR:
                captureDevice = new SmartphoneCaptureDevice(arCameraManager);
                break;
            case DeviceType.VR:
                captureDevice = new MetaQuestCaptureDevice(webcamTextureManager);
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
        Debug.Assert(captureDevice != null, "captureDevice is null");
        Debug.Assert(modelCollector != null, "modelCollector is null");

        latestIntrinsics = captureDevice.GetIntrinsics();
        latestExtrinsics = captureDevice.GetExtrinsics();

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
