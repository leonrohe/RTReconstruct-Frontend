using System;
using RTReconstruct.Core.Models;

namespace RTReconstruct.Core.Models
{
    [Serializable]
    public class ModelFragment
    {
        public string ModelName;
        public CaptureDeviceFrame[] Frames;
        public CaptureDeviceIntrinsics[] Intrinsics;
        public CaptureDeviceExtrinsics[] Extrinsics;

        public ModelFragment(string modelName, CaptureDeviceFrame[] frames, CaptureDeviceIntrinsics[] intrinsics, CaptureDeviceExtrinsics[] extrinsics)
        {
            ModelName = modelName;
            Frames = frames;
            Intrinsics = intrinsics;
            Extrinsics = extrinsics;
        }
    }
}

