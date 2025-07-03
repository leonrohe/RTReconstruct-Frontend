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

public class RoomReconstructor : MonoBehaviour
{
    [SerializeField]
    private Material vertexMaterial;
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
            ApplyMaterialToAllMeshRenderers(gameObject, vertexMaterial);
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

    private void ApplyMaterialToAllMeshRenderers(GameObject root, Material material)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            var newMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = material;
            }
            renderer.materials = newMaterials;
        }

        Debug.Log($"Applied custom material to {renderers.Length} mesh renderer(s).");
    }

}
