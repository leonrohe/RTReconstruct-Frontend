using System.Collections;
using System.Collections.Generic;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.CaptureDevices.Smartphone;
using RTReconstruct.Collectors.Interfaces;
using RTReconstruct.Collectors.NeuralRecon;
using RTReconstruct.Networking;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ReconstructionManager : MonoBehaviour
{
    [SerializeField]
    private ARCameraManager aRCameraManager;

    private ICaptureDevice captureDevice;
    private IModelCollector modelCollector;

    void Start()
    {
        captureDevice = new SmartphoneCaptureDevice(aRCameraManager);
        modelCollector = new NeuralReconCollector();

        ReconstructionClient.Instance.Connect();
    }

    void Update()
    {
        uint frameIDX = (uint)Time.frameCount;

        if (!modelCollector.IsNthFrame(frameIDX))
        {
            return;
        }

        var intrinsics = captureDevice.GetIntrinsics();
        var extrinsics = captureDevice.GetExtrinsics();

        if (modelCollector.ShouldCollect(intrinsics, extrinsics))
        {
            Handheld.Vibrate();

            var frame = captureDevice.GetFrame();
            if (frame.Dimensions == Vector2.zero)
            {
                return;
            }

            modelCollector.Collect(intrinsics, extrinsics, frame);
            
            if (modelCollector.IsFull())
            {
                var fragment = modelCollector.Consume();
                ReconstructionClient.Instance.EnqueueFragment(fragment);
                Debug.Log($"Created fragment: {fragment}");
            }
        }
    }
}
