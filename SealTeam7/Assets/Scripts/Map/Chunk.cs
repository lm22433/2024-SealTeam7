using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Map
{
    public struct ChunkSettings
    {
        public int Size;
        public float Spacing;
        public float LerpFactor;
        public int Index;
        public int LOD;
    }

    public class Chunk : MonoBehaviour
    {
        private ChunkSettings _settings;
        private int _lodFactor;
        private NativeArray<float> _heightMap;
        private int _vertexSideCount;
        private int _chunkIndex;
        private Mesh _mesh;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private bool _running;

        public void Setup(ChunkSettings s, ref NativeArray<float> heightMap)
        {
            _settings = s;

            _lodFactor = _settings.LOD == 0 ? 1 : _settings.LOD * 2;
            _vertexSideCount = _settings.Size / _lodFactor + 1;
            _heightMap = heightMap;

            _mesh = new Mesh { name = "Generated Mesh" };
            _mesh.MarkDynamic();

            UpdateMesh();

            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
            
            _meshCollider.enabled = false;

            _running = true;
        }

        private void Update()
        {
            if (_running)
            {
                // UpdateHeights();
            }
        }

        public void UpdateHeights()
        {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var vertices = new NativeArray<Vector3>(_mesh.vertices, Allocator.TempJob).Reinterpret<float3>();
            var heights = _heightMap.GetSubArray(_settings.Index * numberOfVertices, numberOfVertices);

            var heightUpdate = new HeightUpdate {
                Vertices = vertices,
                Heights = heights,
                LerpFactor = _settings.LerpFactor
            }.Schedule(numberOfVertices, 1);
            heightUpdate.Complete();
            
            _mesh.SetVertices(vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            
            _meshCollider.sharedMesh = _mesh;

            vertices.Dispose();
        }

        private void UpdateMesh()
        {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var numberOfTriangles = (_vertexSideCount - 1) * (_vertexSideCount - 1) * 6;

            var vertices = new NativeArray<float3>(numberOfVertices, Allocator.TempJob);
            var heights = _heightMap.GetSubArray(_settings.Index * numberOfVertices, numberOfVertices);
            var triangles = new NativeArray<int>(numberOfTriangles, Allocator.TempJob);
            
            var meshVertexUpdate = new MeshVertexUpdate
            {
                Vertices = vertices,
                Heights = heights,
                VertexSideCount = _vertexSideCount,
                Spacing = _settings.Spacing,
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
            
            vertices.Dispose();
            triangles.Dispose();
        }
    }
    
    [BurstCompile]
    public struct HeightUpdate : IJobParallelFor {

        public NativeArray<float3> Vertices;
        [ReadOnly] public NativeArray<float> Heights;
        [ReadOnly] public float LerpFactor;
        
        public void Execute(int index) {
            var p = Vertices[index];
            var y = p.y;
            y = Mathf.Lerp(y, Heights[index], LerpFactor);
            p.y = y;
            Vertices[index] = p;
        }
    }

    [BurstCompile]
    public struct MeshVertexUpdate : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float3> Vertices;
        [ReadOnly] public NativeArray<float> Heights;
        [ReadOnly] public int VertexSideCount;
        [ReadOnly] public float Spacing;
        [ReadOnly] public float LODFactor;
        
        public void Execute(int index)
        {
            //update vertices
            var x = index / VertexSideCount * LODFactor * Spacing;
            var z = index % VertexSideCount * LODFactor * Spacing;
            Vertices[index] = new float3(x, Heights[index], z);
        }
    }
    
    [BurstCompile]
    public struct MeshTriangleUpdate : IJobParallelFor
    {
        [WriteOnly] public NativeArray<int> Triangles;
        [ReadOnly] public int VertexSideCount;
        
        public void Execute(int index)
        {
            //update triangles
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