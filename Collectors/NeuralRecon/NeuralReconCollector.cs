using RTReconstruct.CaptureDevices.Interfaces;
using RTReconstruct.Collectors.Interfaces;
using RTReconstruct.Core.Models;

namespace RTReconstruct.Collectors.NeuralRecon
{
    public class NeuralReconCollector : IModelCollector
    {
        public void Collect(CaptureDeviceFrame frame)
        {
            throw new System.NotImplementedException();
        }

        public ModelFragment Consume()
        {
            throw new System.NotImplementedException();
        }

        public bool IsFull()
        {
            throw new System.NotImplementedException();
        }

        public bool IsNthFrame(uint frameIDX)
        {
            throw new System.NotImplementedException();
        }

        public bool ShouldCollect(CaptureDeviceIntrinsics intrinsics, CaptureDeviceExtrinsics extrinsics)
        {
            throw new System.NotImplementedException();
        }
    }
}