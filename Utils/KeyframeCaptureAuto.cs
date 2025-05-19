using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class KeyframeCaptureAuto : MonoBehaviour
{
    public ARCameraManager cameraManager;
    public string outputFolder = "CapturedFrames";
    public float minAngleDeg = 15f;
    public float minDistance = 0.1f;

    private int frameCount = 0;
    private Matrix4x4? lastPose = null;

    void Start()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, outputFolder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    void Update()
    {
        // Get current camera pose
        Matrix4x4 currentPose = cameraManager.transform.localToWorldMatrix;

        if (!lastPose.HasValue || ShouldCapture(currentPose, lastPose.Value))
        {
            lastPose = currentPose;

            if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                StartCoroutine(ProcessImage(image, currentPose));
            }
        }
    }

    bool ShouldCapture(Matrix4x4 current, Matrix4x4 previous)
    {
        Vector3 posCurrent = current.GetColumn(3);
        Vector3 posPrevious = previous.GetColumn(3);
        float distance = Vector3.Distance(posCurrent, posPrevious);

        Quaternion rotCurrent = current.rotation;
        Quaternion rotPrevious = previous.rotation;
        float angle = Quaternion.Angle(rotCurrent, rotPrevious);

        return angle > minAngleDeg || distance > minDistance;
    }

    IEnumerator ProcessImage(XRCpuImage image, Matrix4x4 pose)
    {
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        var rawData = new NativeArray<byte>(image.GetConvertedDataSize(conversionParams), Allocator.Temp);
        image.Convert(conversionParams, rawData);
        image.Dispose();

        Texture2D texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(rawData);
        texture.Apply();
        rawData.Dispose();

        string folder = Path.Combine(Application.persistentDataPath, outputFolder);
        string filename = $"frame_{frameCount}.png";
        File.WriteAllBytes(Path.Combine(folder, filename), texture.EncodeToPNG());

        // Save pose
        string poseText = MatrixToString(pose);
        File.WriteAllText(Path.Combine(folder, $"pose_{frameCount}.txt"), poseText);

        Handheld.Vibrate();

        Debug.Log($"Auto-saved keyframe {frameCount} to {folder}");
        frameCount++;

        yield return null;
    }

    string MatrixToString(Matrix4x4 m)
    {
        return $"{m.m00} {m.m01} {m.m02} {m.m03}\n" +
               $"{m.m10} {m.m11} {m.m12} {m.m13}\n" +
               $"{m.m20} {m.m21} {m.m22} {m.m23}\n" +
               $"{m.m30} {m.m31} {m.m32} {m.m33}";
    }
}
