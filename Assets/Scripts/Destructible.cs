using System;
using System.Collections;
using System.Collections.Generic;
using MarchingCubes;
using UnityEditor;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(IVoxelDataProvider))]
public class Destructible : MonoBehaviour
{
    [SerializeField] private GameObject tool = null;
    [SerializeField] private GameObject capsuleTool = null;
    [SerializeField] private Vector3Int size = new(32, 32, 32);

    [SerializeField] private ComputeShader cubeMarcher = null;
    [SerializeField, Range(1, 8)] private int scale = 3;
    [SerializeField] private float SurfaceLevel = 0f;
    private ComputeBuffer voxelBuffer; // GPU
    private MeshBuilder builder;

    public void Start()
    {
        var voxelData = GetComponent<IVoxelDataProvider>().GetVoxelData(size);
        voxelBuffer = new ComputeBuffer(size.x * size.y * size.z, sizeof(float));
        voxelBuffer.SetData(voxelData);
        cubeMarcher.SetBuffer(2, "Voxels", voxelBuffer);
        cubeMarcher.SetBuffer(3, "Voxels", voxelBuffer);
        builder = new MeshBuilder(size, 4 * size.x * size.y * size.z, cubeMarcher);

        BuildMesh();
    }

    public void Update()
    {
        if (tool != null)
        {
            Carve();
        }
        if (capsuleTool != null)
        {
            CarveCapsule();
        }
        BuildMesh();
    }

    public void OnDestroy()
    {
        builder.Dispose();
        voxelBuffer.Dispose();
    }

    private void Carve()
    {
        float _scale = (float)(Math.Pow(2, scale) / 256);
        var tp = tool.transform.position;
        var dp = transform.position - new Vector3(size.x, size.y, size.z) / 2f * _scale;
        cubeMarcher.SetFloats("ToolPosition", tp.x, tp.y, tp.z);
        cubeMarcher.SetFloat("ToolRange", 0.125f);
        cubeMarcher.SetFloat("Scale", _scale);
        cubeMarcher.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        cubeMarcher.DispatchThreads(2, size.x, size.y, size.z);
    }
    private void CarveCapsule()
    {
        float _scale = (float)(Math.Pow(2, scale) / 256);
        var tp = capsuleTool.transform.position;
        var dp = transform.position - new Vector3(size.x, size.y, size.z) / 2f * _scale;
        cubeMarcher.SetFloat("Scale", _scale);
        cubeMarcher.SetFloats("DestructiblePosition", dp.x, dp.y, dp.z);
        cubeMarcher.SetFloats("capsuleToolA", tp.x, tp.y + 0.0625f, tp.z);
        cubeMarcher.SetFloats("capsuleToolB", tp.x, tp.y - 0.0625f, tp.z);
        cubeMarcher.SetFloat("capsuleToolRange", 0.125f);
        cubeMarcher.DispatchThreads(3, size.x, size.y, size.z);
    }

    private void BuildMesh()
    {
        builder.BuildIsosurface(voxelBuffer, SurfaceLevel, (float)(Math.Pow(2, scale) / 256));
        GetComponent<MeshFilter>().sharedMesh = builder.Mesh;
    }
}
