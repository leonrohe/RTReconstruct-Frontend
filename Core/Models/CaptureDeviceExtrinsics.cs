using System;
using UnityEngine;

namespace RTReconstruct.Core.Models
{
    [Serializable]
    public struct CaptureDeviceExtrinsics
    {
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
    } 
}