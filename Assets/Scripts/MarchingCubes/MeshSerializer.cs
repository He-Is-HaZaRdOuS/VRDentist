using System.IO;
using UnityEngine;

namespace MarchingCubes
{
    public static class MeshSerializer
    {
        public static void Save(string key, Vector3Int gridSize, float voxelSize,
                                float[,,] voxels, Vector2[,,] texCoords)
        {
            string path = Path.Combine(Application.persistentDataPath, key + ".vox");
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            // header
            bw.Write(gridSize.x); bw.Write(gridSize.y); bw.Write(gridSize.z);
            bw.Write(voxelSize);

            int count = gridSize.x * gridSize.y * gridSize.z;
            // densities
            for (int i = 0; i < count; i++)
            {
                int x = i % gridSize.x;
                int y = (i / gridSize.x) % gridSize.y;
                int z = i / (gridSize.x * gridSize.y);
                bw.Write(voxels[x,y,z]);
            }
            // UVs
            for (int i = 0; i < count; i++)
            {
                int x = i % gridSize.x;
                int y = (i / gridSize.x) % gridSize.y;
                int z = i / (gridSize.x * gridSize.y);
                var uv = texCoords[x,y,z];
                bw.Write(uv.x);
                bw.Write(uv.y);
            }
        }

        // Returns false if no cache found
        public static bool Load(string key, out Vector3Int gridSize, out float voxelSize,
                                out float[,,] voxels, out Vector2[,,] texCoords)
        {
            string path = Path.Combine(Application.persistentDataPath, key + ".vox");
            if (!File.Exists(path))
            {
                gridSize = Vector3Int.zero;
                voxelSize = 0;
                voxels = null;
                texCoords = null;
                return false;
            }

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            int x = br.ReadInt32();
            int y = br.ReadInt32();
            int z = br.ReadInt32();
            gridSize = new Vector3Int(x, y, z);
            voxelSize = br.ReadSingle();

            int count = x * y * z;
            voxels    = new float[x,y,z];
            texCoords = new Vector2[x,y,z];

            for (int i = 0; i < count; i++)
            {
                int xi = i % x;
                int yi = (i / x) % y;
                int zi = i / (x * y);
                voxels[xi,yi,zi] = br.ReadSingle();
            }
            for (int i = 0; i < count; i++)
            {
                int xi = i % x;
                int yi = (i / x) % y;
                int zi = i / (x * y);
                texCoords[xi,yi,zi] = new Vector2(br.ReadSingle(), br.ReadSingle());
            }

            return true;
        }
    }
}