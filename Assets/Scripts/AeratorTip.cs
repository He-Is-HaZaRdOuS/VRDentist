using System.Collections.Generic;
using UnityEngine;

public enum AeratorType
{
    Sphere,
    Capsule
}

public class AeratorTip : MonoBehaviour
{
    [SerializeField] private AeratorType aeratorType;
    [SerializeField, Range(0, 1000)] private int power;
    [SerializeField] private GameObject debugCubePrefab;
    [SerializeField] private bool showDebugCubes;
    [SerializeField] private bool physicalRotationEnabled;

    public Transform Transform => transform;
    public int Power => power;
    public AeratorType AeratorType => aeratorType;

    private Tooth _tooth;
    private Vector3Int _lastIndex;

    // Debug cubes
    private readonly List<GameObject> _debugCubes = new();
    private readonly List<Renderer> _cubeRenderers = new();
    private readonly List<Transform> _cubeTransforms = new();
    private static readonly Quaternion _cubeRotation = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f);

    private readonly List<Vector3> _offsetDirs = new()
    {
        new Vector3(-1, 0, -1),
        new Vector3(-1, 0, 1),
        new Vector3(1, 0, -1),
        new Vector3(1, 0, 1)
    };

    void Start()
    {
        _tooth = FindObjectOfType<Tooth>();
        if (_tooth == null || debugCubePrefab == null)
            return;

        // Instantiate and cache cubes, renderers, transforms
        for (int i = 0; i < _offsetDirs.Count; i++)
        {
            var cube = Instantiate(debugCubePrefab);
            cube.transform.localScale = Vector3.one * _tooth.VoxelSize;
            cube.transform.rotation = _cubeRotation;
            cube.SetActive(showDebugCubes);

            _debugCubes.Add(cube);
            _cubeTransforms.Add(cube.transform);
            _cubeRenderers.Add(cube.GetComponent<Renderer>());
        }

        // Initialize first positions
        UpdateDebugCubes();

        physicalRotationEnabled = false;
    }

    void Update()
    {
        if (physicalRotationEnabled)
        {
            Transform.Rotate(Vector3.up * (Time.deltaTime * power * 2.0f));
        }
        
        if (_tooth is null || _debugCubes.Count == 0)
            return;

        // Show/hide once per frame if flag changed
        foreach (var t in _debugCubes)
        {
            t.SetActive(showDebugCubes);
        }

        if (!showDebugCubes)
            return;

        UpdateDebugCubes();
    }

    private void UpdateDebugCubes()
    {
        // Compute voxel index once
        Vector3 worldPos = transform.position;
        Vector3Int idx = _tooth.GlobalPositionToVoxelIndex(worldPos);
        if (idx == _lastIndex)
            return; // no change
        _lastIndex = idx;

        // Determine color based on carve state
        Color color = _tooth.IsCarvedThisFrame
            ? new Color(0f, 1f, 0f, 0.5f)
            : new Color(1f, 0f, 0f, 0.5f);

        Vector3 baseOffset = _tooth.transform.position - _tooth.CenterOffset;
        float voxelSize = _tooth.VoxelSize;

        for (int i = 0; i < _debugCubes.Count; i++)
        {
            // Update renderer color
            var rend = _cubeRenderers[i];
            if (rend is not null)
                rend.material.color = color;

            // Position cube at voxel neighbor
            Vector3 offset = (_offsetDirs[i] + idx) * voxelSize;
            _cubeTransforms[i].position = baseOffset + offset;
            _cubeTransforms[i].rotation = _cubeRotation; // static
        }
    }
}