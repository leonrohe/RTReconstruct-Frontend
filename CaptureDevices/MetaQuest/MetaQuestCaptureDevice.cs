using System;
using System.Threading.Tasks;
using UnityEngine;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.Core.Models;
using PassthroughCameraSamples;
using Oculus.Interaction.Samples;
using Unity.Mathematics;

namespace RTReconstruct.CaptureDevices.MetaQuest
{
    public class MetaQuestCaptureDevice : ICaptureDevice
    {
        private readonly WebCamTextureManager m_WebCamTextureManager;
        private Color32[] m_PixelBuffer;

        public MetaQuestCaptureDevice(WebCamTextureManager wcTextManag)
        {
            m_WebCamTextureManager = wcTextManag;
        }

        public CaptureDeviceExtrinsics GetExtrinsics()
        {
            var headPose = OVRPlugin.GetNodePoseStateImmediate(OVRPlugin.Node.Head).Pose.ToOVRPose();
            return new CaptureDeviceExtrinsics
            {
                CameraPosition = headPose.position,
                CameraRotation = headPose.orientation
            };
        }

        public CaptureDeviceFrame GetFrame()
        {
            var webcamTexture = m_WebCamTextureManager.WebCamTexture;
            if (webcamTexture == null || !webcamTexture.isPlaying)
                throw new Exception("WebCamTexture not correctly initialized");
            int width = webcamTexture.width;
            int height = webcamTexture.height;

            var texture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
            m_PixelBuffer ??= new Color32[width * height];
            _ = m_WebCamTextureManager.WebCamTexture.GetPixels32(m_PixelBuffer);
            texture.SetPixels32(m_PixelBuffer);
            texture.Apply();

            byte[] jpgData = texture.EncodeToJPG(100);
            UnityEngine.Object.Destroy(texture);  // Clean up the texture

            return new CaptureDeviceFrame
            {
                Image = jpgData,
                Dimensions = new Vector2(width, height)
            };
        }

        public CaptureDeviceIntrinsics GetIntrinsics()
        {
            // Intrinsics correspond to the maximum resolution (e.g., 1280x960)
            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left);

            // Get resolutions
            var maxResolution = PassthroughCameraUtils.GetOutputSizes(PassthroughCameraEye.Left)[^1]; // last element
            var requestedResolution = m_WebCamTextureManager.RequestedResolution;

            // Compute scale between resolutions
            var scale = new Vector2(
                (float)requestedResolution.x / maxResolution.x,
                (float)requestedResolution.y / maxResolution.y
            );

            // Apply scaling: intrinsics scale with image resolution
            return new CaptureDeviceIntrinsics
            {
                FocalLength = Vector2.Scale(intrinsics.FocalLength, scale),
                PrincipalPoint = Vector2.Scale(intrinsics.PrincipalPoint, scale)
            };
        }
    }
}