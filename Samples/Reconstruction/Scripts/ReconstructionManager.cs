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

public class ReconstructionManager : MonoBehaviour
{
    [Header("AR Settings")]
    [SerializeField] private ARCameraManager aRCameraManager;
    [SerializeField] private TMP_Text statusText;

    private string currentScene;
    private Coroutine captureCoroutine;
    private ICaptureDevice captureDevice;
    private IModelCollector modelCollector;

    void Start()
    {
        //InitReconstruction(clientRole, clientScene);
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
        Debug.Log($"Set new Collector of type: {collector.GetType()}");
        modelCollector = collector;
    }

    private void RegisterHost()
    {
        captureDevice = new SmartphoneCaptureDevice(aRCameraManager);
        SetCollector(new NeuralReconCollector());

        captureCoroutine = StartCoroutine(CaptureCoroutine());
        ReconstructionClient.Instance.Connect("host", currentScene);
    }

    private void RegisterVisitor()
    {
        ReconstructionClient.Instance.Connect("visitor", currentScene);
    }

    private IEnumerator CaptureCoroutine()
    {
        while (true)
        {
            uint frameIDX = (uint)Time.frameCount;

            if (modelCollector != null && modelCollector.IsNthFrame(frameIDX))
            {
                var intrinsics = captureDevice.GetIntrinsics();
                var extrinsics = captureDevice.GetExtrinsics();

                statusText.text = $"Frame: {frameIDX}\n" +
                                  $"Camera Position: {extrinsics.CameraPosition}\n" +
                                  $"Camera Rotation: {extrinsics.CameraRotation}\n" +
                                  $"Intrinsics: {intrinsics.FocalLength}";
                if (modelCollector.ShouldCollect(intrinsics, extrinsics))
                {
                    var frame = captureDevice.GetFrame();
                    if (frame.Dimensions != Vector2.zero)
                    {
                        modelCollector.Collect(intrinsics, extrinsics, frame);

                        if (modelCollector.IsFull())
                        {
                            var fragment = modelCollector.Consume(currentScene);
                            ReconstructionClient.Instance.EnqueueFragment(fragment);
                            Debug.Log($"Created fragment: {fragment}");
                        }
                    }
                }
            }
            yield return null; // or yield return new WaitForSeconds(0.1f);
        }
    } 
}
