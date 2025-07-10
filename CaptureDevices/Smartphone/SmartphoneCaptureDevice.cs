using System.Collections;
using System.Collections.Generic;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.Core.Models;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RTReconstruct.CaptureDevices.Smartphone
{
    public class SmartphoneCaptureDevice : ICaptureDevice
    {
        readonly ARCameraManager m_cameraManager;
        private Matrix4x4 initialWorldToCameraMatrix;


        public SmartphoneCaptureDevice(ARCameraManager cameraManager)
        {
            m_cameraManager = cameraManager;
            initialWorldToCameraMatrix = m_cameraManager.transform.localToWorldMatrix.inverse;
        }

        public CaptureDeviceIntrinsics GetIntrinsics()
        {
            if (m_cameraManager.TryGetIntrinsics(out XRCameraIntrinsics intrinsics))
            {
                return new CaptureDeviceIntrinsics
                {
                    FocalLength = intrinsics.focalLength,
                    PrincipalPoint = intrinsics.principalPoint
                };
            }

            return default;
        }

        public CaptureDeviceExtrinsics GetExtrinsics()
        {
            Transform camTransform = m_cameraManager.transform;
            Matrix4x4 current = camTransform.localToWorldMatrix;

            Matrix4x4 relative = initialWorldToCameraMatrix * current;

            Vector3 position = relative.GetColumn(3); // Relative translation
            Quaternion rotation = Quaternion.LookRotation(
                relative.GetColumn(2),
                relative.GetColumn(1) 
            );

            return new CaptureDeviceExtrinsics
            {
                CameraPosition = position,
                CameraRotation = rotation
            };
        }


       public CaptureDeviceFrame GetFrame()
        {
            if (m_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                int width = image.width;
                int height = image.height;

                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, width, height),
                    outputDimensions = new Vector2Int(width, height),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.MirrorX
                };

                int size = image.GetConvertedDataSize(conversionParams);
                var buffer = new NativeArray<byte>(size, Allocator.Temp);

                image.Convert(conversionParams, buffer);

                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(buffer);
                texture.Apply();

                byte[] imageBytes = texture.EncodeToJPG();

                image.Dispose();
                buffer.Dispose();

                return new CaptureDeviceFrame
                {
                    Image = imageBytes,
                    Dimensions = new Vector2(width, height)
                };
            }

            return default;
        }
    }
}
