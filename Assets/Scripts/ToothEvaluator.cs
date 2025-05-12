using System;
using UnityEngine;

public class ToothEvaluator
{
    public struct EvaluationResult
    {
        public float Score;
        public float[,,] Errors;
        public float[,,] Improvements;
    }

    private struct LayerData
    {
        public bool IsEmpty;
        public Vector2 MinBounds;
        public Vector2 MaxBounds;
        public Vector2 BoundsCenter;
        public Vector2 CenterOfMass;
    }

    public static EvaluationResult Evaluate(float[,,] voxels, Vector3Int gridSize, int minY, float threshold = 0.1f)
    {
        float[,,] errors = new float[gridSize.x, gridSize.y, gridSize.z];
        float[,,] improvements = new float[gridSize.x, gridSize.y, gridSize.z];
        for (int x = 0; x < gridSize.x; x++)
        for (int y = 0; y < gridSize.y; y++)
        for (int z = 0; z < gridSize.z; z++)
        {
            errors[x, y, z] = -1f;
            improvements[x, y, z] = -1f;
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
                    if (voxels[x, y, z] > threshold)
                    {
                        filledCount++;
                        centerOfMass += new Vector2(x, z);
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (z < minZ) minZ = z;
                        if (z > maxZ) maxZ = z;

                        if (voxels[x, y - 1, z] < threshold)
                        {
                            errors[x, y - 1, z] = 1.0f;
                            undercuts++;
                        }
                    }
                    else
                    {
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

        for (int y = height - 1; y >= minY; y--)
        {
            if (layers[y - minY].IsEmpty) continue;
            var radius = Math.Min(layers[y - minY].MaxBounds.x - layers[y - minY].MinBounds.x,
                layers[y - minY].MaxBounds.y - layers[y - minY].MinBounds.y) / 2.0f;
            var center = layers[y - minY].BoundsCenter;
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (
                        (center.x - x) * (center.x - x) + (center.y - z) * (center.y - z) < radius * radius &&
                        voxels[x, y, z] < threshold
                    )
                    {
                        if (errors[x, y, z] < 0.0f)
                            improvements[x, y, z] = 1.0f;
                    }
                }
            }
        }

        float alignmentScore = EvaluateSliceAlignment(layers);
        float score = Math.Min(100, 110 - undercuts);
        Debug.Log($"undercuts: {undercuts}");
        Debug.Log($"layersUsed: {layersUsed}");
        Debug.Log($"alignmentScore: {alignmentScore}");
        Debug.Log($"score: {score}");

        return new EvaluationResult
        {
            Score = score,
            Errors = errors,
            Improvements = improvements,
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