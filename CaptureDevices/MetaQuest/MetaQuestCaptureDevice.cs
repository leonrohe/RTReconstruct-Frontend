using System;
using System.Threading.Tasks;
using UnityEngine;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.Core.Models;
using PassthroughCameraSamples;
using Oculus.Interaction.Samples;

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
            m_PixelBuffer ??= new Color32[width*height];
            _ = m_WebCamTextureManager.WebCamTexture.GetPixels32(m_PixelBuffer);
            texture.SetPixels32(m_PixelBuffer);
            texture.Apply();

            return new CaptureDeviceFrame
            {
                Image = texture.EncodeToJPG(),
                Dimensions = new Vector2(width, height)
            };
        }

        public CaptureDeviceIntrinsics GetIntrinsics()
        {
            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left);
            return new CaptureDeviceIntrinsics
            {
                FocalLength = intrinsics.FocalLength,
                PrincipalPoint = intrinsics.PrincipalPoint
            };
        }
    }
}