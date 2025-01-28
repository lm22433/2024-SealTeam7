using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Map
{
    [Serializable]
    public struct ChunkSettings {
        public ushort size;
        public float spacing;
        public float heightScale;
        public float lerpFactor;
        public ushort x;
        public ushort z;
        public ushort lod;
    }

    public class Chunk : MonoBehaviour {
        
        [SerializeField] private ChunkSettings settings;
        private int _lodFactor;
        private NativeArray<float3> _vertices;
        private float[] _heightMap;
        private int _vertexSideCount;
        private Mesh _mesh;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private NoiseGenerator _noiseGenerator;
        private Bounds _bounds;
        
        public void SetSettings(ChunkSettings s) { settings = s; }
        
        public void SetLod(ushort lod)
        {
            settings.lod = lod;
            _lodFactor = lod == 0 ? 1 : lod * 2;
            _vertexSideCount = settings.size / _lodFactor + 1;
            _heightMap = new float[_vertexSideCount * _vertexSideCount];
            _meshCollider.enabled = lod == 0;
            if (_mesh) UpdateMesh();
        }

        public void SetVisible(bool visible)
        {
            _meshFilter.sharedMesh = visible ? _mesh : null;
        }
        
        private void Awake()
        {
            //TODO: use Bounds object to calculate player distance
            //_bounds = new Bounds(transform.position, Vector2.one * settings.size);
            _lodFactor = 1;
            _vertexSideCount = settings.size / _lodFactor + 1;
            _vertices = new NativeArray<float3>(_vertexSideCount * _vertexSideCount, Allocator.Persistent);
            
            _mesh = new Mesh {name = "Generated Mesh"};
            _mesh.MarkDynamic();
            
            UpdateMesh();
            
            _meshFilter = GetComponent<MeshFilter>();
            _meshFilter.sharedMesh = _mesh;
            _meshCollider = GetComponent<MeshCollider>();
            _meshCollider.sharedMesh = _mesh;
            _meshCollider.enabled = false;
            
            _heightMap = new float[_vertexSideCount * _vertexSideCount];
            _noiseGenerator = GetComponentInParent<NoiseGenerator>();
        }

        private void Update()
        {
            UpdateHeights();
        }

        private void UpdateHeights()
        {
            _noiseGenerator.GetChunkNoise(ref _heightMap, settings.lod, settings.z, settings.x);
            var heights = new NativeArray<float>(_heightMap, Allocator.TempJob);
            
            var heightUpdate = new HeightUpdate {
                Vertices = _vertices,
                Heights = heights,
                Scale = settings.heightScale,
                LerpFactor = settings.lerpFactor
            }.Schedule(_vertexSideCount * _vertexSideCount, 1);
            heightUpdate.Complete();
            
            _mesh.SetVertices(_vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            if (_meshCollider.enabled) _meshCollider.sharedMesh = _mesh;
            
            heights.Dispose();
        }

        private void UpdateMesh() {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var numberOfTriangles = (settings.size / _lodFactor) * (settings.size / _lodFactor) * 6;

            _vertices.Dispose();
            _vertices = new NativeArray<float3>(numberOfVertices, Allocator.Persistent);
            var triangles = new NativeArray<int>(numberOfTriangles, Allocator.TempJob);

            var meshVertexUpdate = new MeshVertexUpdate
            {
                Vertices = _vertices,
                VertexSideCount = _vertexSideCount,
                Spacing = settings.spacing * _lodFactor,
                Size = settings.size
            }.Schedule(numberOfVertices, 1);

            var meshTriangleUpdate = new MeshTriangleUpdate
            {
                Triangles = triangles,
                VertexSideCount = _vertexSideCount
            }.Schedule(numberOfTriangles, 1, meshVertexUpdate);
            
            meshTriangleUpdate.Complete();
            
            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(triangles.ToArray(), 0);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            //_mesh.RecalculateTangents();
            
            triangles.Dispose();
        }
    }

    [BurstCompile]
    public struct HeightUpdate : IJobParallelFor {

        public NativeArray<float3> Vertices;
        public NativeArray<float> Heights;
        public float LerpFactor;
        public float Scale;
        
        public void Execute(int index) {
            var p = Vertices[index];
            var y = p.y;
            y = Mathf.Lerp(y, Heights[index] * Scale, LerpFactor);
            p.y = y;
            Vertices[index] = p;
        }
    }

    [BurstCompile]
    public struct MeshVertexUpdate : IJobParallelFor
    {
        public NativeArray<float3> Vertices;
        public int VertexSideCount;
        public float Spacing;
        public int Size;
        
        public void Execute(int index)
        {
            var x = index / VertexSideCount;
            var z = index % VertexSideCount;
            Vertices[index] = new float3(x * Spacing, 0f, z * Spacing);
        }
    }
    
    [BurstCompile]
    public struct MeshTriangleUpdate : IJobParallelFor
    {
        public NativeArray<int> Triangles;
        public int VertexSideCount;
        
        public void Execute(int index)
        {
            var baseIndex = index / 6;
            baseIndex += baseIndex / (VertexSideCount - 1);
            Triangles[index] =
                index % 3 == 0 ? baseIndex :
                index % 6 - 4 == 0 || index % 6 - 2 == 0 ? baseIndex + VertexSideCount + 1 :
                index % 6 - 1 == 0 ? baseIndex + 1 :
                index % 6 - 5 == 0 ? baseIndex + VertexSideCount : -1;
        }
    }
}
