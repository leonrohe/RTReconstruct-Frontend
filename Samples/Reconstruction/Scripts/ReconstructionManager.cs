using System.Collections;
using System.Collections.Generic;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.CaptureDevices.Smartphone;
using RTReconstruct.Collectors.Interfaces;
using RTReconstruct.Collectors.NeuralRecon;
using RTReconstruct.Collector.SLAM3R;
using RTReconstruct.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Assertions;
using RTReconstruct.Core.Models;
using UnityEngine.UI;

public class ReconstructionManager : MonoBehaviour
{
    [Header("AR Settings")]
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private TMP_Text deviceInfo;
    [SerializeField] private Toggle captureToggle;
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
        captureDevice = new SmartphoneCaptureDevice(arCameraManager);

        if (drawDeviceInfo)
        {
            arCameraManager.frameReceived += DisplayDeviceInfo;
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

    private void RegisterHost()
    {
        isHost = true;
        SetCollector(new NeuralReconCollector());
        ReconstructionClient.Instance.Connect("host", currentScene);
    }

    private void RegisterVisitor()
    {
        ReconstructionClient.Instance.Connect("visitor", currentScene);
    }

    private void DisplayDeviceInfo(ARCameraFrameEventArgs args)
    {
        string info = "";

        info += $"Focal Length: {latestIntrinsics.FocalLength}\n";
        info += $"Principal Point: {latestIntrinsics.PrincipalPoint}\n";

        info += $"Position: {latestExtrinsics.CameraPosition}\n";
        info += $"Rotation: {latestExtrinsics.CameraRotation}\n";

        deviceInfo.text = info;
    }

    void Update()
    {
        latestIntrinsics = captureDevice.GetIntrinsics();
        latestExtrinsics = captureDevice.GetExtrinsics();

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
