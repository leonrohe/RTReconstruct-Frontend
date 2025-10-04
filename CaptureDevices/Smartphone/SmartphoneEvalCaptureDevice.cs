using System;
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
    public class SmartphoneEvalCaptureDevice : ICaptureDevice
    {
        private Camera _camera;
        private RenderTexture _captureRT;
        private Texture2D _readbackTex;
        private int _width = 640;
        private int _height = 480;
        readonly ARCameraManager m_cameraManager;

        public SmartphoneEvalCaptureDevice(ARCameraManager cameraManager, Camera camera)
        {
            m_cameraManager = cameraManager;
            _camera = camera;
            InitCamera();
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

            Quaternion rotation = Quaternion.LookRotation(
                current.GetColumn(2),
                current.GetColumn(1)
            );
            Vector3 position = current.GetColumn(3);

            return new CaptureDeviceExtrinsics
            {
                CameraPosition = position,
                CameraRotation = rotation
            };
        }

        public CaptureDeviceFrame GetFrame()
        {
            if (_captureRT == null || _readbackTex == null)
                throw new Exception("Capture camera not initialized");

            // Set Camera Position and Rotation
            var extrinsics = GetExtrinsics();
            _camera.transform.position = extrinsics.CameraPosition;
            _camera.transform.rotation = extrinsics.CameraRotation;

            // Render into RT
            _camera.Render();

            // Read back into persistent Texture2D
            RenderTexture.active = _captureRT;
            _readbackTex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            _readbackTex.Apply();
            RenderTexture.active = null;

            // Encode
            byte[] jpgData = _readbackTex.EncodeToJPG(100);

            return new CaptureDeviceFrame
            {
                Image = jpgData,
                Dimensions = new Vector2(_width, _height)
            };

        }

        private void InitCamera()
        {
            // Create RenderTexture and readback Texture2D
            _captureRT = new RenderTexture(_width, _height, 24);
            _readbackTex = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _camera.targetTexture = _captureRT;

            // Only render the "RoomGeometry" layer
            int captureLayer = LayerMask.NameToLayer("RoomGeometry");
            _camera.cullingMask = 1 << captureLayer;

            if (m_cameraManager.TryGetIntrinsics(out XRCameraIntrinsics intrinsics))
            {
                // Native camera resolution (ARKit/ARCore gives this implicitly)
                Vector2Int nativeRes = intrinsics.resolution; 

                // Scale factors
                float scaleX = (float)_width / nativeRes.x;
                float scaleY = (float)_height / nativeRes.y;

                // Scale intrinsics to match our render resolution
                Vector2 focalLength = new Vector2(
                    intrinsics.focalLength.x * scaleX,
                    intrinsics.focalLength.y * scaleY
                );

                Vector2 principalPoint = new Vector2(
                    intrinsics.principalPoint.x * scaleX,
                    intrinsics.principalPoint.y * scaleY
                );

                // 1. Aspect ratio
                _camera.aspect = (float)_width / _height;

                // 2. Vertical FOV
                float fovY = 2f * Mathf.Atan(0.5f * _height / focalLength.y) * Mathf.Rad2Deg;
                _camera.fieldOfView = fovY;

                // 3. Lens shift (principal point offset)
                _camera.lensShift = new Vector2(
                    (principalPoint.x - _width * 0.5f) / (_width * 0.5f),
                    (_height * 0.5f - principalPoint.y) / (_height * 0.5f) // flip Y
                );
            }
        }
    }
}
