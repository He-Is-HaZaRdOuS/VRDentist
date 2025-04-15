using UnityEngine;

public class MeshVoxelizer : MonoBehaviour
{
    public struct Voxel_t
    {
        public Vector3 position;
        public Vector2 uv;
        public uint fill;
        public uint front;

        public bool IsFrontFace()
        {
            return fill > 0 && front > 0;
        }

        public bool IsBackFace()
        {
            return fill > 0 && front > 0;
        }

        public bool IsEmpty()
        {
            return fill < 1;
        }
    }
    public class Triangle
    {
        public Vector3 a, b, c;
        public Bounds bounds;
        public bool frontFacing;

        public Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 dir)
        {
            this.a = a;
            this.b = b;
            this.c = c;

            var cross = Vector3.Cross(b - a, c - a);
            this.frontFacing = (Vector3.Dot(cross, dir) <= 0f);

            var min = Vector3.Min(Vector3.Min(a, b), c);
            var max = Vector3.Max(Vector3.Max(a, b), c);
            bounds.SetMinMax(min, max);
        }

        public Vector2 GetUV(Vector3 p, Vector2 uva, Vector2 uvb, Vector2 uvc)
        {
            float u, v, w;
            Barycentric(p, out u, out v, out w);
            return uva * u + uvb * v + uvc * w;
        }

