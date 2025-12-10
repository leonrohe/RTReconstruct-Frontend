using System;
using System.Threading.Tasks;
using UnityEngine;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.Core.Models;
using PassthroughCameraSamples;
using Oculus.Interaction.Samples;
using Unity.Mathematics;
using Meta.XR;
using Unity.Collections;

namespace RTReconstruct.CaptureDevices.MetaQuest
{
    public class MetaQuestCaptureDevice : ICaptureDevice
    {
        private readonly PassthroughCameraAccess m_cameraAccess;
        private Color32[] m_PixelBuffer;

        public MetaQuestCaptureDevice(PassthroughCameraAccess cameraAccess)
        {
            m_cameraAccess = cameraAccess;
        }

        public CaptureDeviceExtrinsics GetExtrinsics()
        {
            var headPose = m_cameraAccess.GetCameraPose();
            return new CaptureDeviceExtrinsics
            {
                CameraPosition = headPose.position,
                CameraRotation = headPose.rotation
            };
        }

        public CaptureDeviceFrame GetFrame()
        {
            var resolution = m_cameraAccess.CurrentResolution;
            NativeArray<Color32> pixels = m_cameraAccess.GetColors();
            NativeArray<byte> jpgData = ImageConversion.EncodeNativeArrayToJPG(
                pixels, 
                UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                (uint)resolution.x, 
                (uint)resolution.y,
                quality: 100
            );
            byte[] jpgBytes = jpgData.ToArray();
            jpgData.Dispose();
            
            return new CaptureDeviceFrame
            {
                Image = jpgBytes,
                Dimensions = resolution
            };         
        }

       
        public CaptureDeviceIntrinsics GetIntrinsics()
        {
            if(m_cameraAccess.IsPlaying)
            {
                var resolution = m_cameraAccess.CurrentResolution;
                PassthroughCameraAccess.CameraIntrinsics intrinsics = m_cameraAccess.Intrinsics;
                Vector2Int nativeRes = intrinsics.SensorResolution;

                // Scale focal length and principal point according to actual resolution
                float scaleX = (float)resolution.x / nativeRes.x;
                float scaleY = (float)resolution.y / nativeRes.y;

                Vector2 focalLengthScaled = new Vector2(
                    intrinsics.FocalLength.x * scaleX,
                    intrinsics.FocalLength.y * scaleY
                );

                Vector2 principalPointScaled = new Vector2(
                    intrinsics.PrincipalPoint.x * scaleX,
                    intrinsics.PrincipalPoint.y * scaleY
                );

                return new CaptureDeviceIntrinsics()
                {
                    FocalLength = focalLengthScaled,
                    PrincipalPoint = principalPointScaled
                };
            }
            else
            {
                throw new Exception("CameraAccess not playing!");
            }
        }
    }
}