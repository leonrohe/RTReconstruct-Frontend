using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RTReconstruct.Core.Models
{
    public class ModelResult
    {

        private string m_scene;
        private bool m_isPointcloud;
        private byte[] m_glb;
        private Vector3 m_Position;
        private Quaternion m_rotation;
        private Vector3 m_scale;

        public ModelResult(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read and validate magic header ("LEON")
                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (magic != "LEON")
                    throw new InvalidDataException("Invalid magic header");

                // Read version
                int version = reader.ReadInt32();

                // Read scene name length and scene name
                int sceneNameLength = reader.ReadInt32();
                m_scene = Encoding.UTF8.GetString(reader.ReadBytes(sceneNameLength));

                // Read isPointcloud (but we'll ignore it here unless needed)
                bool isPointcloud = reader.ReadBoolean();
                m_isPointcloud = isPointcloud;

                // Read transform (3 floats)
                float tx = reader.ReadSingle();
                float ty = reader.ReadSingle();
                float tz = reader.ReadSingle();
                m_Position = new Vector3(tx, ty, tz);

                // Read rotation (4 floats)
                float rx = reader.ReadSingle();
                float ry = reader.ReadSingle();
                float rz = reader.ReadSingle();
                float rw = reader.ReadSingle();
                m_rotation = new Quaternion(rx, ry, rz, rw);

                // Read scale (3 floats)
                float sx = reader.ReadSingle();
                float sy = reader.ReadSingle();
                float sz = reader.ReadSingle();
                m_scale = new Vector3(sx, sy, sz);

                // Read the remaining bytes as GLB data
                m_glb = reader.ReadBytes((int)(stream.Length - stream.Position));
            }
        }

        public string GetScene()
        {
            return m_scene;
        }

        public bool IsPointcloud()
        {
            return m_isPointcloud;
        }

        public byte[] GetGLB()
        {
            return m_glb;
        }

        public Vector3 GetPosition()
        {
            return m_Position;
        }

        public Quaternion GetRotation()
        {
            return m_rotation;
        }

        public Vector3 GetScale()
        {
            return m_scale;
        }
    }
}