using System;
using RTReconstruct.Collectors.Interfaces;
using RTReconstruct.Core.Models;
using UnityEngine;

namespace RTReconstruct.Collector.SLAM3R
{
    public class SLAM3RCollector : IModelCollector
    {

        private uint m_BufferIdx = 0;
        private readonly CaptureDeviceIntrinsics[] m_Intrinsics;
        private readonly CaptureDeviceExtrinsics[] m_Extrinsics;
        private readonly CaptureDeviceFrame[] m_Frames;

        private readonly float m_tMax;
        private readonly float m_rMax;
        private readonly float m_iMax;
        private CaptureDeviceExtrinsics? m_LastExtrinsic;
        private float m_LastCaptureTime = -Mathf.Infinity;

        public SLAM3RCollector(uint windowsSize = 5, float tMax = 0.1f, float rMax = 15f, float mTime = 0.0f)
        {
            m_Intrinsics = new CaptureDeviceIntrinsics[windowsSize];
            m_Extrinsics = new CaptureDeviceExtrinsics[windowsSize];
            m_Frames = new CaptureDeviceFrame[windowsSize];

            m_tMax = tMax;
            m_rMax = rMax;
            m_iMax = mTime;
        }

        public void Collect(CaptureDeviceIntrinsics intrinsics, CaptureDeviceExtrinsics extrinsics, CaptureDeviceFrame frame)
        {
            Debug.Assert(!IsFull(), "Cannot collect: buffer is full.");

            m_Intrinsics[m_BufferIdx] = intrinsics;
            m_Extrinsics[m_BufferIdx] = extrinsics;
            m_Frames[m_BufferIdx] = frame;
            m_BufferIdx++;
        }

        public ModelFragment Consume(string scene)
        {
            Debug.Assert(IsFull(), "Cannot consume: buffer is not full.");

            var frames_copy = (CaptureDeviceFrame[])m_Frames.Clone();
            var intrinsics_copy = (CaptureDeviceIntrinsics[])m_Intrinsics.Clone();
            var extrinsics_copy = (CaptureDeviceExtrinsics[])m_Extrinsics.Clone();

            m_BufferIdx = 0;

            return new ModelFragment(
                "slam3r",
                scene,
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
                m_LastExtrinsic = extrinsics;
                m_LastCaptureTime = Time.time;
                return true;
            }

            if (Time.time - m_LastCaptureTime < m_iMax)
            {
                return false;
            }

            var last = m_LastExtrinsic.Value;
            Vector3 currentForward = extrinsics.CameraRotation * Vector3.forward;
            Vector3 lastForward = last.CameraRotation * Vector3.forward;

            float rotationAngle = Vector3.Angle(currentForward, lastForward);
            float translation = Vector3.Distance(extrinsics.CameraPosition, last.CameraPosition);

            if (translation > m_tMax || rotationAngle > m_rMax)
            {
                m_LastExtrinsic = extrinsics;
                m_LastCaptureTime = Time.time;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            // Reset buffer index
            m_BufferIdx = 0;

            // Clear arrays to release references and reset value types
            Array.Clear(m_Intrinsics, 0, m_Intrinsics.Length);
            Array.Clear(m_Extrinsics, 0, m_Extrinsics.Length);
            Array.Clear(m_Frames, 0, m_Frames.Length);

            // Reset last extrinsic tracking
            m_LastExtrinsic = null;
            m_LastCaptureTime = -Mathf.Infinity;
        }
    }
}