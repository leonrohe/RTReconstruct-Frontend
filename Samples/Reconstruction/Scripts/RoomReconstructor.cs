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
using RTReconstruct.Core.Models;

public class RoomReconstructor : MonoBehaviour
{
    [SerializeField]
    private Material MeshMaterial;
    [SerializeField]
    private Material pointcloudMaterial;
    private readonly Queue<Action> mainThreadActions = new();

    void Start()
    {
        ReconstructionClient.Instance.OnModelResultReceived += OnModelResultReceived;
    }

    void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }
    }

    private void OnModelResultReceived(ModelResult result)
    {
        Debug.Log($@"
            Received Result:
            Scene:        {result.GetScene()}
            Position:     {result.GetPosition()}
            Rotation:     {result.GetRotation()}
            Scale:        {result.GetScale()}
            IsPointcloud: {result.IsPointcloud()}
            GLB Size:     {result.GetGLB().Length} bytes
        ");

        mainThreadActions.Enqueue(() => _ = InstantiateResultAsync(result));
    }


    private async Task InstantiateResultAsync(ModelResult result)
    {
        var logger = new ConsoleLogger();
        var gltf = new GltfImport(logger: logger);

        bool success = await gltf.Load(result.GetGLB());
        if (success)
        {
            ClearOldMeshes();

            await gltf.InstantiateMainSceneAsync(transform);

            transform.transform.localPosition = result.GetPosition();
            transform.transform.localRotation = result.GetRotation();
            transform.transform.localScale = result.GetScale();

            var material = result.IsPointcloud() ? pointcloudMaterial : MeshMaterial;
            ApplyMaterialToAllMeshRenderers(gameObject, material);
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
