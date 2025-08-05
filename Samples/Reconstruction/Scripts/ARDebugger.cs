using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARCameraInfoDisplay : MonoBehaviour
{
    public ARCameraManager cameraManager;
    public TMP_Text outputText; // Assign this in the Inspector

    void OnEnable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        string info = "";

        // Get intrinsics
        if (cameraManager.TryGetIntrinsics(out XRCameraIntrinsics intrinsics))
        {
            info += $"Focal Length: {intrinsics.focalLength}\n";
            info += $"Principal Point: {intrinsics.principalPoint}\n";
            info += $"Resolution: {intrinsics.resolution}\n";
        }
        else
        {
            info += "Intrinsics not available\n";
        }

        // Get transform
        var camTransform = Camera.main.transform;
        info += $"Position: {camTransform.position}\n";
        info += $"Rotation: {camTransform.rotation.eulerAngles}\n";

        if (outputText != null)
            outputText.text = info;
    }
}
