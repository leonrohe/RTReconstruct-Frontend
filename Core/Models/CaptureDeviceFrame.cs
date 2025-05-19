using System;
using UnityEngine;

namespace RTReconstruct.Core.Models
{
    [Serializable]
    public struct CaptureDeviceFrame
    {
        public byte[] Image;
        public Vector2 Dimensions;
    } 
}