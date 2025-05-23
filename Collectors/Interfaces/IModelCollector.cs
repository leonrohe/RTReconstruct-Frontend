using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.Core.Models;

namespace RTReconstruct.Collectors.Interfaces
{
    public interface IModelCollector
    {
        bool IsNthFrame(uint frameIDX);

        bool ShouldCollect(CaptureDeviceIntrinsics intrinsics, CaptureDeviceExtrinsics extrinsics);

        bool IsFull();

        void Collect(CaptureDeviceIntrinsics intrinsics, CaptureDeviceExtrinsics extrinsics, CaptureDeviceFrame frame);

        ModelFragment Consume();
    }
}