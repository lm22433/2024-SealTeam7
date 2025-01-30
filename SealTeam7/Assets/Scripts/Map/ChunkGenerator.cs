using System;
using FishNet;
using FishNet.Object;
using Kinect;
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
        public bool hasPlayer;
    }
    public class ChunkGenerator : MonoBehaviour {
        
        [SerializeField] private ChunkSettings settings;
        [SerializeField] private ushort[] heightMap;
        private KinectAPI kinect;
        private NativeArray<float3> _vertices;
        private Mesh _mesh;
        private MeshCollider _meshCollider;
        bool isCreated = false;
        
        public void SetSettings(ChunkSettings s) { settings = s; }
        public void CreateChunk() {
            _mesh = CreateMesh();
            _meshCollider = GetComponent<MeshCollider>();
            heightMap = new ushort[settings.size * settings.size];
            kinect = FindAnyObjectByType<KinectAPI>();
            
            GetHeights();

            isCreated = true;
        }

        private void OnDisable() { _vertices.Dispose(); }

        private void Update() { if (isCreated) {UpdateMesh();}}

        private void GetHeights() { 
           kinect.RequestTexture(settings.z, settings.x);
        }

        public void SetHeights(ushort[] heights) {
            heightMap = heights;
        }


        private Mesh CreateMesh() {
            Mesh mesh = new Mesh {name = "Generated Mesh"};
            mesh.MarkDynamic();

            _vertices = new NativeArray<float3>(settings.size * settings.size, Allocator.Persistent);

            // generate grid
            var index = 0;
            for (int x = 0; x < settings.size; x++) {
                for (int z = 0; z < settings.size; z++) {
                    _vertices[index++] = new Vector3(x * settings.spacing, 0f, z * settings.spacing);
                }
            }
            
            // index triangles from vertices
            var indices = new int[(settings.size - 1) * (settings.size - 1) * 6];
            index = 0;
            for (var y = 0; y < settings.size - 1; y++)
            {		
                for (var x = 0; x < settings.size - 1; x++)
                {
                    var baseIndex = y * settings.size + x;
                    indices[index++] = baseIndex;
                    indices[index++] = baseIndex + 1;
                    indices[index++] = baseIndex + settings.size + 1;
                    indices[index++] = baseIndex; 
                    indices[index++] = baseIndex + settings.size + 1;
                    indices[index++] = baseIndex + settings.size;
                }
            }
            
            mesh.SetVertices(_vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            
            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;

            return mesh;
        }

        private void UpdateMesh() {
            GetHeights();
            var heights = new NativeArray<ushort>(heightMap, Allocator.TempJob);
            
            var posHandle = new HeightSampler {
                Vertices = _vertices,
                Heights = heights,
                Scale = settings.heightScale,
                LerpFactor = settings.lerpFactor
            }.Schedule(settings.size * settings.size, 1);
            posHandle.Complete();
            
            _mesh.SetVertices(_vertices);
            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
            if (settings.hasPlayer) _meshCollider.sharedMesh = _mesh;
            
            heights.Dispose();
        }
    }

    [BurstCompile]
    public struct HeightSampler : IJobParallelFor {

        public NativeArray<float3> Vertices;
        public NativeArray<ushort> Heights;
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
}
