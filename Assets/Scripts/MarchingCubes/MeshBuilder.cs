using UnityEngine;
using UnityEngine.Rendering;

namespace MarchingCubes
{

    //
    // Isosurface mesh builder with the marching cubes algorithm
    //
    sealed class MeshBuilder : System.IDisposable
    {
        private static readonly int MaxTriangle = Shader.PropertyToID("MaxTriangle");
        private static readonly int Scale = Shader.PropertyToID("Scale");
        private static readonly int Isovalue = Shader.PropertyToID("Isovalue");
        private static readonly int TriangleTable = Shader.PropertyToID("TriangleTable");
        private static readonly int Voxels = Shader.PropertyToID("Voxels");
        private static readonly int VertexBuffer = Shader.PropertyToID("VertexBuffer");
        private static readonly int IndexBuffer = Shader.PropertyToID("IndexBuffer");
        private static readonly int Counter = Shader.PropertyToID("Counter");

        #region Public members

        public Mesh Mesh => _mesh;

        public MeshBuilder(int x, int y, int z, int budget, ComputeShader compute)
            => Initialize((x, y, z), budget, compute);

        public MeshBuilder(Vector3Int dims, int budget, ComputeShader compute)
            => Initialize((dims.x, dims.y, dims.z), budget, compute);

        public void Dispose() => ReleaseAll();

        public void BuildIsosurface(ComputeBuffer voxels, float target, float scale)
            => RunCompute(voxels, target, scale);

        #endregion

        #region Private members

        (int x, int y, int z) _grids;
        int _triangleBudget;
        ComputeShader _compute;

        void Initialize((int, int, int) dims, int budget, ComputeShader compute)
        {
            _grids = dims;
            _triangleBudget = budget;
            _compute = compute;

            AllocateBuffers();
            AllocateMesh(3 * _triangleBudget);
        }

        void ReleaseAll()
        {
            ReleaseBuffers();
            ReleaseMesh();
        }

        void RunCompute(ComputeBuffer voxels, float target, float scale)
        {
            _counterBuffer.SetCounterValue(0);

            // Isosurface reconstruction
            _compute.SetInts("Dims", _grids);
            _compute.SetInt(MaxTriangle, _triangleBudget);
            _compute.SetFloat(Scale, scale);
            _compute.SetFloat(Isovalue, target);
            _compute.SetBuffer(0, TriangleTable, _triangleTable);
            _compute.SetBuffer(0, Voxels, voxels);
            _compute.SetBuffer(0, VertexBuffer, _vertexBuffer);
            _compute.SetBuffer(0, IndexBuffer, _indexBuffer);
            _compute.SetBuffer(0, Counter, _counterBuffer);
            _compute.DispatchThreads(0, _grids);

            // Clear unused area of the buffers.
            _compute.SetBuffer(1, VertexBuffer, _vertexBuffer);
            _compute.SetBuffer(1, IndexBuffer, _indexBuffer);
            _compute.SetBuffer(1, Counter, _counterBuffer);
            _compute.DispatchThreads(1, 1024, 1, 1);

            // Bounding box
            var ext = new Vector3(_grids.x, _grids.y, _grids.z) * scale;
            _mesh.bounds = new Bounds(Vector3.zero, ext);
        }

        #endregion

        #region Compute buffer objects

        ComputeBuffer _triangleTable;
        ComputeBuffer _counterBuffer;

        void AllocateBuffers()
        {
            // Marching cubes triangle table
            _triangleTable = new ComputeBuffer(256, sizeof(ulong));
            _triangleTable.SetData(PrecalculatedData.TriangleTable);

            // Buffer for triangle counting
            _counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
        }

        void ReleaseBuffers()
        {
            _triangleTable.Dispose();
            _counterBuffer.Dispose();
        }

        #endregion

        #region Mesh objects

        Mesh _mesh;
        GraphicsBuffer _vertexBuffer;
        GraphicsBuffer _indexBuffer;

        void AllocateMesh(int vertexCount)
        {
            _mesh = new Mesh();

            // We want GraphicsBuffer access as Raw (ByteAddress) buffers.
            _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

            // Vertex position: float32 x 3
            var vp = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

            // Vertex normal: float32 x 3
            var vn = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
            
            // Vertex uv (texture coordinate): float32 x 2
            var vtc = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2);

            // Vertex/index buffer formats
            _mesh.SetVertexBufferParams(vertexCount, attributes: new[]{vp, vn, vtc});
            _mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);

            // Submesh initialization
            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount),
                MeshUpdateFlags.DontRecalculateBounds);

            // GraphicsBuffer references
            _vertexBuffer = _mesh.GetVertexBuffer(0);
            _indexBuffer = _mesh.GetIndexBuffer();
        }

        void ReleaseMesh()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            Object.Destroy(_mesh);
        }

        #endregion
    }

} // namespace MarchingCubes