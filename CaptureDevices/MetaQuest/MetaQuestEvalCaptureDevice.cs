using System;
using System.Threading.Tasks;
using UnityEngine;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.Core.Models;
using PassthroughCameraSamples;
using Oculus.Interaction.Samples;
using Unity.Mathematics;
using Meta.XR;

namespace RTReconstruct.CaptureDevices.MetaQuest
{
    public class MetaQuestEvalCaptureDevice : ICaptureDevice
    {

        private Camera _camera;
        private PassthroughCameraAccess m_cameraAccess;
        private RenderTexture _captureRT;
        private Texture2D _readbackTex;
        private int _width = 640;
        private int _height = 480;

        public MetaQuestEvalCaptureDevice(Camera camera, PassthroughCameraAccess cameraAccess)
        {
            _camera = camera;
            m_cameraAccess = cameraAccess;
            InitCamera();
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
            if (_captureRT == null || _readbackTex == null)
                throw new Exception("Capture camera not initialized");


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

        public CaptureDeviceIntrinsics GetIntrinsics()
        {
        #if UNITY_EDITOR
            // Hardcoded values for in-editor replay
            return new CaptureDeviceIntrinsics()
            {
                FocalLength = new Vector2(432.53857422f, 432.53857422f),
                PrincipalPoint = new Vector2(321.10760498f, 240.78053284f)
            };
        #else
            PassthroughCameraAccess.CameraIntrinsics intrinsics = m_cameraAccess.Intrinsics;

            return new CaptureDeviceIntrinsics()
            {
                FocalLength = new Vector2(432.53857422f, 432.53857422f),
                PrincipalPoint = new Vector2(321.10760498f, 240.78053284f)
            };
#endif
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

            // Set camera intrinsics
            var intrinsics = GetIntrinsics();

            // 1. Aspect ratio
            _camera.aspect = (float)_width / _height;

            // 2. Vertical FOV
            float fovY = 2f * Mathf.Atan(0.5f * _height / intrinsics.FocalLength.y) * Mathf.Rad2Deg;
            _camera.fieldOfView = fovY;

            // 3. Lens shift (principal point offset)
            Vector2 pp = intrinsics.PrincipalPoint;
            _camera.lensShift = new Vector2(
                (pp.x - _width * 0.5f) / (_width * 0.5f),
                (_height * 0.5f - pp.y) / (_height * 0.5f) // Y axis flipped in Unity
            );
        }
    }
}