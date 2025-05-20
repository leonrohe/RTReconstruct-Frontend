using System;
using UnityEngine;

namespace RTReconstruct.Core.Models
{
    [Serializable]
    public struct CaptureDeviceIntrinsics
    {
        public Vector2 FocalLength;
        public Vector2 PrincipalPoint;
    }
}