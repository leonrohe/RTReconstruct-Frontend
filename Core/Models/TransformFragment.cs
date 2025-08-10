using UnityEngine;
using System.IO;
using RTReconstruct.Core.Models.Interfaces;
using System;

namespace RTReconstruct.Core.Models
{
    public class TransformFragment : IFragment
    {
        private Vector3 m_Position;
        private Quaternion m_Rotation;
        private Vector3 m_Scale;

        public TransformFragment(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            m_Position = position;
            m_Rotation = rotation;
            m_Scale = scale;
        }

        public byte[] Serialize()
        {
            var bytestream = new MemoryStream();

            // Header
            bytestream.Write(new byte[4] { 76, 69, 79, 78 }, 0, 4);     // Write magic bytes
            bytestream.Write(BitConverter.GetBytes(2), 0, 4);           // Write version number

            // Position
            bytestream.Write(BitConverter.GetBytes(m_Position.x), 0, 4);
            bytestream.Write(BitConverter.GetBytes(m_Position.y), 0, 4);
            bytestream.Write(BitConverter.GetBytes(m_Position.z), 0, 4);

            // Rotation
            bytestream.Write(BitConverter.GetBytes(m_Rotation.x), 0, 4);
            bytestream.Write(BitConverter.GetBytes(m_Rotation.y), 0, 4);
            bytestream.Write(BitConverter.GetBytes(m_Rotation.z), 0, 4);
            bytestream.Write(BitConverter.GetBytes(m_Rotation.w), 0, 4);

            // Scale
            bytestream.Write(BitConverter.GetBytes(m_Scale.x), 0, 4);
            bytestream.Write(BitConverter.GetBytes(m_Scale.y), 0, 4);
            bytestream.Write(BitConverter.GetBytes(m_Scale.z), 0, 4);

            return bytestream.ToArray();
        }
    }
}