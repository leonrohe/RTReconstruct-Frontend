using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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

struct PointData
{
    public Vector3 position;
    public Color color;
    
    public PointData(Vector3 pos, Color col)
    {
        position = pos;
        color = col;
    }
}

public class MeshUtils
{
    public static GameObject CreateCameraFrustumWireframe(Vector3 position, Quaternion rotation, float fov = 60f, float aspect = 1.33f, float length = 0.2f)
    {
        GameObject frustumGO = new GameObject("CameraFrustumWire");

        // Convert FOV to radians
        float halfFOV = fov * 0.5f * Mathf.Deg2Rad;
        float height = Mathf.Tan(halfFOV) * length;
        float width = height * aspect;

        // Define frustum corners in local space
        Vector3 tip = Vector3.zero;
        Vector3 topLeft = new Vector3(-width, height, length);
        Vector3 topRight = new Vector3(width, height, length);
        Vector3 bottomRight = new Vector3(width, -height, length);
        Vector3 bottomLeft = new Vector3(-width, -height, length);

        // Transform corners to world space
        Matrix4x4 trs = Matrix4x4.TRS(position, rotation, Vector3.one);

        Vector3 wTip = trs.MultiplyPoint3x4(tip);
        Vector3 wTL = trs.MultiplyPoint3x4(topLeft);
        Vector3 wTR = trs.MultiplyPoint3x4(topRight);
        Vector3 wBR = trs.MultiplyPoint3x4(bottomRight);
        Vector3 wBL = trs.MultiplyPoint3x4(bottomLeft);

        // Create a line renderer for each edge
        void DrawEdge(Vector3 a, Vector3 b, Color color)
        {
            GameObject lineGO = new GameObject("FrustumEdge");
            lineGO.transform.parent = frustumGO.transform;

            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.widthMultiplier = 0.002f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = color;
            lr.useWorldSpace = true;
        }

        // Draw the 8 edges of the frustum
        DrawEdge(wTip, wTL, Color.cyan);
        DrawEdge(wTip, wTR, Color.cyan);
        DrawEdge(wTip, wBR, Color.cyan);
        DrawEdge(wTip, wBL, Color.cyan);

        DrawEdge(wTL, wTR, Color.cyan);
        DrawEdge(wTR, wBR, Color.cyan);
        DrawEdge(wBR, wBL, Color.cyan);
        DrawEdge(wBL, wTL, Color.cyan);


        // Draw center line
        DrawEdge(wTip, wTip + rotation * Vector3.forward, Color.red);

        return frustumGO;
    }

    public static void ChunkMesh(Mesh mesh, Material material, Transform parent, int chunksX, int chunksY, int chunksZ)
    {
        RemoveOldChunks(parent);

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

    public static void ChunkPointCloud(Mesh mesh, Material material, Transform parent,
        int chunksX, int chunksY, int chunksZ, bool useMultiThreading = true)
    {
        RemoveOldChunks(parent);

        Vector3[] vertices = mesh.vertices;
        Color[] colors = mesh.colors;

        if (vertices.Length == 0) return;

        // Pre-calculate bounds and chunk dimensions
        Bounds bounds = mesh.bounds;
        Vector3 min = bounds.min;
        Vector3 size = bounds.size;

        Vector3 chunkSize = new Vector3(
            size.x / chunksX,
            size.y / chunksY,
            size.z / chunksZ
        );

        Vector3 invChunkSize = new Vector3(
            1f / chunkSize.x,
            1f / chunkSize.y,
            1f / chunkSize.z
        );

        // Use arrays instead of dictionaries for better performance
        int totalChunks = chunksX * chunksY * chunksZ;
        List<PointData>[] chunkData = new List<PointData>[totalChunks];

        // Initialize arrays
        for (int i = 0; i < totalChunks; i++)
        {
            chunkData[i] = new List<PointData>();
        }

        // Assign points to chunks
        if (useMultiThreading && vertices.Length > 10000)
        {
            AssignPointsMultiThreaded(vertices, colors, min, invChunkSize,
                chunksX, chunksY, chunksZ, chunkData);
        }
        else
        {
            AssignPointsSingleThreaded(vertices, colors, min, invChunkSize,
                chunksX, chunksY, chunksZ, chunkData);
        }

        // Create meshes for non-empty chunks
        int createdChunks = CreateChunkMeshes(chunkData, material, parent, chunksX, chunksY, chunksZ);

        Debug.Log($"Point cloud chunking complete: created {createdChunks} chunks from {vertices.Length} points.");
    }

    private static void AssignPointsSingleThreaded(Vector3[] vertices, Color[] colors,
        Vector3 min, Vector3 invChunkSize, int chunksX, int chunksY, int chunksZ,
        List<PointData>[] chunkData)
    {
        bool hasColors = colors != null && colors.Length > 0;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            Color color = hasColors && i < colors.Length ? colors[i] : Color.white;

            // Faster chunk index calculation
            Vector3 relativePos = vertex - min;
            int cx = Mathf.Clamp((int)(relativePos.x * invChunkSize.x), 0, chunksX - 1);
            int cy = Mathf.Clamp((int)(relativePos.y * invChunkSize.y), 0, chunksY - 1);
            int cz = Mathf.Clamp((int)(relativePos.z * invChunkSize.z), 0, chunksZ - 1);

            int chunkIndex = cx + cy * chunksX + cz * chunksX * chunksY;
            chunkData[chunkIndex].Add(new PointData(vertex, color));
        }
    }

