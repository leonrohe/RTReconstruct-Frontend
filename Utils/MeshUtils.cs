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
    private static Material frustumMaterial;

    public static GameObject CreateCameraFrustumWireframe(Vector3 position, Quaternion rotation, float fov = 60f, float aspect = 1.33f, float length = 0.2f)
    {
        if (frustumMaterial == null)
            frustumMaterial = new Material(Shader.Find("Sprites/Default"));

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
            lr.material = frustumMaterial;
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
            mrChunk.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            createdChunks++;
        }

        Debug.Log($"Chunking complete: created {createdChunks} chunks.");
    }

    private static void RemoveOldChunks(Transform parent)
    {
        // Clear old chunks
        List<GameObject> childrenToDelete = new List<GameObject>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("Chunk_"))
                childrenToDelete.Add(child.gameObject);
        }
        foreach (var c in childrenToDelete)
        {
            Object.Destroy(c.GetComponent<MeshFilter>().sharedMesh);
            Object.Destroy(c.GetComponent<MeshRenderer>().material);
            Object.Destroy(c);
        }
    }
}