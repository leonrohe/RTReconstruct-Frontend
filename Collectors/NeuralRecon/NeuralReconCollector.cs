using RTReconstruct.Collectors.Interfaces;
using RTReconstruct.Core.Models;
using UnityEngine;

namespace RTReconstruct.Collectors.NeuralRecon
{
    public class NeuralReconCollector : IModelCollector
    {
        private uint m_BufferIdx = 0;
        private readonly CaptureDeviceIntrinsics[] m_Intrinsics;
        private readonly CaptureDeviceExtrinsics[] m_Extrinsics;
        private readonly CaptureDeviceFrame[] m_Frames;

        private readonly float m_tMax;
        private readonly float m_rMax;
        private CaptureDeviceExtrinsics? m_LastExtrinsic;

        public NeuralReconCollector(uint windowsSize = 9, float tMax = 0.1f, float rMax = 15f)
        {
            m_Intrinsics = new CaptureDeviceIntrinsics[windowsSize];
            m_Extrinsics = new CaptureDeviceExtrinsics[windowsSize];
            m_Frames = new CaptureDeviceFrame[windowsSize];

            m_tMax = tMax;
            m_rMax = rMax;
        }

        public void Collect(CaptureDeviceIntrinsics intrinsics, CaptureDeviceExtrinsics extrinsics, CaptureDeviceFrame frame)
        {
            Debug.Assert(!IsFull(), "Cannot collect: buffer is full.");

            m_Intrinsics[m_BufferIdx] = intrinsics;
            m_Extrinsics[m_BufferIdx] = extrinsics;
            m_Frames[m_BufferIdx] = frame;
            m_LastExtrinsic = extrinsics;
            m_BufferIdx++;
        }

        public ModelFragment Consume()
        {
            Debug.Assert(IsFull(), "Cannot consume: buffer is not full.");

            var frames_copy = (CaptureDeviceFrame[])m_Frames.Clone();
            var intrinsics_copy = (CaptureDeviceIntrinsics[])m_Intrinsics.Clone();
            var extrinsics_copy = (CaptureDeviceExtrinsics[])m_Extrinsics.Clone();

            m_BufferIdx = 0;

            return new ModelFragment(
                "neural_recon",
                frames_copy,
                intrinsics_copy,
                extrinsics_copy
            );
        }

        public bool IsFull()
        {
            return m_BufferIdx >= m_Frames.Length;
        }

        public bool IsNthFrame(uint frameIDX)
        {
            return true;
        }

        public bool ShouldCollect(CaptureDeviceIntrinsics intrinsics, CaptureDeviceExtrinsics extrinsics)
        {
            if (m_LastExtrinsic == null)
            {
                return true;
            }

            var last = m_LastExtrinsic.Value;
            float translation = Vector3.Distance(extrinsics.CameraPosition, last.CameraPosition);
            float rotation = Quaternion.Angle(extrinsics.CameraRotation, last.CameraRotation);

            if (translation > m_tMax || rotation > m_rMax)
            {
                return true;
            }

            return false;
        }
    }
}