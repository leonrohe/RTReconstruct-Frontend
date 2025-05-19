using RTReconstruct.Core.Models;
using UnityEngine;

namespace RTReconstruct.CaptureDevices.Interfaces
{
    public interface ICaptureDevice
    {
        public CaptureDeviceFrame GetFrame();

        public CaptureDeviceIntrinsics GetIntrinsics();

        public CaptureDeviceExtrinsics GetExtrinsics();
    }
}