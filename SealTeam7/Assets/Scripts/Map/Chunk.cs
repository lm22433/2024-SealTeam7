using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Kinect;

namespace Map
{
    [Serializable]
    public struct ChunkSettings {
        public ushort size;
        public float spacing;
        public float maxHeight;
        public float lerpFactor;
        public ushort x;
        public ushort z;
        public ushort lod;
        public bool isLocalhost;
    }

    public class Chunk : MonoBehaviour {
        
        [SerializeField] private ChunkSettings settings;
        private int _lodFactor;
        private half[] _heightMap;
        private int _vertexSideCount;
        private Mesh _mesh;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private NoiseGenerator _noiseGenerator;
        private KinectAPI _kinect;
        private Bounds _bounds;
        
        public void SetSettings(ChunkSettings s) { settings = s; }
        
        public void SetLod(ushort lod)
        {
            settings.lod = lod;
            _lodFactor = lod == 0 ? 1 : lod * 2;
            _vertexSideCount = settings.size / _lodFactor + 1;
            _heightMap = new half[_vertexSideCount * _vertexSideCount];
            _meshCollider.enabled = lod == 0;
            if (_mesh) UpdateMesh();
        }

        public void SetVisible(bool visible)
        {
            _meshRenderer.enabled = visible;
            enabled = visible;
        }

        public float SqrDistanceToPlayer(Vector3 playerPos)
        {
            return _bounds.SqrDistance(playerPos);
        }
        
        private void Awake()
        {
            _lodFactor = 1;
            _vertexSideCount = settings.size / _lodFactor + 1;
            
            _mesh = new Mesh {name = "Generated Mesh"};
            _mesh.MarkDynamic();
            
            UpdateMesh();
            
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
            _meshCollider.enabled = false;
            
            _heightMap = new half[_vertexSideCount * _vertexSideCount];

            if (settings.isLocalhost) {
                _noiseGenerator = GetComponentInParent<NoiseGenerator>();
            } else {
                _kinect = FindAnyObjectByType<KinectAPI>();
            }

            _bounds = new Bounds(transform.position, new Vector3(settings.size * settings.spacing, 2f * settings.maxHeight, settings.size * settings.spacing));
        }

        private void Update()
        {
            UpdateHeights();
        }

        private void GetHeights() { 
            if (settings.isLocalhost) {
                _noiseGenerator.GetChunkNoise(ref _heightMap, settings.lod, settings.z, settings.x);
            } else {
                _kinect.RequestTexture(settings.lod, settings.z, settings.x);
            }
        }

        public void SetHeights(half[] heights) {
            _heightMap = heights;
        }

        private void UpdateHeights()
        {
            GetHeights();
            
            var vertices = new NativeArray<Vector3>(_mesh.vertices, Allocator.TempJob).Reinterpret<float3>();
            var heights = new NativeArray<float>(_heightMap, Allocator.TempJob);
            
            var heightUpdate = new HeightUpdate {
                Vertices = vertices,
                Heights = heights,
                Scale = settings.maxHeight,
                LerpFactor = settings.lerpFactor
            }.Schedule(_vertexSideCount * _vertexSideCount, 1);
            heightUpdate.Complete();
            
            _mesh.SetVertices(vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            if (_meshCollider.enabled) _meshCollider.sharedMesh = _mesh;
            
            vertices.Dispose(heightUpdate);
            heights.Dispose(heightUpdate);
        }

        private void UpdateMesh() {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var numberOfTriangles = (settings.size / _lodFactor) * (settings.size / _lodFactor) * 6;
            
            var vertices = new NativeArray<float3>(numberOfVertices, Allocator.TempJob);
            var triangles = new NativeArray<int>(numberOfTriangles, Allocator.TempJob);

            var meshVertexUpdate = new MeshVertexUpdate
            {
                Vertices = vertices,
                VertexSideCount = _vertexSideCount,
                Spacing = settings.spacing,
                Size = settings.size,
                LODFactor = _lodFactor
            }.Schedule(numberOfVertices, 1);

            var meshTriangleUpdate = new MeshTriangleUpdate
            {
                Triangles = triangles,
                VertexSideCount = _vertexSideCount
            }.Schedule(numberOfTriangles, 1, meshVertexUpdate);
            
            meshTriangleUpdate.Complete();
            
            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles.ToArray(), 0);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            //_mesh.RecalculateTangents();

            vertices.Dispose(meshVertexUpdate);
            triangles.Dispose(meshTriangleUpdate);
        }
    }

    [BurstCompile]
    public struct HeightUpdate : IJobParallelFor {

        public NativeArray<float3> Vertices;
        public NativeArray<half> Heights;
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
        public float LODFactor;
        public int Size;
        
        public void Execute(int index)
        {
            var x = (int) (index / VertexSideCount) * LODFactor - 0.5f * Size;
            var z = (int) (index % VertexSideCount) * LODFactor - 0.5f * Size;
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
