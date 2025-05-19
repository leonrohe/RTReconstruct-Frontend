using RTReconstruct.Core.Models;

namespace RTReconstruct.Core.Models
{
    public class ModelFragment
    {
        public string ModelName { get; }
        public CaptureDeviceFrame[] Frames { get; }
        public CaptureDeviceIntrinsics[] Intrinsics { get; }
        public CaptureDeviceExtrinsics[] Extrinsics { get; }

        public ModelFragment(string modelName, CaptureDeviceFrame[] frames, CaptureDeviceIntrinsics[] intrinsics, CaptureDeviceExtrinsics[] extrinsics)
        {
            ModelName = modelName;
            Frames = frames;
            Intrinsics = intrinsics;
            Extrinsics = extrinsics;
        }
    }
}