    private static void AssignPointsMultiThreaded(Vector3[] vertices, Color[] colors,
        Vector3 min, Vector3 invChunkSize, int chunksX, int chunksY, int chunksZ,
        List<PointData>[] chunkData)
    {
        bool hasColors = colors != null && colors.Length > 0;
        int numThreads = System.Environment.ProcessorCount;
        int pointsPerThread = vertices.Length / numThreads;

        // Create thread-local storage
        List<PointData>[][] threadLocalChunks = new List<PointData>[numThreads][];
        for (int t = 0; t < numThreads; t++)
        {
            threadLocalChunks[t] = new List<PointData>[chunkData.Length];
            for (int i = 0; i < chunkData.Length; i++)
            {
                threadLocalChunks[t][i] = new List<PointData>();
            }
        }

        // Process points in parallel
        Parallel.For(0, numThreads, threadIndex =>
        {
            int startIndex = threadIndex * pointsPerThread;
            int endIndex = threadIndex == numThreads - 1 ? vertices.Length : (threadIndex + 1) * pointsPerThread;

            var localChunks = threadLocalChunks[threadIndex];

            for (int i = startIndex; i < endIndex; i++)
            {
                Vector3 vertex = vertices[i];
                Color color = hasColors && i < colors.Length ? colors[i] : Color.white;

                Vector3 relativePos = vertex - min;
                int cx = Mathf.Clamp((int)(relativePos.x * invChunkSize.x), 0, chunksX - 1);
                int cy = Mathf.Clamp((int)(relativePos.y * invChunkSize.y), 0, chunksY - 1);
                int cz = Mathf.Clamp((int)(relativePos.z * invChunkSize.z), 0, chunksZ - 1);

                int chunkIndex = cx + cy * chunksX + cz * chunksX * chunksY;
                localChunks[chunkIndex].Add(new PointData(vertex, color));
            }
        });

        // Merge thread-local results
        for (int chunkIndex = 0; chunkIndex < chunkData.Length; chunkIndex++)
        {
            for (int t = 0; t < numThreads; t++)
            {
                chunkData[chunkIndex].AddRange(threadLocalChunks[t][chunkIndex]);
            }
        }
    }

    private static int CreateChunkMeshes(List<PointData>[] chunkData, Material material,
        Transform parent, int chunksX, int chunksY, int chunksZ)
    {
        int createdChunks = 0;

        for (int i = 0; i < chunkData.Length; i++)
        {
            var points = chunkData[i];
            if (points.Count == 0) continue;

            // Convert back to chunk coordinates for naming
            int z = i / (chunksX * chunksY);
            int y = (i - z * chunksX * chunksY) / chunksX;
            int x = i % chunksX;

            // Pre-allocate arrays with exact size
            Vector3[] vertices = new Vector3[points.Count];
            Color[] colors = new Color[points.Count];
            int[] indices = new int[points.Count];

            // Single loop to fill all arrays
            for (int j = 0; j < points.Count; j++)
            {
                vertices[j] = points[j].position;
                colors[j] = points[j].color;
                indices[j] = j;
            }

            // Create optimized mesh
            Mesh chunkMesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = vertices,
                colors = colors
            };
            chunkMesh.SetIndices(indices, MeshTopology.Points, 0);
            chunkMesh.UploadMeshData(false); // Mark as non-readable for better performance

            // Create GameObject with optimized setup
            GameObject chunkGO = new GameObject($"Chunk_{x}_{y}_{z}");
            chunkGO.isStatic = true; // Static for better batching

            Transform chunkTransform = chunkGO.transform;
            chunkTransform.SetParent(parent, false); // worldPositionStays = false for better performance
            chunkTransform.localPosition = Vector3.zero;
            chunkTransform.localRotation = Quaternion.identity;
            chunkTransform.localScale = Vector3.one;

            // Add components
            MeshFilter mf = chunkGO.AddComponent<MeshFilter>();
            mf.mesh = chunkMesh;

            MeshRenderer mr = chunkGO.AddComponent<MeshRenderer>();
            mr.material = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Usually not needed for point clouds

            createdChunks++;
        }

        return createdChunks;
    }

    // Alternative version using Unity Jobs System for even better performance
    public static void ChunkPointCloudWithJobs(Mesh mesh, Material material, Transform parent,
        int chunksX, int chunksY, int chunksZ)
    {
        // Implementation would use IJob/IJobParallelFor for maximum performance
        // This would be the ultimate optimization but requires more complex setup
    }

    // Utility method for spatial queries (useful for LOD systems)
    public static List<GameObject> GetChunksInRadius(Transform parent, Vector3 center, float radius)
    {
        List<GameObject> chunksInRadius = new List<GameObject>();
        float radiusSquared = radius * radius;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if ((child.position - center).sqrMagnitude <= radiusSquared)
            {
                chunksInRadius.Add(child.gameObject);
            }
        }

        return chunksInRadius;
    }

    private static void RemoveOldChunks(Transform parent)
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
    }
}