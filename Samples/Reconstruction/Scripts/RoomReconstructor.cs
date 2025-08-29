using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using RTReconstruct.Networking;
using System.IO;
using GLTFast;
using GLTFast.Logging;
using RTReconstruct.Core.Models;
using UnityEngine.VFX;

public class RoomReconstructor : MonoBehaviour
{
    [SerializeField]
    private Vector3 relativeOffset = new Vector3(0, 0, 0);
    [SerializeField]
    private Material MeshMaterial;
    [SerializeField]
    private Material pointcloudMaterial;
    [SerializeField]
    private VisualEffect vfx;
    private GraphicsBuffer posBuffer, colBuffer;

    private readonly Queue<Action> mainThreadActions = new();

    void Start()
    {
        ReconstructionClient.Instance.OnModelResultReceived += OnModelResultReceived;
        posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 100_000, sizeof(float) * 3);
        colBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 100_000, sizeof(float) * 4);
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
        try
        {
            var logger = new ConsoleLogger();
            var gltf = new GltfImport(logger: logger);

            bool success = await gltf.Load(result.GetGLB());
            if (success)
            {
                transform.localPosition = result.GetPosition() + relativeOffset;
                transform.localRotation = result.GetRotation();
                transform.localScale = new Vector3(
                    result.GetScale().x / transform.parent.localScale.x,
                    result.GetScale().y / transform.parent.localScale.y,
                    result.GetScale().z / transform.parent.localScale.z
                );

                Mesh mesh = gltf.GetMesh(0, 0);
                var material = result.IsPointcloud() ? pointcloudMaterial : MeshMaterial;
                if (result.IsPointcloud())
                {
                    InitPointcloud(mesh);
                }
                else
                {
                    MeshUtils.ChunkMesh(mesh, material, transform, 5, 5, 5);
                }
                Destroy(mesh);
            }
            else
            {
                Debug.LogError("Failed to load GLTF.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in InstantiateResultAsync: {ex}");
        }
    }

    private void InitPointcloud(Mesh mesh)
    {
        posBuffer.SetData(mesh.vertices);
        colBuffer.SetData(mesh.colors);

        vfx.SetGraphicsBuffer("PosBuffer", posBuffer);
        vfx.SetGraphicsBuffer("ColBuffer", colBuffer);
        vfx.SetInt("PointCount", 100_000);
        vfx.Reinit();
    }
}
