using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using System.Threading.Tasks;
using System.Collections;

namespace Map
{
    public struct ChunkSettings {
        public int Size;
        public float Spacing;
        public float LerpFactor;
        public int X;
        public int Z;
        public int LOD;
        public int ColliderDst;
        public bool IsKinectPresent;
        public MapManager Manager;
    }

    public class Chunk : MonoBehaviour {
        
        private ChunkSettings _settings;
        private int _lodFactor;
        private byte[] _heightMap;
        private int _vertexSideCount;
        private Mesh _mesh;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private bool _running;
        private bool _gettingHeights;
        
        private bool _newLod;
        private int _requestedLod;
        
        public void SetLod(int lod)
        {
            if (_settings.LOD == lod) return;
            _requestedLod = lod;

        }

        public void SetVisible(bool visible)
        {
            _running = visible;
        }

        public int ChunkDistance(Vector3 playerPos)
        {
            return (int) Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), new Vector3(transform.position.x + _settings.Size / 2, 0, transform.position.z + _settings.Size / 2)) / _settings.Size;
        }
        
        public void Setup(ChunkSettings s, ref byte[] heightMap)
        {
            _settings = s;
            
            _lodFactor = 1;
            _requestedLod = _settings.LOD;
            _vertexSideCount = _settings.Size / _lodFactor + 1;

            var zChunkOffset = _settings.Z * _settings.Size;
            var xChunkOffset = _settings.X * _settings.Size;
            //_heightMap = heightMap[(_lodFactor * _vertexSideCount * _vertexSideCount)..((chunkIndex + 1) * _vertexSideCount * _vertexSideCount)];
            
            _mesh = new Mesh {name = "Generated Mesh"};
            _mesh.MarkDynamic();
            
            UpdateMesh();
            
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
            _meshCollider.enabled = false;
        }

        private void Update()
        {
            /*
            if (_running)
            {
                if (!_gettingHeights) StartCoroutine(GetHeightsCoroutine());

                _meshCollider.enabled = false;
            }

            _meshRenderer.enabled = _running;
            */
        }

        public void SetHeights(byte[] heights, ushort lod)
        {
            if (_settings.LOD == lod) {
                _heightMap = heights;
            }
            else {
                _settings.LOD = lod;
                _lodFactor = lod == 0 ? 1 : lod * 2;
                _vertexSideCount = _settings.Size / _lodFactor + 1;
                _heightMap = new byte[heights.Length];
                _heightMap = heights;

                UpdateMesh();
            }
            
            UpdateHeights();
        }

        private void UpdateHeights()
        {
            var vertices = new NativeArray<Vector3>(_mesh.vertices, Allocator.TempJob).Reinterpret<float3>();
            var heights = new NativeArray<byte>(_heightMap, Allocator.TempJob);
            
            var heightUpdate = new HeightUpdate {
                Vertices = vertices,
                Heights = heights,
                LerpFactor = _settings.LerpFactor
            }.Schedule(_vertexSideCount * _vertexSideCount, 1);
            heightUpdate.Complete();
            
            _mesh.SetVertices(vertices);
            _mesh.RecalculateNormals();
            //_mesh.RecalculateTangents();
            _mesh.RecalculateBounds();
            
            if (_meshCollider.enabled) _meshCollider.sharedMesh = _mesh;
            
            vertices.Dispose();
            heights.Dispose();
        }

        private void UpdateMesh() {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var numberOfTriangles = (_vertexSideCount - 1) * (_vertexSideCount - 1) * 6;
            
            //TODO: adjust so that old height data is preserved over LOD switch
            var vertices = new NativeArray<float3>(numberOfVertices, Allocator.TempJob);
            var triangles = new NativeArray<int>(numberOfTriangles, Allocator.TempJob);
            var uvs = new NativeArray<float2>(numberOfVertices, Allocator.TempJob);
            var heights = new NativeArray<byte>(_heightMap, Allocator.TempJob);
            
            var meshVertexUpdate = new MeshVertexUpdate
            {
                Heights = heights,
                Vertices = vertices,
                UVs = uvs,
                VertexSideCount = _vertexSideCount,
                Spacing = _settings.Spacing,
                Size = _settings.Size,
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
            _mesh.SetUVs(0, uvs);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
            
            vertices.Dispose();
            triangles.Dispose();
            uvs.Dispose();
            heights.Dispose();
        }
    }

    [BurstCompile]
    public struct HeightUpdate : IJobParallelFor {

        public NativeArray<float3> Vertices;
        public NativeArray<byte> Heights;
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
        public NativeArray<byte> Heights;
        public NativeArray<float3> Vertices;
        public NativeArray<float2> UVs;
        public int VertexSideCount;
        public float Spacing;
        public float LODFactor;
        public int Size;
        
        public void Execute(int index)
        {
            //update vertices
            var x = (int) (index / VertexSideCount) * LODFactor * Spacing;
            var z = (int) (index % VertexSideCount) * LODFactor * Spacing;
            Vertices[index] = new float3(x, Heights[index], z);
            
            //update uvs
            UVs[index] = new float2(x / (Size - 1), z / (Size - 1));
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
