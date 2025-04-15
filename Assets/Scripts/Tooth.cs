using System;
using System.Collections.Generic;
using MarchingCubes;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Tooth : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader = null;
    [SerializeField] private int resolution = 128;
    private float triggerValue = 0.0f;
    public float lowFreq = 0.0f;
    public float highFreq = 0.0f;
    private ComputeBuffer voxelBuffer; // GPU
    private ComputeBuffer voxelUVBuffer; // GPU
    private ComputeBuffer voxelToughnessBuffer; // GPU
    private ComputeBuffer collisionInfoBuffer; // GPU
    private float[] readBuffer = new float[32]; // CPU
    private MeshBuilder builder;
    
    private MeshFilter _meshFilter;
    private AeratorTip[] _aerators;

    // number of voxels in each dimension
    private Vector3Int _gridSize = Vector3Int.zero;
    private float _voxelSize;
    
    public Vector3Int GridSize => _gridSize;
    public float VoxelSize => _voxelSize * transform.localScale.x;
    public Vector3 CenterOffset => new Vector3(GridSize.x * transform.localScale.x,
        GridSize.y * transform.localScale.y, GridSize.z * transform.localScale.z) * VoxelSize / 2.0f;
    
    private bool _collided = false;
    public bool IsCarvedThisFrame => _collided;

    public void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _aerators = FindObjectsOfType<AeratorTip>();
    }

    public void Start()
    {
        transform.position += _meshFilter.mesh.bounds.center;
        Vector2[,,] voxelUV = null;
        var voxelData = MeshVoxelizer.SmoothVoxelize(_meshFilter.mesh, ref _gridSize, ref _voxelSize, resolution, 1, ref voxelUV);
        var voxelToughnessData = new float[_gridSize.x, _gridSize.y, _gridSize.z];
        for (int x = 0; x < _gridSize.x; x++)
            for (int y = 0; y < _gridSize.y; y++)
                for (int z = 0; z < _gridSize.z; z++)
                    voxelToughnessData[x, y, z] = voxelData[x, y, z];
        
        voxelBuffer = new ComputeBuffer(_gridSize.x * _gridSize.y * _gridSize.z, sizeof(float));
        voxelUVBuffer = new ComputeBuffer(_gridSize.x * _gridSize.y * _gridSize.z, sizeof(float)*2);
        collisionInfoBuffer = new ComputeBuffer(_gridSize.x * _gridSize.y * _gridSize.z, sizeof(float));
        voxelToughnessBuffer = new ComputeBuffer(_gridSize.x * _gridSize.y * _gridSize.z, sizeof(float));
        
        voxelBuffer.SetData(voxelData);
        voxelUVBuffer.SetData(voxelUV);
        voxelToughnessBuffer.SetData(voxelToughnessData);

        computeShader.SetBuffer(0, "VoxelUV", voxelUVBuffer);
        computeShader.SetBuffer(4, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(2, "Voxels", voxelBuffer);
        computeShader.SetBuffer(2, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(2, "VoxelToughness", voxelToughnessBuffer);
        computeShader.SetBuffer(3, "Voxels", voxelBuffer);
        computeShader.SetBuffer(3, "CollisionInfo", collisionInfoBuffer);
        computeShader.SetBuffer(3, "VoxelToughness", voxelToughnessBuffer);

        builder = new MeshBuilder(_gridSize, 1000000, computeShader);
        BuildMesh();
    }

    public void FixedUpdate()
    {
        triggerValue = ToolInputManager.instance.RightTriggerValue;
        triggerValue = 1.0f; // Dirty quick fix // TODO: Remove this line

        _collided = false;
        foreach (var aerator in _aerators)
        {
            switch (aerator.AeratorType)
            {
                case AeratorType.Sphere:
                    CarveSphere(aerator);
                    break;
                case AeratorType.Capsule:
                    CarveCapsule(aerator);
                    break;
            }
            if (_collided)
            {
                RumbleManager.instance.SetCollisionIntensity(highFreq * triggerValue);
            }
        }

        BuildMesh();
        _collided = readCollision();
        // Clear collision info buffer
        computeShader.DispatchThreads(4, _gridSize.x, _gridSize.y, _gridSize.z);
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
        voxelUVBuffer.Dispose();
        voxelToughnessBuffer.Dispose();
        collisionInfoBuffer.Dispose();
    }

    private void SetToolPower(AeratorTip aerator)
    {
        computeShader.SetFloat("ToolPower", aerator.Power * triggerValue);
    }

    private void CarveSphere(AeratorTip aerator)
    {
        var tp = aerator.Transform.position;
        var dp = transform.position - CenterOffset;
        computeShader.SetFloats("ToolPosition", tp.x, tp.y, tp.z);
        computeShader.SetFloat("ToolRange", 0.125f);
        computeShader.SetFloat("Scale", VoxelSize);
        SetToolPower(aerator);
        computeShader.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        computeShader.DispatchThreads(2, _gridSize.x, _gridSize.y, _gridSize.z);
        
        // var voxelIndex = GlobalPositionToVoxelIndex(tp);
        // var flattenedIndex = voxelIndex.z + voxelIndex.y * _gridSize.z + voxelIndex.x * _gridSize.y * _gridSize.z;
        // if (flattenedIndex >= 0 && flattenedIndex < _gridSize.x * _gridSize.y * _gridSize.z)
        // {
        //     collisionInfoBuffer.GetData(readBuffer, 0, flattenedIndex, 1);
        //     if (readBuffer[0] > 0) _collided = true;
        // }
    }

    private void CarveCapsule(AeratorTip aerator)
    {
        var tp = aerator.Transform.position;
        var ts = aerator.Transform.localScale;
        var dp = transform.position - CenterOffset;
        // var topObj = GameObject.Find("top").transform.position;
        // var bottomObj = GameObject.Find("bottom").transform.position;

        // Apply local offsets for top and bottom tips, boost the Y value inversely proportional to the tool's scale
        Vector3 localTopTip = new Vector3(0, 1, 0);  // Offset upward
        Vector3 localBottomTip = new Vector3(0, -1, 0);  // Offset downward

        // Convert the adjusted local positions back to world space
        Vector3 topTip = aerator.Transform.TransformPoint(localTopTip);
        Vector3 bottomTip = aerator.Transform.TransformPoint(localBottomTip);

        // Debug.Log($"TopTip: {topTip}, BottomTip: {bottomTip}, Capsule Rotation: {transform.rotation}");

        SetToolPower(aerator);
        computeShader.SetFloats("capsuleToolA", topTip.x, topTip.y, topTip.z);
        computeShader.SetFloats("capsuleToolB", bottomTip.x, bottomTip.y, bottomTip.z);
        computeShader.SetFloat("capsuleToolRange", ts.y); // used to be 0.125f

        computeShader.SetFloat("Scale", VoxelSize);
        computeShader.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        computeShader.DispatchThreads(3, _gridSize.x, _gridSize.y, _gridSize.z);

        // var voxelIndex = GlobalPositionToVoxelIndex(tp);
        // var flattenedIndex = voxelIndex.z + voxelIndex.y * _gridSize.z + voxelIndex.x * _gridSize.y * _gridSize.z;
        // if (flattenedIndex >= 0 && flattenedIndex < _gridSize.x * _gridSize.y * _gridSize.z)
        // {
        //     collisionInfoBuffer.GetData(readBuffer, 0, flattenedIndex, 1);
        //     if (readBuffer[0] > 0) _collided = true;
        // }
    }

    public Vector3Int GlobalPositionToVoxelIndex(Vector3 position)
    {
        Vector3 localPosition = position - transform.position;
        localPosition += CenterOffset;
        Vector3Int index = new(
            (int)Math.Round(localPosition.x / VoxelSize),
            (int)Math.Round(localPosition.y / VoxelSize),
            (int)Math.Round(localPosition.z / VoxelSize)
            );

        index.Clamp(new Vector3Int(0, 0, 0), _gridSize);
        return index;
    }

    private bool readCollision()
    {
        collisionInfoBuffer.GetData(readBuffer, 0, 0, 1);
        return readBuffer[0] > 0;
    }
}
