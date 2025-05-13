using System;
using System.Collections.Generic;
using UnityEngine;

public class ToothEvaluator
{
    public struct EvaluationResult
    {
        public float Score;
        public float[,,] Errors;
        public float[,,] Improvements;
        public float[,,] CarvedArea;
        public Vector2[,,] DistanceUVs;
    }

    private struct LayerData
    {
        public bool IsEmpty;
        public Vector2 MinBounds;
        public Vector2 MaxBounds;
        public Vector2 BoundsCenter;
        public Vector2 CenterOfMass;
    }

    private struct ToothParameters
    {
        public int StartY;
        public float MinDist;
        public float MaxDist;
    }

    public static EvaluationResult Evaluate(float[,,] initialState, float[,,] voxels, Vector3Int gridSize, int minY,
        float voxelSize, float threshold = 0.1f)
    {
        /*
         * voxels[i,j,k] > 0                              filled
         * voxels[i,j,k] < 0                              empty
         * initialState[i,j,k] > 0 && voxels[i,j,k] < 0   carved
         */
        Vector2[,,] distanceUVs = new Vector2[gridSize.x, gridSize.y, gridSize.z];
        float[,,] errors = new float[gridSize.x, gridSize.y, gridSize.z];
        float[,,] improvements = new float[gridSize.x, gridSize.y, gridSize.z];
        float[,,] carvedArea = new float[gridSize.x, gridSize.y, gridSize.z];
        for (int x = 0; x < gridSize.x; x++)
        for (int y = 0; y < gridSize.y; y++)
        for (int z = 0; z < gridSize.z; z++)
        {
            errors[x, y, z] = -1f;
            improvements[x, y, z] = -1f;
            carvedArea[x, y, z] = -1f;
            distanceUVs[x, y, z].x = 0.5f;
        }

        int width = gridSize.x;
        int height = gridSize.y;
        int depth = gridSize.z;

        int layerCount = height - minY;
        LayerData[] layers = new LayerData[layerCount];
        int undercuts = 0;
        int layersUsed = 0;

        for (int y = height - 1; y >= minY; y--)
        {
            float minX = width;
            float maxX = 0;
            float minZ = depth;
            float maxZ = 0;
            Vector2 centerOfMass = Vector2.zero;
            int filledCount = 0;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (voxels[x, y, z] > threshold) // if voxel is filled
                    {
                        filledCount++;
                        centerOfMass += new Vector2(x, z);
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (z < minZ) minZ = z;
                        if (z > maxZ) maxZ = z;

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

                        // if the voxel above this one is marked as undercut, mark this one too
                        if (y + 1 < height && errors[x, y + 1, z] > 0.0f && voxels[x, y + 1, z] < threshold)
                        {
                            errors[x, y, z] = 1.0f;
                            undercuts++;
                        }
                    }
                }
            }

            if (filledCount == 0)
            {
                layers[y - minY].IsEmpty = true;
                continue;
            }

            layers[y - minY].MinBounds = new Vector2(minX, minZ);
            layers[y - minY].MaxBounds = new Vector2(maxX, maxZ);
            layers[y - minY].BoundsCenter = new Vector2(minX + (maxX - minX) / 2.0f, minZ + (maxZ - minZ) / 2.0f);
            layers[y - minY].CenterOfMass = centerOfMass / filledCount;
            layersUsed++;
        }

        for (int x = 0; x < gridSize.x; x++)
        for (int y = 0; y < gridSize.y; y++)
        for (int z = 0; z < gridSize.z; z++)
            if (initialState[x, y, z] > threshold && voxels[x, y, z] < threshold)
            {
                if (y < minY) errors[x, y, z] = 1.0f;
                carvedArea[x, y, z] = 1.0f;
            }

        for (int y = minY; y < gridSize.y - 1; y++)
        {
            var outerRing = new List<Vector2>();
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
                    initialState[x - 1, y, z - 1] < initialState[x, y, z]
                )
                {
                    outerRing.Add(new Vector2(x, z));
                    improvements[x, y, z] = 1.0f;
                }
            }

            for (int x = 1; x < gridSize.x - 1; x++)
            for (int z = 1; z < gridSize.z - 1; z++)
            {
                var minSquareDistance = 99999999.0f;
                foreach (var p in outerRing)
                {
                    var dist = (p.x - x) * (p.x - x) + (p.y - z) * (p.y - z);
                    if (dist < minSquareDistance)
                        minSquareDistance = dist;
                }

                // target: 1 millimeters
                const float targetDistance = 1.0f;
                var carvedDistance = voxelSize * (float)Math.Sqrt(minSquareDistance);
                var err = carvedDistance - targetDistance;
                err = Mathf.Clamp(err, -1.0f, 1.0f);
                err = (err + 1.0f) / 2.0f;
                distanceUVs[x, y, z].y = 0.5f;
                distanceUVs[x, y, z].x = err;
            }
        }


        // float alignmentScore = EvaluateSliceAlignment(layers);
        float score = Math.Min(100, 110 - undercuts);
        Debug.Log($"undercuts: {undercuts}");
        Debug.Log($"layersUsed: {layersUsed}");
        // Debug.Log($"alignmentScore: {alignmentScore}");
        Debug.Log($"score: {score}");

        return new EvaluationResult
        {
            Score = score,
            Errors = errors,
            Improvements = improvements,
            CarvedArea = carvedArea,
            DistanceUVs = distanceUVs
        };
    }

    private static float EvaluateSliceAlignment(LayerData[] layers, float maxSpread = 0.05f)
    {
        if (layers == null || layers.Length == 0)
            return 0f;

        int validLayers = 0;
        // 1. Compute centroid
        Vector2 centroid = Vector2.zero;
        foreach (var pt in layers)
        {
            if (pt.IsEmpty)
                continue;

            centroid += pt.BoundsCenter;
            validLayers++;
        }

        centroid /= validLayers;
        Debug.Log($"centroid: {centroid}");

        // 2. Compute mean squared distance from centroid
        float totalSquaredDistance = 0f;
        foreach (var pt in layers)
        {
            if (pt.IsEmpty)
                continue;

            float sqDist = (pt.BoundsCenter - centroid).sqrMagnitude;
            totalSquaredDistance += sqDist;
        }

        float meanSquaredDistance = totalSquaredDistance / validLayers;

        // 3. Normalize into score [0, 100]
        float normalized = Mathf.Clamp01(1f - (meanSquaredDistance / maxSpread));
        float score = normalized * 100f;

        return score;
    }
}