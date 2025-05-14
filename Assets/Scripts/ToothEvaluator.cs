using System;
using System.Collections.Generic;
using UnityEngine;

public class ToothEvaluator
{
    public struct EvaluationResult
    {
        public float Score;
        public float[,,] Errors;
        public float[,,] CarvedArea;
        public Vector2[,,] DistanceUVs;
    }

    public struct ToothParameters
    {
        public int StartY;

        /// In millimeters
        public float TargetDistance;
    }

    public static EvaluationResult Evaluate(float[,,] initialState, float[,,] voxels, Vector3Int gridSize,
        ToothParameters tParams, float voxelSize, float threshold = 0.1f)
    {
        /*
         * voxels[i,j,k] > 0                              filled
         * voxels[i,j,k] < 0                              empty
         * initialState[i,j,k] > 0 && voxels[i,j,k] < 0   carved
         */
        Vector2[,,] distanceUVs = new Vector2[gridSize.x, gridSize.y, gridSize.z];
        float[,,] errors = new float[gridSize.x, gridSize.y, gridSize.z];
        float[,,] carvedArea = new float[gridSize.x, gridSize.y, gridSize.z];
        for (int x = 0; x < gridSize.x; x++)
        for (int y = 0; y < gridSize.y; y++)
        for (int z = 0; z < gridSize.z; z++)
        {
            errors[x, y, z] = -1f;
            carvedArea[x, y, z] = -1f;
            distanceUVs[x, y, z].x = 0.5f;
        }

        int width = gridSize.x;
        int height = gridSize.y;
        int depth = gridSize.z;


        int undercuts = 0;
        // Detects undercuts && fills carved area buffer
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (voxels[x, y, z] > threshold) // if voxel is filled
                    {
                        // if the voxel under this one is carved
                        if (y - 1 >= 0 && voxels[x, y - 1, z] < threshold && initialState[x, y - 1, z] > threshold)
                        {
                            errors[x, y - 1, z] = 1.0f;
                            undercuts++;
                        }
                    }
                    else // if voxel is empty
                    {
                        // continue if it was empty initially (not carved)
                        if (initialState[x, y, z] < threshold) continue;

                        carvedArea[x, y, z] = 1.0f;
                        if (y < tParams.StartY)
                        {
                            errors[x, y, z] = 1.0f;
                            continue;
                        }

                        // if the voxel above this one is marked as undercut, mark this one too
                        if (y + 1 < height && errors[x, y + 1, z] > 0.0f && voxels[x, y + 1, z] < threshold)
                        {
                            errors[x, y, z] = 1.0f;
                            undercuts++;
                        }
                    }
                }
            }
        }

        // Create a shell from initial state
        List<List<Vector2>> shell = new List<List<Vector2>>();
        for (int y = tParams.StartY; y < gridSize.y - 1; y++)
        {
            var ring = new List<Vector2>();
            for (int x = 1; x < gridSize.x - 1; x++)
            for (int z = 1; z < gridSize.z - 1; z++)
            {
                if (initialState[x, y, z] < threshold) continue;
                if (
                    initialState[x + 1, y, z] < initialState[x, y, z] ||
                    initialState[x, y, z + 1] < initialState[x, y, z] ||
                    initialState[x - 1, y, z] < initialState[x, y, z] ||
                    initialState[x, y, z - 1] < initialState[x, y, z] ||
                    initialState[x + 1, y, z + 1] < initialState[x, y, z] ||
                    initialState[x + 1, y, z - 1] < initialState[x, y, z] ||
                    initialState[x - 1, y, z + 1] < initialState[x, y, z] ||
                    initialState[x - 1, y, z - 1] < initialState[x, y, z] ||
                    initialState[x, y + 1, z] < initialState[x, y, z]
                )
                    ring.Add(new Vector2(x, z));
            }

            shell.Add(ring);
        }

        // Calculates each voxel's distance to the shell
        for (int y = tParams.StartY; y < gridSize.y - 1; y++)
        {
            for (int x = 1; x < gridSize.x - 1; x++)
            for (int z = 1; z < gridSize.z - 1; z++)
            {
                if (initialState[x, y, z] < -0.5) continue;
                var down = 1;
                var up = 1;
                if (y - tParams.StartY < (gridSize.y - tParams.StartY) * 0.75f)
                {
                    up = 0;
                    down = 0;
                }

                var minSquareDistance = 99999999.0f;
                for (int lookupHeight = -down;
                     lookupHeight <= up &&
                     y + lookupHeight - tParams.StartY > 0 &&
                     y + lookupHeight - tParams.StartY < shell.Count;
                     lookupHeight++)
                {
                    foreach (var p in shell[y + lookupHeight - tParams.StartY])
                    {
                        var dist = (p.x - x) * (p.x - x) + (p.y - z) * (p.y - z);
                        if (dist < minSquareDistance)

                            minSquareDistance = dist;
                    }
                }

                var carvedDistance = voxelSize * (float)Math.Sqrt(minSquareDistance);
                var err = carvedDistance - tParams.TargetDistance;
                err = Mathf.Clamp(err, -0.95f, 0.95f);
                err = (err + 1.0f) / 2.0f;
                distanceUVs[x, y, z].y = 0.5f;
                distanceUVs[x, y, z].x = err;
            }
        }


        float score = Math.Min(100, 110 - undercuts);
        return new EvaluationResult
        {
            Score = score,
            Errors = errors,
            CarvedArea = carvedArea,
            DistanceUVs = distanceUVs
        };
    }
}