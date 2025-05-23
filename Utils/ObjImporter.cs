using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class ObjImporter
{
    public struct ParsedMeshData
    {
        public List<Vector3> vertices;
        public List<int> triangles;
    }

    /// <summary>
    /// Parses .obj text and returns raw vertex/triangle data.
    /// Safe to call from background threads.
    /// </summary>
    public static ParsedMeshData Parse(string objText)
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();

        using StringReader reader = new(objText);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("v "))
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    vertices.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)
                    ));
                }
            }
            else if (line.StartsWith("f "))
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < parts.Length; i++)
                {
                    string[] vertexParts = parts[i].Split('/');
                    if (int.TryParse(vertexParts[0], out int vertexIndex))
                    {
                        triangles.Add(vertexIndex - 1); // OBJ uses 1-based indexing
                    }
                }
            }
        }

        return new ParsedMeshData
        {
            vertices = vertices,
            triangles = triangles
        };
    }
}