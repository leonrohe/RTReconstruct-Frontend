using System;
using System.IO;
using System.Text;
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

        public byte[] Serialize()
        {
            var bytestream = new MemoryStream();

            // Header
            bytestream.Write(new byte[4] { 76, 69, 79, 78}, 0, 4);  // Write magic bytes
            bytestream.Write(BitConverter.GetBytes(1), 0, 4);       // Write version number
            bytestream.Write(BitConverter.GetBytes(Frames.Length), 0, 4); // Write window size

            // Model data
            bytestream.Write(BitConverter.GetBytes(ModelName.Length), 0, 4); // Write model name length
            bytestream.Write(Encoding.UTF8.GetBytes(ModelName), 0, ModelName.Length); // Write model name
            using (var writer = new BinaryWriter(bytestream))
            {
                // Write frames to stream
                foreach (var frame in Frames)
                {
                    writer.Write(frame.Image.Length);
                    writer.Write(frame.Image);
                    writer.Write(frame.Dimensions.x);
                    writer.Write(frame.Dimensions.y);
                }

                // Write intrinsics to stream
                foreach (var intrinsics in Intrinsics)
                {
                    writer.Write(intrinsics.FocalLength.x);
                    writer.Write(intrinsics.FocalLength.y);
                    writer.Write(intrinsics.PrincipalPoint.x);
                    writer.Write(intrinsics.PrincipalPoint.y);
                }

                // Write extrinsics to stream
                foreach (var extrinsics in Extrinsics)
                {
                    writer.Write(extrinsics.CameraPosition.x);
                    writer.Write(extrinsics.CameraPosition.y);
                    writer.Write(extrinsics.CameraPosition.z);
                    writer.Write(extrinsics.CameraRotation.x);
                    writer.Write(extrinsics.CameraRotation.y);
                    writer.Write(extrinsics.CameraRotation.z);
                    writer.Write(extrinsics.CameraRotation.w);
                }
            }
            return bytestream.ToArray();
        }
    }
}

