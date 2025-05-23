using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using RTReconstruct.Networking;
using static ObjImporter;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RoomReconstructor : MonoBehaviour
{
    private MeshFilter meshFilter;
    private readonly Queue<Action> mainThreadActions = new();

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        ReconstructionClient.Instance.OnMessageReceived += OnObjReceived;
    }

    void Update()
    {
        // Execute queued actions on the main thread
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action();
        }
    }

    private void OnObjReceived(string obj)
    {
        // Start background task for parsing
        Task.Run(() =>
        {
            ParsedMeshData parsed = ObjImporter.Parse(obj);

            // Schedule mesh assignment back on the main thread
            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(() =>
                {
                    Mesh mesh = new Mesh();
                    mesh.SetVertices(parsed.vertices);
                    mesh.SetTriangles(parsed.triangles, 0);
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();

                    meshFilter.mesh = mesh;
                });
            }
        });
    }
}
