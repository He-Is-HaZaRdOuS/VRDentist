using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IVoxelDataProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="gridSize">Number of voxels in each axis</param>
    /// <returns>3D array of floats where positive values represent a shape</returns>
    float[,,] GetVoxelData(Vector3Int gridSize);
}