        // https://gamedev.stackexchange.com/questions/23743/whats-the-most-efficient-way-to-find-barycentric-coordinates
        public void Barycentric(Vector3 p, out float u, out float v, out float w)
        {
            Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = 1f / (d00 * d11 - d01 * d01);
            v = (d11 * d20 - d01 * d21) * denom;
            w = (d00 * d21 - d01 * d20) * denom;
            u = 1.0f - v - w;
        }

    }

    // http://blog.wolfire.com/2009/11/Triangle-mesh-voxelization
    public static float[,,] Voxelize(Mesh mesh, ref Vector3Int gridSize, ref float voxelSize, float resolution, ref Vector2[,,]texCoords)
    {
        mesh.RecalculateBounds();

        var bounds = mesh.bounds;

        resolution -= 4;
        var maxLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        voxelSize = maxLength / resolution;

        var start = bounds.min - 2*new Vector3(voxelSize, voxelSize, voxelSize);
        var end = bounds.max + new Vector3(voxelSize, voxelSize, voxelSize);
        var size = end - start;
        
        gridSize.x = Mathf.CeilToInt(size.x / voxelSize) + 4;
        gridSize.y = Mathf.CeilToInt(size.y / voxelSize) + 4;
        gridSize.z = Mathf.CeilToInt(size.z / voxelSize) + 4;
        
        var volume = new Voxel_t[gridSize.x, gridSize.y, gridSize.z];
        var boxes = new Bounds[gridSize.x, gridSize.y, gridSize.z];
        var voxelSizeVec3 = Vector3.one * voxelSize;
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    var p = new Vector3(x, y, z) * voxelSize + start;
                    var aabb = new Bounds(p, voxelSizeVec3);
                    boxes[x, y, z] = aabb;
                }
            }
        }

        // build triangles
        var vertices = mesh.vertices;
        var uvs = mesh.uv;
        var uv00 = Vector2.zero;
        var indices = mesh.triangles;
        var direction = Vector3.forward;

        for (int i = 0, n = indices.Length; i < n; i += 3)
        {
            var tri = new Triangle(
                vertices[indices[i]],
                vertices[indices[i + 1]],
                vertices[indices[i + 2]],
                direction
            );

            Vector2 uva, uvb, uvc;
            if (uvs.Length > 0)
            {
                uva = uvs[indices[i]];
                uvb = uvs[indices[i + 1]];
                uvc = uvs[indices[i + 2]];
            }
            else
            {
                uva = uvb = uvc = uv00;
                Debug.Log("HERE");
            }

            var min = tri.bounds.min - start;
            var max = tri.bounds.max - start;
            // int iminX = Mathf.RoundToInt(min.x / voxelSize), iminY = Mathf.RoundToInt(min.y / voxelSize), iminZ = Mathf.RoundToInt(min.z / voxelSize);
            // int imaxX = Mathf.RoundToInt(max.x / voxelSize), imaxY = Mathf.RoundToInt(max.y / voxelSize), imaxZ = Mathf.RoundToInt(max.z / voxelSize);
            int iminX = Mathf.FloorToInt(min.x / voxelSize), iminY = Mathf.FloorToInt(min.y / voxelSize), iminZ = Mathf.FloorToInt(min.z / voxelSize);
            int imaxX = Mathf.CeilToInt(max.x / voxelSize), imaxY = Mathf.CeilToInt(max.y / voxelSize), imaxZ = Mathf.CeilToInt(max.z / voxelSize);

            iminX = Mathf.Clamp(iminX, 0, gridSize.x - 1);
            iminY = Mathf.Clamp(iminY, 0, gridSize.y - 1);
            iminZ = Mathf.Clamp(iminZ, 0, gridSize.z - 1);
            imaxX = Mathf.Clamp(imaxX, 0, gridSize.x - 1);
            imaxY = Mathf.Clamp(imaxY, 0, gridSize.y - 1);
            imaxZ = Mathf.Clamp(imaxZ, 0, gridSize.z - 1);

            // Debug.Log((iminX + "," + iminY + "," + iminZ) + " ~ " + (imaxX + "," + imaxY + "," + imaxZ));

            uint front = (uint)(tri.frontFacing ? 1 : 0);

            for (int x = iminX; x <= imaxX; x++)
            {
                for (int y = iminY; y <= imaxY; y++)
                {
                    for (int z = iminZ; z <= imaxZ; z++)
                    {
                        volume[x, y, z].uv = tri.GetUV(volume[x, y, z].position, uva, uvb, uvc);
                        if (Intersects(tri, boxes[x, y, z]))
                        {
                            var voxel = volume[x, y, z];
                            voxel.position = boxes[x, y, z].center;
                            // voxel.uv = tri.GetUV(voxel.position, uva, uvb, uvc);
                            if ((voxel.fill & 1) == 0)
                            {
                                voxel.front = front;
                            }
                            else
                            {
                                voxel.front = voxel.front & front;
                            }
                            voxel.fill = voxel.fill | 1;
                            volume[x, y, z] = voxel;
                        }
                    }
                }
            }
        }

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    if (volume[x, y, z].IsEmpty()) continue;

                    int ifront = z;

                    Vector2 uv = Vector2.zero;
                    for (; ifront < gridSize.z; ifront++)
                    {
                        if (!volume[x, y, ifront].IsFrontFace())
                        {
                            break;
                        }
                        uv = volume[x, y, ifront].uv;
                    }

                    if (ifront >= gridSize.z) break;

                    var iback = ifront;

                    // step forward to cavity
                    for (; iback < gridSize.z && volume[x, y, iback].IsEmpty(); iback++) { }

                    if (iback >= gridSize.z) break;

                    // check if iback is back voxel
                    if (volume[x, y, iback].IsBackFace())
                    {
                        // step forward to back face
                        for (; iback < gridSize.z && volume[x, y, iback].IsBackFace(); iback++) { }
                    }

                    // fill from ifront to iback
                    for (int z2 = ifront; z2 < iback; z2++)
                    {
                        var p = boxes[x, y, z2].center;
                        var voxel = volume[x, y, z2];
                        voxel.position = p;
                        voxel.uv = uv;
                        voxel.fill = 1;
                        volume[x, y, z2] = voxel;
                    }

                    z = iback;
                }
            }
        }
        
        var emptyVoxelCount = 0;
        var voxels = new float[gridSize.x, gridSize.y, gridSize.z];
        texCoords = new Vector2[gridSize.x, gridSize.y, gridSize.z];
        for (var x = 0; x < gridSize.x; x++)
            for (var y = 0; y < gridSize.y; y++)
                for (var z = 0; z < gridSize.z; z++)
                {
                    voxels[x, y, z] = volume[x, y, z].IsEmpty() ? -1f : 1f;
                    texCoords[x,y,z] = volume[x, y, z].uv;
                    if (volume[x, y, z].IsEmpty())
                        emptyVoxelCount++;
                }
     
        var numVoxels = gridSize.x * gridSize.y * gridSize.z;
        print($"Generated a {gridSize.x}x{gridSize.y}x{gridSize.z} voxel grid ({numVoxels} voxels)");
        print($"Empty voxel count: {emptyVoxelCount} ({100.0f * emptyVoxelCount / numVoxels}% of all voxels)");
        return voxels;
    }

    public static float[,,] SmoothVoxelize(Mesh mesh, ref Vector3Int gridSize, ref float voxelSize, int resolution, int smoothingLevel, ref Vector2[,,]texCoords)
    {
        float[,,] voxels = Voxelize(mesh, ref gridSize,ref voxelSize, resolution, ref texCoords);
        if (smoothingLevel <= 0)
            return voxels;
        
        float[,,] smoothVoxels = new float[gridSize.x, gridSize.y, gridSize.z];
        for (int i = 0; i < gridSize.x; i++)
            for (int j = 0; j < gridSize.y; j++)
                for (int k = 0; k < gridSize.z; k++)
                    smoothVoxels[i, j, k] = -1f;

        for (int i = 0; i < smoothingLevel; i++)
        {
            if (i % 2 == 0)
            {
                ApplySmoothing(ref voxels, ref smoothVoxels, gridSize);
            }
            else
            {
                ApplySmoothing(ref smoothVoxels, ref voxels, gridSize);
            }

        }

        return smoothingLevel % 2 == 0 ? voxels : smoothVoxels;
    }

    private static void ApplySmoothing(ref float[,,] source, ref float[,,] dest, Vector3Int gridSize)
    {
        for (int x = 1; x < gridSize.x - 1; x++)
        {
            for (int y = 1; y < gridSize.y - 1; y++)
            {
                for (int z = 1; z < gridSize.z - 1; z++)
                {
                    var sum = 0f;
                    for (int i = -1; i < 2; i++)
                        for (int j = -1; j < 2; j++)
                            for (int k = -1; k < 2; k++)
                                if (i == 0 || j == 0 || k == 0)
                                    sum += source[x + i, y + j, z + k] * 2;
                                else
                                    sum += source[x + i, y + j, z + k];
                    dest[x, y, z] = sum / 54f;
                }
            }
        }
    }

    public static bool Intersects(Triangle tri, Bounds aabb)
    {
        float p0, p1, p2, r;

        Vector3 center = aabb.center, extents = aabb.max - center;

        Vector3 v0 = tri.a - center,
            v1 = tri.b - center,
            v2 = tri.c - center;

        Vector3 f0 = v1 - v0,
            f1 = v2 - v1,
            f2 = v0 - v2;

        Vector3 a00 = new Vector3(0, -f0.z, f0.y),
            a01 = new Vector3(0, -f1.z, f1.y),
            a02 = new Vector3(0, -f2.z, f2.y),
            a10 = new Vector3(f0.z, 0, -f0.x),
            a11 = new Vector3(f1.z, 0, -f1.x),
            a12 = new Vector3(f2.z, 0, -f2.x),
            a20 = new Vector3(-f0.y, f0.x, 0),
            a21 = new Vector3(-f1.y, f1.x, 0),
            a22 = new Vector3(-f2.y, f2.x, 0);

        // Test axis a00
        p0 = Vector3.Dot(v0, a00);
        p1 = Vector3.Dot(v1, a00);
        p2 = Vector3.Dot(v2, a00);
        r = extents.y * Mathf.Abs(f0.z) + extents.z * Mathf.Abs(f0.y);

        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        // Test axis a01
        p0 = Vector3.Dot(v0, a01);
        p1 = Vector3.Dot(v1, a01);
        p2 = Vector3.Dot(v2, a01);
        r = extents.y * Mathf.Abs(f1.z) + extents.z * Mathf.Abs(f1.y);

        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        // Test axis a02
        p0 = Vector3.Dot(v0, a02);
        p1 = Vector3.Dot(v1, a02);
        p2 = Vector3.Dot(v2, a02);
        r = extents.y * Mathf.Abs(f2.z) + extents.z * Mathf.Abs(f2.y);

        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        // Test axis a10
        p0 = Vector3.Dot(v0, a10);
        p1 = Vector3.Dot(v1, a10);
        p2 = Vector3.Dot(v2, a10);
        r = extents.x * Mathf.Abs(f0.z) + extents.z * Mathf.Abs(f0.x);
        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        // Test axis a11
        p0 = Vector3.Dot(v0, a11);
        p1 = Vector3.Dot(v1, a11);
        p2 = Vector3.Dot(v2, a11);
        r = extents.x * Mathf.Abs(f1.z) + extents.z * Mathf.Abs(f1.x);

        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        // Test axis a12
        p0 = Vector3.Dot(v0, a12);
        p1 = Vector3.Dot(v1, a12);
        p2 = Vector3.Dot(v2, a12);
        r = extents.x * Mathf.Abs(f2.z) + extents.z * Mathf.Abs(f2.x);

        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        // Test axis a20
        p0 = Vector3.Dot(v0, a20);
        p1 = Vector3.Dot(v1, a20);
        p2 = Vector3.Dot(v2, a20);
        r = extents.x * Mathf.Abs(f0.y) + extents.y * Mathf.Abs(f0.x);

        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        // Test axis a21
        p0 = Vector3.Dot(v0, a21);
        p1 = Vector3.Dot(v1, a21);
        p2 = Vector3.Dot(v2, a21);
        r = extents.x * Mathf.Abs(f1.y) + extents.y * Mathf.Abs(f1.x);

        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        // Test axis a22
        p0 = Vector3.Dot(v0, a22);
        p1 = Vector3.Dot(v1, a22);
        p2 = Vector3.Dot(v2, a22);
        r = extents.x * Mathf.Abs(f2.y) + extents.y * Mathf.Abs(f2.x);

        if (Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r)
        {
            return false;
        }

        if (Mathf.Max(v0.x, v1.x, v2.x) < -extents.x || Mathf.Min(v0.x, v1.x, v2.x) > extents.x)
        {
            return false;
        }

        if (Mathf.Max(v0.y, v1.y, v2.y) < -extents.y || Mathf.Min(v0.y, v1.y, v2.y) > extents.y)
        {
            return false;
        }

        if (Mathf.Max(v0.z, v1.z, v2.z) < -extents.z || Mathf.Min(v0.z, v1.z, v2.z) > extents.z)
        {
            return false;
        }

        var normal = Vector3.Cross(f1, f0).normalized;
        var pl = new Plane(normal, Vector3.Dot(normal, tri.a));
        return Intersects(pl, aabb);
    }

    public static bool Intersects(Plane pl, Bounds aabb)
    {
        Vector3 center = aabb.center;
        var extents = aabb.max - center;

        var r = extents.x * Mathf.Abs(pl.normal.x) + extents.y * Mathf.Abs(pl.normal.y) + extents.z * Mathf.Abs(pl.normal.z);
        var s = Vector3.Dot(pl.normal, center) - pl.distance;

        return Mathf.Abs(s) <= r;
    }
}

