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
        public int Size;
        public int MapSize;
        public float Spacing;
        public float LerpFactor;
        public int X;
        public int Z;
        public int LOD;
    }

    public class Chunk : MonoBehaviour {
        [SerializeField] private ChunkSettings _settings;
        private int _lodFactor;
        private NativeArray<float> _heightMap;
        private int _vertexSideCount;
        private Mesh _mesh;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private bool _running;
        
        public int ChunkDistance(Vector3 playerPos)
        {
            return (int) Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), new Vector3(transform.position.x + _settings.Size / 2, 0, transform.position.z + _settings.Size / 2)) / _settings.Size;
        }
        
        public void Setup(ChunkSettings s, ref NativeArray<float> heightMap)
        {
            _settings = s;
            
            _lodFactor = 1;
            _vertexSideCount = _settings.Size / _lodFactor + 1;
            _heightMap = heightMap;
            
            _mesh = new Mesh {name = "Generated Mesh"};
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
                UpdateHeights();
            }
        }

        private void UpdateHeights()
        {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            // var vertices = new NativeArray<float3>(numberOfVertices, Allocator.TempJob);
            // var heights = new NativeArray<float>(numberOfVertices, Allocator.TempJob);
            
            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = (int) (i / _vertexSideCount * _lodFactor * _settings.Spacing);
                var z = (int) (i % _vertexSideCount * _lodFactor * _settings.Spacing);
                var p = _mesh.vertices[i];
                var y = p.y;
                y = Mathf.Lerp(y, _heightMap[(_lodFactor * z + zChunkOffset) * _settings.MapSize + xChunkOffset + _lodFactor * x], _settings.LerpFactor);
                p.y = y;
                _mesh.vertices[i] = p;
            }
            
            /*var heightUpdate = new HeightUpdate {
                Vertices = vertices,
                Heights = heights,
                LerpFactor = _settings.LerpFactor
            }.Schedule(numberOfVertices, 1);
            heightUpdate.Complete();*/
            
            // _mesh.SetVertices(vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            
            // vertices.Dispose();
            // heights.Dispose();
        }

        private void UpdateMesh() {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var numberOfTriangles = (_vertexSideCount - 1) * (_vertexSideCount - 1) * 6;
            
            //TODO: adjust so that old height data is preserved over LOD switch
            var vertices = new NativeArray<float3>(numberOfVertices, Allocator.TempJob);
            var triangles = new NativeArray<int>(numberOfTriangles, Allocator.TempJob);
            var heights = new NativeArray<float>(numberOfVertices, Allocator.TempJob);
            
            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;
            
            for (int z = 0; z < _vertexSideCount; z++)
            {
                for (int x = 0; x < _vertexSideCount; x++)
                {
                    heights[z * _vertexSideCount + x] = _heightMap[(_lodFactor * z + zChunkOffset) * _settings.MapSize + xChunkOffset + _lodFactor * x];
                }
            }
            
            var meshVertexUpdate = new MeshVertexUpdate
            {
                Heights = heights,
                Vertices = vertices,
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
            heights.Dispose();
        }
    }

    [BurstCompile]
    public struct HeightUpdate : IJobParallelFor {

        public NativeArray<float3> Vertices;
        public NativeArray<float> Heights;
        public float LerpFactor;
        
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
        public NativeArray<float> Heights;
        public NativeArray<float3> Vertices;
        public int VertexSideCount;
        public float Spacing;
        public float LODFactor;
        
        public void Execute(int index)
        {
            //update vertices
            var x = (int) (index / VertexSideCount) * LODFactor * Spacing;
            var z = (int) (index % VertexSideCount) * LODFactor * Spacing;
            Vertices[index] = new float3(x, Heights[index], z);
        }
    }
    
    [BurstCompile]
    public struct MeshTriangleUpdate : IJobParallelFor
    {
        public NativeArray<int> Triangles;
        public int VertexSideCount;
        
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
