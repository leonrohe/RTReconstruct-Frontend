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
            m_WebCamTextureManager.RequestedResolution = new Vector2Int(640, 480);
        }

        public CaptureDeviceExtrinsics GetExtrinsics()
        {
            var camPose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
            return new CaptureDeviceExtrinsics
            {
                CameraPosition = camPose.position,
                CameraRotation = camPose.rotation
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
            var webcamTexture = m_WebCamTextureManager.WebCamTexture;
            if (webcamTexture == null || !webcamTexture.isPlaying)
                throw new Exception("WebCamTexture not correctly initialized");

            // Use the actual captured resolution
            int actualWidth = webcamTexture.width;
            int actualHeight = webcamTexture.height;

            // Get native intrinsics from the passthrough camera
            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left);

            // Native resolution the intrinsics correspond to
            Vector2Int nativeRes = intrinsics.Resolution;

            // Scale focal length and principal point according to actual resolution
            float scaleX = (float)actualWidth / nativeRes.x;
            float scaleY = (float)actualHeight / nativeRes.y;

            Vector2 focalLengthScaled = new Vector2(
                intrinsics.FocalLength.x * scaleX,
                intrinsics.FocalLength.y * scaleY
            );

            Vector2 principalPointScaled = new Vector2(
                intrinsics.PrincipalPoint.x * scaleX,
                intrinsics.PrincipalPoint.y * scaleY
            );

            return new CaptureDeviceIntrinsics
            {
                FocalLength = focalLengthScaled,
                PrincipalPoint = principalPointScaled
            };
        }
    }
}