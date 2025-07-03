using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using RTReconstruct.Networking;
using static ObjImporter;
using System.IO;
using GLTFast;
using GLTFast.Logging;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RoomReconstructor : MonoBehaviour
{
    private MeshFilter meshFilter;
    private readonly Queue<Action> mainThreadActions = new();

    void Start()
    {
        ReconstructionClient.Instance.OnMessageReceived += OnGLBReceived;
    }

    void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }
    }

    private void OnGLBReceived(byte[] bytes)
    {
        Debug.Log("OnGLBReceived invoked with byte length: " + bytes.Length);
        mainThreadActions.Enqueue(() => _ = LoadGlbAsync(bytes));
    }


    private async Task LoadGlbAsync(byte[] bytes)
    {
        var logger = new ConsoleLogger(); // Verbose
        var gltf = new GltfImport(logger: logger);

        bool success = await gltf.Load(bytes);
        if (success)
        {
            ClearOldMeshes();
            await gltf.InstantiateMainSceneAsync(transform);
        }
        else
        {
            Debug.LogError("Failed to load GLTF.");
        }
    }

    private void ClearOldMeshes()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

}
