using System;
using System.Collections.Generic;
using MarchingCubes;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Tooth : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader = null;
    [SerializeField] private List<Aerator> aerators;
    [SerializeField] private GameObject debugCubePrefab;
    [SerializeField] private int resolution = 128;
    private List<GameObject> debugCubes = new();
    private List<Vector3> debugCubeOffsetDirs = new();
    private float triggerValue = 0.0f;
    public float lowFreq = 0.0f;
    public float highFreq = 0.0f;
    private ComputeBuffer voxelBuffer; // GPU
    private ComputeBuffer voxelToughnessBuffer; // GPU
    private ComputeBuffer collisionInfoBuffer; // GPU
    private float[] readBuffer = new float[32]; // CPU
    private MeshBuilder builder;
    
    private MeshFilter _meshFilter;

    private float VoxelSize => _voxelSize;
    // number of voxels in each dimension
    private Vector3Int _gridSize = Vector3Int.zero;
    private float _voxelSize;

    public void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
    }

    public void Start()
    {
        var voxelData = MeshVoxelizer.SmoothVoxelize(_meshFilter.mesh, ref _gridSize, ref _voxelSize, resolution, 1);
        var voxelToughnessData = new float[_gridSize.x, _gridSize.y, _gridSize.z];
        
        voxelBuffer = new ComputeBuffer(_gridSize.x * _gridSize.y * _gridSize.z, sizeof(float));
        collisionInfoBuffer = new ComputeBuffer(_gridSize.x * _gridSize.y * _gridSize.z, sizeof(float));
        voxelToughnessBuffer = new ComputeBuffer(_gridSize.x * _gridSize.y * _gridSize.z, sizeof(float));
        
        voxelBuffer.SetData(voxelData);
        voxelToughnessBuffer.SetData(voxelToughnessData);

        computeShader.SetBuffer(4, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(2, "Voxels", voxelBuffer);
        computeShader.SetBuffer(2, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(2, "VoxelToughness", voxelToughnessBuffer);
        computeShader.SetBuffer(3, "Voxels", voxelBuffer);
        computeShader.SetBuffer(3, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(3, "VoxelToughness", voxelToughnessBuffer);

        builder = new MeshBuilder(_gridSize, 1000000, computeShader);
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
        triggerValue = ToolInputManager.instance.RightTriggerValue;
        triggerValue = 1.0f; // Dirty quick fix // TODO: Remove this line

        bool aeratorCollided = true;
        foreach (var aerator in aerators)
        {
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
                        debugCube.transform.position -= new Vector3(_gridSize.x, _gridSize.y, _gridSize.z) / 2.0f * VoxelSize;
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
        if (aeratorCollided)
            BuildMesh();
    }

    private void BuildMesh()
    {
        builder.BuildIsosurface(voxelBuffer, 0f, VoxelSize);
        _meshFilter.sharedMesh = builder.Mesh;
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
        var dp = transform.position - new Vector3(_gridSize.x, _gridSize.y, _gridSize.z) / 2f * VoxelSize;
        computeShader.SetFloats("ToolPosition", tp.x, tp.y, tp.z);
        computeShader.SetFloat("ToolRange", 0.125f);
        computeShader.SetFloat("Scale", VoxelSize);
        SetToolPower(aerator);
        computeShader.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        computeShader.DispatchThreads(2, _gridSize.x, _gridSize.y, _gridSize.z);

        // TODO: check neighboring voxels
        // TODO: move this code to its own function
        // TODO: do not run this code in CarveSphere
        for (int i = 0; i < 4; i++)
        {
            var debugCube = debugCubes[i];
            var voxelIndex = GlobalPositionToVoxelIndex(tp + debugCubeOffsetDirs[i] * VoxelSize);
            var flattenedIndex = voxelIndex.x + _gridSize.x * (voxelIndex.y + _gridSize.y * voxelIndex.z);
            if (flattenedIndex >= 0 && flattenedIndex < _gridSize.x * _gridSize.y * _gridSize.z)
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
        computeShader.DispatchThreads(4, _gridSize.x, _gridSize.y, _gridSize.z);
    }

    private void CarveCapsule(Aerator aerator, ref bool aeratorCollided)
    {
        var tp = aerator.tool.transform.position;
        var ts = aerator.tool.transform.localScale;
        var dp = transform.position - new Vector3(_gridSize.x, _gridSize.y, _gridSize.z) / 2f * VoxelSize;
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
        computeShader.DispatchThreads(3, _gridSize.x, _gridSize.y, _gridSize.z);

        var voxelIndex = GlobalPositionToVoxelIndex(tp);
        var flattenedIndex = voxelIndex.x + _gridSize.x * (voxelIndex.y + _gridSize.y * voxelIndex.z);
        if (flattenedIndex >= 0 && flattenedIndex < _gridSize.x * _gridSize.y * _gridSize.z)
        {
            collisionInfoBuffer.GetData(readBuffer, 0, flattenedIndex, 1);
            if (readBuffer[0] > 0) aeratorCollided = true;
        }
    }

    private Vector3Int GlobalPositionToVoxelIndex(Vector3 position)
    {
        Vector3 localPosition = position - transform.position;
        localPosition += new Vector3(_gridSize.x, _gridSize.y, _gridSize.z) / 2.0f * VoxelSize;
        Vector3Int index = new(
            (int)Math.Round(localPosition.x / VoxelSize),
            (int)Math.Round(localPosition.y / VoxelSize),
            (int)Math.Round(localPosition.z / VoxelSize)
            );

        index.Clamp(new Vector3Int(0, 0, 0), _gridSize);
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
