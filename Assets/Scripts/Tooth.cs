using System;
using System.Collections;
using System.IO;
using Managers;
using Controllers;
using MarchingCubes;
using Utils;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Tooth : MonoBehaviour
{
    private static readonly int VoxelUV = Shader.PropertyToID("VoxelUV");
    private static readonly int CollisionInfo = Shader.PropertyToID("CollisionInfo");
    private static readonly int Voxels = Shader.PropertyToID("Voxels");
    private static readonly int VoxelToughness = Shader.PropertyToID("VoxelToughness");
    private static readonly int ToolPower = Shader.PropertyToID("ToolPower");
    private static readonly int ToolPosition = Shader.PropertyToID("ToolPosition");
    private static readonly int ToolRange = Shader.PropertyToID("ToolRange");
    private static readonly int Scale = Shader.PropertyToID("Scale");
    private static readonly int DestructiblePosition = Shader.PropertyToID("DestructiblePosition");
    private static readonly int CapsuleToolA = Shader.PropertyToID("capsuleToolA");
    private static readonly int CapsuleToolB = Shader.PropertyToID("capsuleToolB");
    private static readonly int CapsuleToolRange = Shader.PropertyToID("capsuleToolRange");

    [SerializeField] private ComputeShader computeShader = null;
    [SerializeField] private int resolution = 128;
    [SerializeField] private MeshFilter errorVisualizer = null;
    [SerializeField] private MeshFilter carvedVisualizer = null;
    [SerializeField] private MeshFilter distanceVisualizer = null;
    [SerializeField] private float triggerValue = 0.0f;
    public float lowFreq = 0.0f;
    public float highFreq = 0.0f;
    private ComputeBuffer voxelBuffer; // GPU
    private ComputeBuffer voxelUVBuffer; // GPU
    private ComputeBuffer voxelToughnessBuffer; // GPU
    private ComputeBuffer collisionInfoBuffer; // GPU
    private float[] readBuffer = new float[32]; // CPU
    private MeshBuilder builder;
    public bool isSelected;

    // serialization related fields
    private string _modelKey;
    private float[,,] _voxelData3D;
    private Vector2[,,] _voxelUV3D;

    private MeshFilter _meshFilter;
    private AeratorTip[] _aerators;

    // number of voxels in each dimension
    private Vector3Int _gridSize = Vector3Int.zero;
    private float _voxelSize;

    public Vector3Int GridSize => _gridSize;
    public float VoxelSize => _voxelSize * transform.lossyScale.x;

    public Vector3 CenterOffset => new Vector3(GridSize.x, GridSize.y, GridSize.z) * VoxelSize / 2.0f;

    private bool _collided = false;
    public bool IsCarvedThisFrame => _collided;

    public void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _aerators = FindObjectsOfType<AeratorTip>();
    }

    public void Start()
    {
        StartCoroutine(VoxelizeMesh());
    }

    private IEnumerator VoxelizeMesh()
    {
        UIManager.Instance.ShowLoadingWindow("Loading");
        yield return null;
        // adjust pivot
        transform.position += _meshFilter.mesh.bounds.center * transform.lossyScale.x;
        /*Debug.Log($"transform.lossyScale.x: {transform.lossyScale.x}");*/

        // Attempt to load cached voxel grid + UVs
        _modelKey = gameObject.name;

        if (!MeshSerializer.Load(_modelKey, out _gridSize, out _voxelSize, out _voxelData3D, out _voxelUV3D))
        {
            // No cache: voxelize and save
            _voxelData3D = MeshVoxelizer.SmoothVoxelize(
                _meshFilter.mesh,
                ref _gridSize,
                ref _voxelSize,
                resolution,
                1,
                ref _voxelUV3D
            );
            if (_modelKey == "nocache")
            {
                Debug.Log("Skip caching stage (object is named 'nocache')");
            }
            else
            {
                MeshSerializer.SaveAsync(_modelKey, _gridSize, _voxelSize, _voxelData3D, _voxelUV3D);
                Debug.Log($"Voxelized and cached model '{_modelKey}'");
            }
        }
        else
        {
            Debug.Log($"Loaded cached voxel data for model '{_modelKey}'");
        }

        // Flatten 3D arrays into 1D for GPU buffers
        int count = _gridSize.x * _gridSize.y * _gridSize.z;
        var voxels1D = new float[count];
        var uvs1D = new Vector2[count];
        var toughness1D = new float[count];

        int idx = 0;
        for (int x = 0; x < _gridSize.x; x++)
        for (int y = 0; y < _gridSize.y; y++)
        for (int z = 0; z < _gridSize.z; z++)
        {
            voxels1D[idx] = _voxelData3D[x, y, z];
            uvs1D[idx] = _voxelUV3D[x, y, z];
            toughness1D[idx++] = _voxelData3D[x, y, z];
        }

        // Create and upload compute buffers
        int totalVoxels = count;
        voxelBuffer = new ComputeBuffer(totalVoxels, sizeof(float));
        voxelUVBuffer = new ComputeBuffer(totalVoxels, sizeof(float) * 2);
        collisionInfoBuffer = new ComputeBuffer(totalVoxels, sizeof(float));
        voxelToughnessBuffer = new ComputeBuffer(totalVoxels, sizeof(float));

        voxelBuffer.SetData(voxels1D);
        voxelUVBuffer.SetData(uvs1D);
        voxelToughnessBuffer.SetData(toughness1D);

        // Bind buffers (use cached kernel indices or FindKernel)
        int kMeshReconstruct = computeShader.FindKernel("MeshReconstruction");
        int kCarve = computeShader.FindKernel("Carve");
        int kCarveCapsule = computeShader.FindKernel("CarveCapsule");
        int kClearColl = computeShader.FindKernel("ClearCollisionBuffer");

        computeShader.SetBuffer(kMeshReconstruct, VoxelUV, voxelUVBuffer);
        computeShader.SetBuffer(kClearColl, CollisionInfo, collisionInfoBuffer);

        computeShader.SetBuffer(kCarve, Voxels, voxelBuffer);
        computeShader.SetBuffer(kCarve, VoxelToughness, voxelToughnessBuffer);
        computeShader.SetBuffer(kCarve, CollisionInfo, collisionInfoBuffer);

        computeShader.SetBuffer(kCarveCapsule, Voxels, voxelBuffer);
        computeShader.SetBuffer(kCarveCapsule, VoxelToughness, voxelToughnessBuffer);
        computeShader.SetBuffer(kCarveCapsule, CollisionInfo, collisionInfoBuffer);

        // Initialize mesh builder and build initial mesh
        builder = new MeshBuilder(_gridSize, 1000000, computeShader);
        BuildMesh();
        UIManager.Instance.HideLoadingWindow();
    }

    private bool _done = false;

    public void Evaluate()
    {
        if (_done) return;
        _done = true;
        StartCoroutine(evaluate());
    }

    public void FixedUpdate()
    {
        if (_done) return;

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
                /*Debug.Log("Collided");*/
                if (XRModeSwitcher.instance.isXRMode)
                {
                    triggerValue = ToolInputManager.instance.CurrentHoldingHand switch
                    {
                        Handedness.Left => ToolInputManager.instance.LeftTriggerValue,
                        Handedness.Right => ToolInputManager.instance.RightTriggerValue,
                        _ => 0f
                    };
                    /*Debug.Log($"CurrentHoldingHand: {ToolInputManager.instance.CurrentHoldingHand}, triggerValue: {triggerValue}");*/
                }
                else
                {
                    triggerValue = ToolInputManager.instance.RightTriggerValue;
                    /*Debug.Log(triggerValue);*/
                    //triggerValue = 1.0f; // Dirty quick fix // TODO: Remove this line
                }

                RumbleManager.instance.SetCollisionIntensity(highFreq * triggerValue);
            }

            _collided = false;
        }

        BuildMesh();
        _collided = readCollision();
        // Clear collision info buffer
        computeShader.DispatchThreads(4, _gridSize.x, _gridSize.y, _gridSize.z);
    }

    private void BuildMesh()
    {
        var old = transform.localScale;
        transform.localScale /= transform.lossyScale.x;
        builder.BuildIsosurface(voxelBuffer, 0f, VoxelSize);
        _meshFilter.sharedMesh = builder.Mesh;
        transform.localScale = old;
    }

    public void OnDestroy()
    {
        if (builder == null) return;
        builder.Dispose();
        voxelBuffer.Dispose();
        voxelUVBuffer.Dispose();
        voxelToughnessBuffer.Dispose();
        collisionInfoBuffer.Dispose();
    }

    private void SetToolPower(AeratorTip aerator)
    {
        computeShader.SetFloat(ToolPower, aerator.Power * triggerValue);
    }

    private void CarveSphere(AeratorTip aerator)
    {
        var tp = aerator.Transform.position;
        var dp = transform.position - CenterOffset;
        computeShader.SetFloats(ToolPosition, tp.x, tp.y, tp.z);
        computeShader.SetFloat(ToolRange, 0.125f);
        computeShader.SetFloat(Scale, VoxelSize);
        SetToolPower(aerator);
        computeShader.SetFloats(DestructiblePosition, dp.x, dp.y, dp.z);
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
        var ts = aerator.Transform.lossyScale;
        var dp = transform.position - CenterOffset;
        // var topObj = GameObject.Find("top").transform.position;
        // var bottomObj = GameObject.Find("bottom").transform.position;

        // Apply local offsets for top and bottom tips, boost the Y value inversely proportional to the tool's scale
        Vector3 localTopTip = new Vector3(0, 1 - ts.x * 2.0f, 0); // Offset upward
        Vector3 localBottomTip = new Vector3(0, 2.0f * ts.x - 1, 0); // Offset downward

        // Convert the adjusted local positions back to world space
        Vector3 topTip = aerator.Transform.TransformPoint(localTopTip);
        Vector3 bottomTip = aerator.Transform.TransformPoint(localBottomTip);

        // Debug.Log($"TopTip: {topTip}, BottomTip: {bottomTip}, Capsule Rotation: {transform.rotation}");

        SetToolPower(aerator);
        computeShader.SetFloats(CapsuleToolA, topTip.x, topTip.y, topTip.z);
        computeShader.SetFloats(CapsuleToolB, bottomTip.x, bottomTip.y, bottomTip.z);
        computeShader.SetFloat(CapsuleToolRange, ts.x * 2.0f);

        computeShader.SetFloat(Scale, VoxelSize);
        computeShader.SetFloats(DestructiblePosition, dp.x, dp.y, dp.z);
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

    public void SaveState()
    {
        // 1) Save voxels
        string baseName = $"{_modelKey}_{DateTime.Now:yyyyMMdd_HHmmss}";
        string voxPath = Path.Combine(Application.persistentDataPath, baseName + ".vox");
        MeshSerializer.SaveAsync(baseName, _gridSize, _voxelSize, _voxelData3D, _voxelUV3D);
        Debug.Log($"Saved voxel cache to {voxPath}");

        // 2) Save mesh
        // Mesh mesh = builder.Mesh; // your built mesh
        // string objPath = Path.Combine(Application.persistentDataPath, baseName + ".obj");
        // RuntimeExporter.ExportToObjAsync(gameObject, objPath);
        // Debug.Log($"Exported mesh .obj to {objPath}");
    }

    private IEnumerator evaluate()
    {
        UIManager.Instance.ShowLoadingWindow("Analyzing");
        yield return null;
        // Get voxels & evaluate
        float[,,] voxels = new float[_gridSize.x, _gridSize.y, _gridSize.z];
        voxelBuffer.GetData(voxels, 0, 0, _gridSize.x * _gridSize.y * _gridSize.z);

        var parameters = LookupToothParameters();
        var voxelSize = LookupToothHeightInMillimeters() / _gridSize.y;
        var res = ToothEvaluator.Evaluate(_voxelData3D, voxels, _gridSize,
            parameters, voxelSize, -0.25f);

        // Visualize errors
        float[,,] errors = new float[_gridSize.x, _gridSize.y, _gridSize.z];
        for (int i = 0; i < _gridSize.x; i++)
        for (int j = 0; j < _gridSize.y; j++)
        for (int k = 0; k < _gridSize.z; k++)
            errors[i, j, k] = -1f;
        MeshVoxelizer.ApplySmoothing(ref res.Errors, ref errors, _gridSize);
        voxelBuffer.SetData(errors);
        computeShader.SetBuffer(computeShader.FindKernel("MeshReconstruction"), Voxels, voxelBuffer);
        BuildMesh();
        var errVisualizer = Instantiate(errorVisualizer, transform, false);
        errVisualizer.sharedMesh = MeshUtils.MakeReadableMeshCopy(_meshFilter.sharedMesh);

        // Visualize carved area
        float[,,] carvedArea = new float[_gridSize.x, _gridSize.y, _gridSize.z];
        for (int i = 0; i < _gridSize.x; i++)
        for (int j = 0; j < _gridSize.y; j++)
        for (int k = 0; k < _gridSize.z; k++)
            carvedArea[i, j, k] = -1f;
        MeshVoxelizer.ApplySmoothing(ref res.CarvedArea, ref carvedArea, _gridSize);
        voxelBuffer.SetData(carvedArea);
        computeShader.SetBuffer(computeShader.FindKernel("MeshReconstruction"), Voxels, voxelBuffer);
        BuildMesh();
        var carveVisualizer = Instantiate(carvedVisualizer, transform, false);
        carveVisualizer.sharedMesh = MeshUtils.MakeReadableMeshCopy(_meshFilter.sharedMesh);

        // Visualize carved distance (how far carved into tooth)
        voxelUVBuffer.SetData(res.DistanceUVs);
        computeShader.SetBuffer(computeShader.FindKernel("MeshReconstruction"), VoxelUV, voxelUVBuffer);
        voxelBuffer.SetData(voxels);
        computeShader.SetBuffer(computeShader.FindKernel("MeshReconstruction"), Voxels, voxelBuffer);
        BuildMesh();
        var distVisualizer = Instantiate(distanceVisualizer, transform, false);
        distVisualizer.sharedMesh = MeshUtils.MakeReadableMeshCopy(_meshFilter.sharedMesh);

        // restore tooth mesh
        voxelUVBuffer.SetData(_voxelUV3D);
        computeShader.SetBuffer(computeShader.FindKernel("MeshReconstruction"), VoxelUV, voxelUVBuffer);
        voxelBuffer.SetData(voxels);
        computeShader.SetBuffer(computeShader.FindKernel("MeshReconstruction"), Voxels, voxelBuffer);
        BuildMesh();
        UIManager.Instance.HideLoadingWindow();
        UIManager.Instance.CarvedVisualizer = carveVisualizer.gameObject;
        UIManager.Instance.UndercutVisualizer = errVisualizer.gameObject;
        UIManager.Instance.DistanceVisualizer = distVisualizer.gameObject;
        UIManager.Instance.ShowEvaluationWindow();
        InputModeManager.instance.OnEnterExitUINavigation(); // Hit the select key from code 
    }

    private float LookupToothHeightInMillimeters()
    {
        return gameObject.name switch
        {
            "LR1" => 20.2f,
            "LR2" => 18.9f,
            "LR3" => 21.9f,
            "LR4" => 19.0f,
            "LR5" => 19.1f,
            "LR6" => 18.2f,
            "LR7" => 17.7f,
            "LR8" => 14.6f,
            "LL1" => 20.2f,
            "LL2" => 18.8f,
            "LL3" => 22.0f,
            "LL4" => 19.9f,
            "LL5" => 19.2f,
            "LL6" => 18.3f,
            "LL7" => 17.0f,
            "LL8" => 14.0f,
            _ => 18f
        };
    }

    private ToothEvaluator.ToothParameters LookupToothParameters()
    {
        return gameObject.name switch
        {
            "LR1" or "LL1" => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 0.6f
            },
            "LR2" or "LL2" => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 0.6f
            },
            "LR3" or "LL3" => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 0.7f
            },
            "LR4" or "LL4" => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 0.8f
            },
            "LR5" or "LL5" => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 1.0f
            },
            "LR6" or "LL6" => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 1.0f
            },
            "LR7" or "LL7" => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 1.0f
            },
            "LR8" or "LL8" => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 1.0f
            },
            _ => new ToothEvaluator.ToothParameters
            {
                StartY = (int)(_gridSize.y * 0.7),
                TargetDistance = 1.0f
            },
        };
    }
}