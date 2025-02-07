using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Kinect;

using System.Threading.Tasks;
using System.Collections;

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
        public bool isKinectPresent;
    }

    public class Chunk : MonoBehaviour {
        
        [SerializeField] public ChunkSettings settings;
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
        private bool _running;
        private bool _gettingHeights;
        private bool _newLod = true;
        [SerializeField] private ushort requestedLod;
        
        public void SetSettings(ChunkSettings s) { settings = s; }
        
        public void SetLod(ushort lod)
        {
            if (settings.lod == lod) return;
            requestedLod = lod;

        }

        public void SetVisible(bool visible)
        {
            _running = visible;
        }

        Vector3 playerPos;

        public float SqrDistanceToPlayer(Vector3 playerPos)
        {
            this.playerPos = playerPos;
            return Vector3.Distance(new Vector3(playerPos.x, playerPos.y, playerPos.z), new Vector3(transform.position.x + (settings.size / 2), settings.maxHeight / 2, transform.position.z + + (settings.size / 2))) / settings.size;
        }
        
        private void Awake()
        {
            _lodFactor = 1;
            requestedLod = settings.lod;
            _vertexSideCount = settings.size / _lodFactor;
            
            _mesh = new Mesh {name = "Generated Mesh"};
            _mesh.MarkDynamic();
            
            _bounds = new Bounds(transform.position, new Vector3((settings.size - 1) * settings.spacing, 2f * settings.maxHeight, (settings.size - 1) * settings.spacing));
            _mesh.bounds = _bounds;
                        
            _heightMap = new half[_vertexSideCount * _vertexSideCount];

            
            UpdateMesh();
            
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
            _meshCollider.enabled = false;

            if (!settings.isKinectPresent) {
                _noiseGenerator = FindAnyObjectByType<NoiseGenerator>();
            } else {
                _kinect = FindAnyObjectByType<KinectAPI>();
            }

        }   

        private void Update()
        {
            if (_running)
            {
                if (!_gettingHeights) StartCoroutine(GetHeightsCoroutine());

                _meshCollider.enabled = SqrDistanceToPlayer(playerPos) <= 4;
            }

            _meshRenderer.enabled = _running;
        }

        private IEnumerator GetHeightsCoroutine() {
            _gettingHeights = true;
            while (_running)
            {
                yield return new WaitForSeconds(0.01f);
                GetHeights();
            }
            _gettingHeights = false;
        }
        
        private void GetHeights() {
            if (!settings.isKinectPresent) {
                _noiseGenerator.RequestNoise(requestedLod, settings.size, settings.x, settings.z);
            } else {
                _kinect.RequestTexture(requestedLod, settings.size, settings.x, settings.z);
            }
        }

        public void SetHeights(half[] heights, ushort lod)
        {
            if (settings.lod == lod) {
                _heightMap = heights;
            }
            else {
                _newLod = true;
                settings.lod = lod;
                _lodFactor = lod == 0 ? 1 : lod * 2;
                _vertexSideCount = settings.size / _lodFactor;
                _heightMap = new half[_vertexSideCount * _vertexSideCount];
                _heightMap = heights;

                UpdateMesh();

            }
            UpdateHeights();
        }

        private void UpdateHeights()
        {
            var vertices = new NativeArray<Vector3>(_mesh.vertices, Allocator.TempJob).Reinterpret<float3>();
            var heights = new NativeArray<half>(_heightMap, Allocator.TempJob);
            
            var heightUpdate = new HeightUpdate {
                Vertices = vertices,
                Heights = heights,
                Scale = settings.maxHeight,
                LerpFactor = _newLod ? 1 : settings.lerpFactor
            }.Schedule(_vertexSideCount * _vertexSideCount, 1);
            heightUpdate.Complete();
            
            _newLod = false;

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
            
            var meshVertexUpdate = new MeshVertexUpdate
            {
                Vertices = vertices,
                UVs = uvs,
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
            _mesh.SetUVs(0, uvs);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
            
            vertices.Dispose();
            triangles.Dispose();
            uvs.Dispose();
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
        public NativeArray<float2> UVs;
        public int VertexSideCount;
        public float Spacing;
        public float LODFactor;
        public int Size;
        
        public void Execute(int index)
        {
            //update vertices
            var x = index / VertexSideCount * LODFactor * Spacing;
            var z = index % VertexSideCount * LODFactor * Spacing;
            Vertices[index] = new float3(x, 0.0f, z);
            
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
