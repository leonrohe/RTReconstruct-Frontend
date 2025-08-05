using System.Collections.Generic;
using UnityEngine;

public class MeshUtils
{
    public static void ChunkMesh(Mesh mesh, Material material, Transform parent, int chunksX, int chunksY, int chunksZ)
    {
        // Clear old chunks
        List<Transform> childrenToDelete = new List<Transform>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("Chunk_"))
                childrenToDelete.Add(child);
        }
        foreach (var c in childrenToDelete)
        {
            UnityEngine.Object.Destroy(c.gameObject);
        }

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;

        Bounds bounds = mesh.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        float chunkSizeX = (max.x - min.x) / chunksX;
        float chunkSizeY = (max.y - min.y) / chunksY;
        float chunkSizeZ = (max.z - min.z) / chunksZ;

        // Use a 3D dictionary key for chunking
        Dictionary<(int, int, int), MeshChunk> chunkDataMap = new Dictionary<(int, int, int), MeshChunk>();

        for (int x = 0; x < chunksX; x++)
            for (int y = 0; y < chunksY; y++)
                for (int z = 0; z < chunksZ; z++)
                    chunkDataMap[(x, y, z)] = new MeshChunk();

        // Assign triangles to chunks based on centroid
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            Vector3 triCenter = (v0 + v1 + v2) / 3f;

            int chunkX = Mathf.Clamp(Mathf.FloorToInt((triCenter.x - min.x) / chunkSizeX), 0, chunksX - 1);
            int chunkY = Mathf.Clamp(Mathf.FloorToInt((triCenter.y - min.y) / chunkSizeY), 0, chunksY - 1);
            int chunkZ = Mathf.Clamp(Mathf.FloorToInt((triCenter.z - min.z) / chunkSizeZ), 0, chunksZ - 1);

            MeshChunk chunk = chunkDataMap[(chunkX, chunkY, chunkZ)];
            chunk.AddTriangle(i0, i1, i2, vertices, normals, uvs);
        }

        int createdChunks = 0;
        foreach (var kvp in chunkDataMap)
        {
            MeshChunk c = kvp.Value;
            if (c.vertexIndices.Count == 0) continue;

            Mesh chunkMesh = new Mesh();
            chunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            chunkMesh.vertices = c.vertices.ToArray();
            if (normals != null && normals.Length > 0)
                chunkMesh.normals = c.normals.ToArray();
            if (uvs != null && uvs.Length > 0)
                chunkMesh.uv = c.uvs.ToArray();
            chunkMesh.triangles = c.triangles.ToArray();

            chunkMesh.RecalculateBounds();
            chunkMesh.RecalculateTangents();

            GameObject chunkGO = new GameObject($"Chunk_{kvp.Key.Item1}_{kvp.Key.Item2}_{kvp.Key.Item3}");
            chunkGO.transform.parent = parent;
            chunkGO.transform.localPosition = Vector3.zero;
            chunkGO.transform.localRotation = Quaternion.identity;
            chunkGO.transform.localScale = Vector3.one;

            MeshFilter mfChunk = chunkGO.AddComponent<MeshFilter>();
            mfChunk.mesh = chunkMesh;

            MeshRenderer mrChunk = chunkGO.AddComponent<MeshRenderer>();
            mrChunk.material = material;

            createdChunks++;
        }

        Debug.Log($"Chunking complete: created {createdChunks} chunks.");
    }

    public static void ChunkPointCloud(Mesh mesh, Material material, Transform parent, int chunksX, int chunksY, int chunksZ)
    {
        // Clear old chunks
        List<Transform> childrenToDelete = new List<Transform>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("PointChunk_"))
                childrenToDelete.Add(child);
        }
        foreach (var c in childrenToDelete)
        {
            Object.Destroy(c.gameObject);
        }

        Vector3[] vertices = mesh.vertices;

        Bounds bounds = mesh.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        float chunkSizeX = (max.x - min.x) / chunksX;
        float chunkSizeY = (max.y - min.y) / chunksY;
        float chunkSizeZ = (max.z - min.z) / chunksZ;

        Dictionary<(int, int, int), List<Vector3>> chunkMap = new Dictionary<(int, int, int), List<Vector3>>();

        // Initialize chunk containers
        for (int x = 0; x < chunksX; x++)
            for (int y = 0; y < chunksY; y++)
                for (int z = 0; z < chunksZ; z++)
                    chunkMap[(x, y, z)] = new List<Vector3>();

        // Assign each vertex to a chunk
        foreach (var vertex in vertices)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt((vertex.x - min.x) / chunkSizeX), 0, chunksX - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt((vertex.y - min.y) / chunkSizeY), 0, chunksY - 1);
            int cz = Mathf.Clamp(Mathf.FloorToInt((vertex.z - min.z) / chunkSizeZ), 0, chunksZ - 1);
            chunkMap[(cx, cy, cz)].Add(vertex);
        }

        int createdChunks = 0;
        foreach (var kvp in chunkMap)
        {
            var points = kvp.Value;
            if (points.Count == 0) continue;

            Mesh chunkMesh = new Mesh();
            chunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            chunkMesh.vertices = points.ToArray();

            int[] indices = new int[points.Count];
            for (int i = 0; i < points.Count; i++) indices[i] = i;
            chunkMesh.SetIndices(indices, MeshTopology.Points, 0);

            GameObject chunkGO = new GameObject($"PointChunk_{kvp.Key.Item1}_{kvp.Key.Item2}_{kvp.Key.Item3}");
            chunkGO.transform.parent = parent;
            chunkGO.transform.localPosition = Vector3.zero;
            chunkGO.transform.localRotation = Quaternion.identity;
            chunkGO.transform.localScale = Vector3.one;

            MeshFilter mf = chunkGO.AddComponent<MeshFilter>();
            mf.mesh = chunkMesh;

            MeshRenderer mr = chunkGO.AddComponent<MeshRenderer>();
            mr.material = material;

            createdChunks++;
        }

        Debug.Log($"Point cloud chunking complete: created {createdChunks} chunks.");
    }

    class MeshChunk
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<int> triangles = new List<int>();

        private Dictionary<int, int> vertexMap = new Dictionary<int, int>();
        public List<int> vertexIndices = new List<int>();

        public void AddTriangle(int i0, int i1, int i2, Vector3[] allVertices, Vector3[] allNormals, Vector2[] allUVs)
        {
            int ni0 = AddVertex(i0, allVertices, allNormals, allUVs);
            int ni1 = AddVertex(i1, allVertices, allNormals, allUVs);
            int ni2 = AddVertex(i2, allVertices, allNormals, allUVs);

            triangles.Add(ni0);
            triangles.Add(ni1);
            triangles.Add(ni2);
        }

        private int AddVertex(int originalIndex, Vector3[] allVertices, Vector3[] allNormals, Vector2[] allUVs)
        {
            if (vertexMap.TryGetValue(originalIndex, out int newIndex))
                return newIndex;

            Vector3 v = allVertices[originalIndex];
            Vector3 n = allNormals != null && allNormals.Length > 0 ? allNormals[originalIndex] : Vector3.zero;
            Vector2 uv = allUVs != null && allUVs.Length > 0 ? allUVs[originalIndex] : Vector2.zero;

            vertices.Add(v);
            if (allNormals != null && allNormals.Length > 0) normals.Add(n);
            if (allUVs != null && allUVs.Length > 0) uvs.Add(uv);

            int newVertIndex = vertices.Count - 1;
            vertexMap[originalIndex] = newVertIndex;
            vertexIndices.Add(originalIndex);

            return newVertIndex;
        }
    }
}