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
    [SerializeField] GameObject debugCubePrefab;
    private List<GameObject> debugCubes = new();
    private List<Vector3> debugCubeOffsetDirs = new();
    private float triggerValue = 0.0f;
    public float lowFreq = 0.0f;
    public float highFreq = 0.0f;
    private float SurfaceLevel = 0f;
    private ComputeBuffer voxelBuffer; // GPU
    private ComputeBuffer voxelToughnessBuffer; // GPU
    private ComputeBuffer collisionInfoBuffer; // GPU
    private float[] readBuffer = new float[32]; // CPU
    private MeshBuilder builder;


    private float VoxelSize => (float)(Math.Pow(2, scale) / 256);

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
        collisionInfoBuffer = new ComputeBuffer(size.x * size.y * size.z, sizeof(float));
        voxelToughnessBuffer = new ComputeBuffer(size.x * size.y * size.z, sizeof(float));

        voxelBuffer.SetData(voxelData);
        voxelToughnessBuffer.SetData(voxelToughnessData);

        computeShader.SetBuffer(4, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(2, "Voxels", voxelBuffer);
        computeShader.SetBuffer(2, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(2, "VoxelToughness", voxelToughnessBuffer);
        computeShader.SetBuffer(3, "Voxels", voxelBuffer);
        computeShader.SetBuffer(3, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(3, "VoxelToughness", voxelToughnessBuffer);

        builder = new MeshBuilder(size, 1000000, computeShader);
        BuildMesh();

        for (int i = 0; i < 4; i++)
        {
            debugCubes.Add(Instantiate(debugCubePrefab));
        }
        debugCubeOffsetDirs.Add(new Vector3(-1, 0, -1));
        debugCubeOffsetDirs.Add(new Vector3(-1, 0, 1));
        debugCubeOffsetDirs.Add(new Vector3(1, 0, -1));
        debugCubeOffsetDirs.Add(new Vector3(1, 0, 1));
    }

    public void FixedUpdate()
    {
        triggerValue = ToolInputManager.instance.triggerValue;
        triggerValue = 1.0f; // Dirty quick fix

        foreach (var aerator in aerators)
        {
            bool aeratorCollided = false;
            switch (aerator.type)
            {
                case AeratorType.Sphere:
                    CarveSphere(aerator, ref aeratorCollided);
                    for (int i = 0; i < 4; i++)
                    {
                        var debugCube = debugCubes[i];
                        var index = GlobalPositionToVoxelIndex(
                            aerator.tool.transform.position + debugCubeOffsetDirs[i] * VoxelSize
                            );
                        debugCube.transform.position = transform.position + (new Vector3(0, 0, 0) + index) * VoxelSize;
                        debugCube.transform.position -= new Vector3(size.x, size.y, size.z) / 2.0f * VoxelSize;
                        debugCube.transform.localScale = new(VoxelSize, VoxelSize, VoxelSize);
                    }
                    break;
                case AeratorType.Capsule:
                    CarveCapsule(aerator, ref aeratorCollided);
                    break;
            }
            if (aeratorCollided)
            {
                RumbleManager.instance.SetCollisionIntensity(highFreq * triggerValue);
            }
        }
        BuildMesh();
    }

    private void BuildMesh()
    {
        builder.BuildIsosurface(voxelBuffer, SurfaceLevel, VoxelSize);
        GetComponent<MeshFilter>().sharedMesh = builder.Mesh;
    }

    public void OnDestroy()
    {
        builder.Dispose();
        voxelBuffer.Dispose();
        voxelToughnessBuffer.Dispose();
        collisionInfoBuffer.Dispose();
    }

    private void SetToolPower(Aerator aerator)
    {
        computeShader.SetFloat("ToolPower", aerator.power * triggerValue);
    }

    private void CarveSphere(Aerator aerator, ref bool aeratorCollided)
    {
        var tp = aerator.tool.transform.position;
        var dp = transform.position - new Vector3(size.x, size.y, size.z) / 2f * VoxelSize;
        computeShader.SetFloats("ToolPosition", tp.x, tp.y, tp.z);
        computeShader.SetFloat("ToolRange", 0.125f);
        computeShader.SetFloat("Scale", VoxelSize);
        SetToolPower(aerator);
        computeShader.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        computeShader.DispatchThreads(2, size.x, size.y, size.z);

        // TODO: check neighboring voxels
        // TODO: move this code to its own function
        // TODO: do not run this code in CarveSphere
        for (int i = 0; i < 4; i++)
        {
            var debugCube = debugCubes[i];
            var voxelIndex = GlobalPositionToVoxelIndex(tp + debugCubeOffsetDirs[i] * VoxelSize);
            var flattenedIndex = voxelIndex.x + size.x * (voxelIndex.y + size.y * voxelIndex.z);
            if (flattenedIndex >= 0 && flattenedIndex < size.x * size.y * size.z)
            {
                collisionInfoBuffer.GetData(readBuffer, 0, flattenedIndex, 1);
                if (readBuffer[0] > 0) aeratorCollided = true;
            }
            if (readBuffer[0] > 0) aeratorCollided = true;
            if (aeratorCollided)
            {
                debugCube.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(0, 255, 0, 128));
            }
            else
            {
                debugCube.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(255, 0, 0, 128));
            }
        }
        // TODO: move this code (clears collision info buffer)
        computeShader.DispatchThreads(4, size.x, size.y, size.z);
    }

    private void CarveCapsule(Aerator aerator, ref bool aeratorCollided)
    {
        var tp = aerator.tool.transform.position;
        var ts = aerator.tool.transform.localScale;
        var dp = transform.position - new Vector3(size.x, size.y, size.z) / 2f * VoxelSize;
        // var topObj = GameObject.Find("top").transform.position;
        // var bottomObj = GameObject.Find("bottom").transform.position;

        // Apply local offsets for top and bottom tips, boost the Y value inversely proportional to the tool's scale
        Vector3 localTopTip = new Vector3(0, 0.03f / ts.x, 0);  // Offset upward
        Vector3 localBottomTip = new Vector3(0, -0.03f / ts.x, 0);  // Offset downward

        // Convert the adjusted local positions back to world space
        Vector3 topTip = aerator.tool.transform.TransformPoint(localTopTip);
        Vector3 bottomTip = aerator.tool.transform.TransformPoint(localBottomTip);

        // Debug.Log($"TopTip: {topTip}, BottomTip: {bottomTip}, Capsule Rotation: {transform.rotation}");

        SetToolPower(aerator);
        computeShader.SetFloats("capsuleToolA", topTip.x, topTip.y, topTip.z);
        computeShader.SetFloats("capsuleToolB", bottomTip.x, bottomTip.y, bottomTip.z);
        computeShader.SetFloat("capsuleToolRange", 0.1f); // used to be 0.125f

        computeShader.SetFloat("Scale", VoxelSize);
        computeShader.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        computeShader.DispatchThreads(3, size.x, size.y, size.z);

        var voxelIndex = GlobalPositionToVoxelIndex(tp);
        var flattenedIndex = voxelIndex.x + size.x * (voxelIndex.y + size.y * voxelIndex.z);
        if (flattenedIndex >= 0 && flattenedIndex < size.x * size.y * size.z)
        {
            collisionInfoBuffer.GetData(readBuffer, 0, flattenedIndex, 1);
            if (readBuffer[0] > 0) aeratorCollided = true;
        }
    }

    private Vector3Int GlobalPositionToVoxelIndex(Vector3 position)
    {
        Vector3 localPosition = position - transform.position;
        localPosition += new Vector3(size.x, size.y, size.z) / 2.0f * VoxelSize;
        Vector3Int index = new(
            (int)Math.Round(localPosition.x / VoxelSize),
            (int)Math.Round(localPosition.y / VoxelSize),
            (int)Math.Round(localPosition.z / VoxelSize)
            );

        index.Clamp(new Vector3Int(0, 0, 0), size);
        return index;
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
