using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Shape
{
    Sphere, Cube, RandomFill
}

public class SDF : MonoBehaviour, IVoxelDataProvider
{
    public Shape shape = Shape.Sphere;

    public float[,,] GetVoxelData(Vector3Int voxelCount)
    {
        float[,,] data = new float[voxelCount.x, voxelCount.y, voxelCount.z];
        switch (shape)
        {
            case Shape.Sphere: Sphere(data, voxelCount); break;
            case Shape.Cube: Cube(data, voxelCount); break;
            case Shape.RandomFill: RandomFill(data, voxelCount); break;
            default: break;
        };

        return data;
    }

    private float[,,] Sphere(float[,,] data, Vector3Int voxelCount)
    {
        var radius = (Math.Min(Math.Min(voxelCount.x, voxelCount.y), voxelCount.z) - 1) / 2f;
        var center = new Vector3(voxelCount.x / 2f, voxelCount.y / 2f, voxelCount.z / 2f);
        center -= new Vector3(.5f, .5f, .5f);
        for (int x = 0; x < voxelCount.x; x++)
        {
            for (int y = 0; y < voxelCount.y; y++)
            {
                for (int z = 0; z < voxelCount.z; z++)
                {
                    data[x, y, z] = radius - (new Vector3(x, y, z) - center).magnitude;
                }
            }
        }
        return data;
    }

    private float[,,] Cube(float[,,] data, Vector3Int voxelCount)
    {
        for (int x = 0; x < voxelCount.x; x++)
        {
            for (int y = 0; y < voxelCount.y; y++)
            {
                for (int z = 0; z < voxelCount.z; z++)
                {
                    if (x == 0 || x == voxelCount.x - 1 ||
                        y == 0 || y == voxelCount.y - 1 ||
                        z == 0 || z == voxelCount.z - 1
                    )
                        data[x, y, z] = -1f;
                    else
                        data[x, y, z] = 1f;
                }
            }
        }

        return data;
    }

    private float[,,] RandomFill(float[,,] data, Vector3Int voxelCount)
    {
        for (int x = 0; x < voxelCount.x; x++)
        {
            for (int y = 0; y < voxelCount.y; y++)
            {
                for (int z = 0; z < voxelCount.z; z++)
                {
                    data[x, y, z] = UnityEngine.Random.Range(-1f, 1f);
                }
            }
        }
        return data;
    }
}
