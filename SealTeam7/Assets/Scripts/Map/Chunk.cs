using System;
using UnityEngine;

namespace Map
{
    public struct ChunkSettings
    {
        public int Size;
        public int MapSize;
        public float Spacing;
        public float LerpFactor;
        public int X;
        public int Z;
        public int LOD;
        public int ColliderLOD;
    }

    public struct MeshData
    {
        public int LODFactor;
        public int VertexSideCount;
        public Vector3[] Vertices;
        public int[] Triangles;
    }

    public class Chunk : MonoBehaviour
    {
        private ChunkSettings _settings;
        private float[] _heightMap;
        
        private Mesh _mesh;
        private Mesh _colliderMesh;
        private MeshData _meshData;
        private MeshData _colliderMeshData;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        
        private bool _running;

        public void Setup(ChunkSettings s, ref float[] heightMap)
        {
            _settings = s;
            
            _meshData = new MeshData
            {
                LODFactor = _settings.LOD == 0 ? 1 : _settings.LOD * 2,
                VertexSideCount = _settings.Size / (_settings.LOD == 0 ? 1 : _settings.LOD * 2) + 1
            };

            _colliderMeshData = new MeshData
            {
                LODFactor = _settings.ColliderLOD == 0 ? 1 : _settings.ColliderLOD * 2,
                VertexSideCount = _settings.Size / (_settings.ColliderLOD == 0 ? 1 : _settings.ColliderLOD * 2) + 1
            };
            
            _heightMap = heightMap;

            _mesh = new Mesh { name = "Generated Mesh" };
            _mesh.MarkDynamic();
            
            _colliderMesh = new Mesh { name = "Generated Collider Mesh" };
            _colliderMesh.MarkDynamic();

            UpdateMesh();

            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _colliderMesh;

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
            var numberOfVertices = _meshData.VertexSideCount * _meshData.VertexSideCount;
            var vertices = _meshData.Vertices;
            
            var colliderNumberOfVertices = _colliderMeshData.VertexSideCount * _colliderMeshData.VertexSideCount;
            var colliderVertices = _colliderMeshData.Vertices;

            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = (int)(i / _meshData.VertexSideCount) * _meshData.LODFactor;
                var z = (int)(i % _meshData.VertexSideCount) * _meshData.LODFactor;
                vertices[i].y = Mathf.Lerp(vertices[i].y,
                    _heightMap[(int) ((z + zChunkOffset) * (_settings.MapSize / _settings.Spacing + 1) + xChunkOffset + x)], _settings.LerpFactor);
                // vertices[i].y = _heightMap[(z + zChunkOffset) * _settings.MapSize + xChunkOffset + x];
            }
            
            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = (int)(i / _colliderMeshData.VertexSideCount) * _colliderMeshData.LODFactor;
                var z = (int)(i % _colliderMeshData.VertexSideCount) * _colliderMeshData.LODFactor;
                colliderVertices[i].y = Mathf.Lerp(colliderVertices[i].y,
                    _heightMap[(int) ((z + zChunkOffset) * (_settings.MapSize / _settings.Spacing + 1) + xChunkOffset + x)], _settings.LerpFactor);
            }

            _meshData.Vertices = vertices;
            _mesh.SetVertices(vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            
            _colliderMeshData.Vertices = colliderVertices;
            _colliderMesh.SetVertices(colliderVertices);
            _colliderMesh.RecalculateNormals();
            _colliderMesh.RecalculateBounds();
            _meshCollider.sharedMesh = _colliderMesh;
        }

        private void UpdateMesh()
        {
            var numberOfVertices = _meshData.VertexSideCount * _meshData.VertexSideCount;
            var numberOfTriangles = (_meshData.VertexSideCount - 1) * (_meshData.VertexSideCount - 1) * 6;
            var vertices = new Vector3[numberOfVertices];
            var triangles = new int[numberOfTriangles];
            
            var colliderNumberOfVertices = _colliderMeshData.VertexSideCount * _colliderMeshData.VertexSideCount;
            var colliderNumberOfTriangles = (_colliderMeshData.VertexSideCount - 1) * (_colliderMeshData.VertexSideCount - 1) * 6;
            var colliderVertices = new Vector3[colliderNumberOfVertices];
            var colliderTriangles = new int[colliderNumberOfTriangles];

            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = (int) (i / _meshData.VertexSideCount) * _meshData.LODFactor;
                var z = (int) (i % _meshData.VertexSideCount) * _meshData.LODFactor;
                vertices[i] = new Vector3(x * _settings.Spacing,
                    _heightMap[(int) ((z + zChunkOffset) * (_settings.MapSize / _settings.Spacing + 1) + xChunkOffset + x)], z * _settings.Spacing);
            }
            
            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = (int) (i / _colliderMeshData.VertexSideCount) * _colliderMeshData.LODFactor;
                var z = (int) (i % _colliderMeshData.VertexSideCount) * _colliderMeshData.LODFactor;
                
                colliderVertices[i] = new Vector3(x * _settings.Spacing,
                    _heightMap[(int) ((z + zChunkOffset) * (_settings.MapSize / _settings.Spacing + 1) + xChunkOffset + x)], z * _settings.Spacing);
            }

            for (int i = 0; i < numberOfTriangles; i++)
            {
                var baseIndex = i / 6;
                baseIndex += baseIndex / (_meshData.VertexSideCount - 1);
                triangles[i] =
                    i % 3 == 0 ? baseIndex :
                    i % 6 - 4 == 0 || i % 6 - 2 == 0 ? baseIndex + _meshData.VertexSideCount + 1 :
                    i % 6 - 1 == 0 ? baseIndex + 1 :
                    i % 6 - 5 == 0 ? baseIndex + _meshData.VertexSideCount : -1;
            }
            
            for (int i = 0; i < colliderNumberOfTriangles; i++)
            {
                var baseIndex = i / 6;
                baseIndex += baseIndex / (_colliderMeshData.VertexSideCount - 1);
                colliderTriangles[i] =
                    i % 3 == 0 ? baseIndex :
                    i % 6 - 4 == 0 || i % 6 - 2 == 0 ? baseIndex + _colliderMeshData.VertexSideCount + 1 :
                    i % 6 - 1 == 0 ? baseIndex + 1 :
                    i % 6 - 5 == 0 ? baseIndex + _colliderMeshData.VertexSideCount : -1;
            }

            _meshData.Vertices = vertices;
            _meshData.Triangles = triangles;
            _colliderMeshData.Vertices = colliderVertices;
            _colliderMeshData.Triangles = colliderTriangles;
            
            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateNormals();
            
            _colliderMesh.Clear();
            _colliderMesh.SetVertices(colliderVertices);
            _colliderMesh.SetTriangles(colliderTriangles, 0);
            _colliderMesh.RecalculateNormals();
        }
    }
}