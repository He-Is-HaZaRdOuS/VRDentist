using System;
using System.Collections.Generic;
using UnityEngine;

public enum AeratorType
{
    Sphere, Capsule
}

public class AeratorTip : MonoBehaviour
{
    [SerializeField] private AeratorType aeratorType;
    [SerializeField, Range(0, 1000)] private int power;
    [SerializeField] private GameObject debugCubePrefab;
    [SerializeField] private bool showDebugCubes;
    
    public Transform Transform => transform;
    public int Power => power;
    public AeratorType AeratorType => aeratorType;
    
    private readonly List<GameObject> _debugCubes = new();
    private readonly List<Vector3> _debugCubeOffsetDirs = new();
    private Tooth _tooth;
    
    public void Start()
    {
        _tooth = FindObjectOfType<Tooth>();
        if (_tooth == null || debugCubePrefab == null) return;
        _debugCubeOffsetDirs.Add(new Vector3(-1, 0, -1));
        _debugCubeOffsetDirs.Add(new Vector3(-1, 0, 1));
        _debugCubeOffsetDirs.Add(new Vector3(1, 0, -1));
        _debugCubeOffsetDirs.Add(new Vector3(1, 0, 1));
        for (var i = 0; i < _debugCubeOffsetDirs.Count; i++)
        {
            _debugCubes.Add(Instantiate(debugCubePrefab));
            _debugCubes[i].SetActive(showDebugCubes);
            _debugCubes[i].transform.localScale = new Vector3(_tooth.VoxelSize, _tooth.VoxelSize, _tooth.VoxelSize);
            // _debugCubes[i].transform.parent = transform;
        }
    }

    public void Update()
    {
        for (var i = 0; i < _debugCubes.Count; i++)
        {
            var debugCube = _debugCubes[i];
            debugCube.SetActive(showDebugCubes);
            debugCube.GetComponent<Renderer>().material.color = _tooth.IsCarvedThisFrame ? new Color(0.0f, 1.0f, 0.0f, 0.5f) : new Color(1.0f, 0.0f, 0.0f, 0.5f);
            var index = _tooth.GlobalPositionToVoxelIndex(transform.position);
            debugCube.transform.position = _tooth.transform.position + (index + _debugCubeOffsetDirs[i]) * _tooth.VoxelSize;
            debugCube.transform.position -= _tooth.CenterOffset;
            debugCube.transform.rotation = new Quaternion(0.5f,0.5f,0.5f,0.5f);
        }
    }
}
