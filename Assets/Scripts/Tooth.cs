using System;
using System.Collections.Generic;
using MarchingCubes;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Tooth : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader = null;
    [SerializeField] private List<Layer> layers;
    [SerializeField] private List<Aerator> aerators;
    [SerializeField] private Vector3Int size = new(32, 32, 32);

    [SerializeField, Range(-2, 8)] private int scale = 3;
    private float SurfaceLevel = 0f;
    private ComputeBuffer voxelBuffer; // GPU
    private ComputeBuffer voxelToughnessBuffer; // GPU
    private MeshBuilder builder;


    public void Start()
    {
        float[,,] voxelData = null;
        float[,,] voxelToughnessData = new float[size.x, size.y, size.z];
        bool hasRenderLayer = false;
        int resolution = Math.Max(size.x, Math.Max(size.y, size.z));
        foreach (var layer in layers)
        {
            if (layer.isRenderLayer)
            {
                if (hasRenderLayer) throw new Exception("Cannot have multiple render layers");
                voxelData = MeshVoxelizer.SmoothVoxelize(layer.mesh, size, resolution, 2);
                hasRenderLayer = true;
            }
            var toughness = MeshVoxelizer.Voxelize(layer.mesh, size, resolution);
            // TODO: Too expensive?
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    for (int z = 0; z < size.z; z++)
                        voxelToughnessData[x, y, z] = Mathf.Max(voxelToughnessData[x, y, z], toughness[x, y, z] * layer.toughness);
        }
        if (!hasRenderLayer) throw new Exception("Render layer not selected");

        voxelBuffer = new ComputeBuffer(size.x * size.y * size.z, sizeof(float));
        voxelBuffer.SetData(voxelData);
        // TODO: FIXME
        voxelToughnessBuffer = new ComputeBuffer(size.x * size.y * size.z, sizeof(float));
        voxelToughnessBuffer.SetData(voxelToughnessData);
        // !TODO
        computeShader.SetBuffer(2, "Voxels", voxelBuffer);
        computeShader.SetBuffer(2, "VoxelToughness", voxelToughnessBuffer);
        computeShader.SetBuffer(3, "Voxels", voxelBuffer);
        computeShader.SetBuffer(3, "VoxelToughness", voxelToughnessBuffer);
        builder = new MeshBuilder(size, 1000000, computeShader);
        BuildMesh();
    }

    public void FixedUpdate()
    {
        foreach (var aerator in aerators)
        {
            switch (aerator.type)
            {

                case AeratorType.Sphere:
                    CarveSphere(aerator);
                    break;
                case AeratorType.Capsule:
                    CarveCapsule(aerator);
                    break;
            }
        }
        BuildMesh();
    }

    private void BuildMesh()
    {
        builder.BuildIsosurface(voxelBuffer, SurfaceLevel, (float)(Math.Pow(2, scale) / 256));
        GetComponent<MeshFilter>().sharedMesh = builder.Mesh;
    }

    public void OnDestroy()
    {
        builder.Dispose();
        voxelBuffer.Dispose();
        voxelToughnessBuffer.Dispose();
    }

    private void CarveSphere(Aerator aerator)
    {
        float _scale = (float)(Math.Pow(2, scale) / 256);
        var tp = aerator.tool.transform.position;
        var dp = transform.position - new Vector3(size.x, size.y, size.z) / 2f * _scale;
        computeShader.SetFloats("ToolPosition", tp.x, tp.y, tp.z);
        computeShader.SetFloat("ToolRange", 0.125f);
        computeShader.SetFloat("Scale", _scale);
        computeShader.SetFloat("ToolPower", aerator.power);
        computeShader.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        computeShader.DispatchThreads(2, size.x, size.y, size.z);
    }
    private void CarveCapsule(Aerator aerator)
    {
        float _scale = (float)(Math.Pow(2, scale) / 256);
        var tp = aerator.tool.transform.position;
        var ts = aerator.tool.transform.localScale;
        var dp = transform.position - new Vector3(size.x, size.y, size.z) / 2f * _scale;
        // var topObj = GameObject.Find("top").transform.position;
        // var bottomObj = GameObject.Find("bottom").transform.position;

        // Apply local offsets for top and bottom tips, boost the Y value inversely proportional to the tool's scale
        Vector3 localTopTip = new Vector3(0, 0.03f / ts.x, 0);  // Offset upward
        Vector3 localBottomTip = new Vector3(0, -0.03f / ts.x, 0);  // Offset downward

        // Convert the adjusted local positions back to world space
        Vector3 topTip = aerator.tool.transform.TransformPoint(localTopTip);
        Vector3 bottomTip = aerator.tool.transform.TransformPoint(localBottomTip);

        Debug.Log($"TopTip: {topTip}, BottomTip: {bottomTip}, Capsule Rotation: {transform.rotation}");

        computeShader.SetFloat("ToolPower", aerator.power);
        computeShader.SetFloats("capsuleToolA", topTip.x, topTip.y, topTip.z);
        computeShader.SetFloats("capsuleToolB", bottomTip.x, bottomTip.y, bottomTip.z);
        computeShader.SetFloat("capsuleToolRange", 0.1f); // used to be 0.125f

        computeShader.SetFloat("Scale", _scale);
        computeShader.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        computeShader.DispatchThreads(3, size.x, size.y, size.z);
    }

    #region Input classes
    private enum AeratorType
    {
        Sphere, Capsule
    };
    [Serializable]
    private struct Aerator
    {
        [SerializeField] public GameObject tool;
        [SerializeField] public AeratorType type;
        [SerializeField, Range(0, 1000)] public int power;
    }
    [Serializable]
    private class Layer
    {
        [SerializeField] public Mesh mesh;
        [SerializeField, Range(0, 1)] public float toughness;
        [SerializeField] public bool isRenderLayer = false;
    };
    #endregion
}
