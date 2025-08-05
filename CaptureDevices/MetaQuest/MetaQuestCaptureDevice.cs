using System;
using System.Threading.Tasks;
using UnityEngine;
using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.Core.Models;

namespace RTReconstruct.CaptureDevices.MetaQuest
{
    public class MetaQuestCaptureDevice : ICaptureDevice
    {
        public CaptureDeviceExtrinsics GetExtrinsics()
        {
            throw new NotImplementedException();
        }

        public CaptureDeviceFrame GetFrame()
        {
            throw new NotImplementedException();
        }

        public CaptureDeviceIntrinsics GetIntrinsics()
        {
            throw new NotImplementedException();
        }
    }
}