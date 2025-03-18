using System;
using Game;
using UnityEngine;

namespace Map
{
    public class BackgroundChunk : MonoBehaviour
    {
        private ChunkSettings _settings;
        
        private Mesh _mesh;
        private Mesh _colliderMesh;
        private MeshData _meshData;
        private MeshData _colliderMeshData;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;

        private float _averageHeight;
        private float _heightScale;
        private float _noiseScale;

        public void Setup(ChunkSettings s, float averageHeight, float heightScale, float noiseScale)
        {
            _settings = s;
            _averageHeight = averageHeight;
            _heightScale = heightScale;
            _noiseScale = noiseScale;
            
            _meshData = new MeshData
            {
                LODFactor = _settings.LODInfo.lod == 0 ? 1 : _settings.LODInfo.lod * 2,
                VertexSideCount = _settings.Size / (_settings.LODInfo.lod == 0 ? 1 : _settings.LODInfo.lod * 2) + 1
            };

            _colliderMeshData = new MeshData
            {
                LODFactor = _settings.LODInfo.colliderLod == 0 ? 1 : _settings.LODInfo.colliderLod * 2,
                VertexSideCount = _settings.Size / (_settings.LODInfo.colliderLod == 0 ? 1 : _settings.LODInfo.colliderLod * 2) + 1
            };
            
            _mesh = new Mesh { name = "Generated Mesh" };
            _mesh.MarkDynamic();
            
            _colliderMesh = new Mesh { name = "Generated Collider Mesh" };
            _colliderMesh.MarkDynamic();
            
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            
            UpdateMesh();
            
            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _colliderMesh;
            
            if (!_settings.ColliderEnabled) _meshCollider.enabled = false;
        }

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            UpdateHeights();
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
                var x = i / _meshData.VertexSideCount * _meshData.LODFactor;
                var z = i % _meshData.VertexSideCount * _meshData.LODFactor;
                // vertices[i].y = _heightMap[(int) ((z + zChunkOffset) * (_settings.MapSize / _settings.Spacing + 1) + xChunkOffset + x)];
            }
            
            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = i / _colliderMeshData.VertexSideCount * _colliderMeshData.LODFactor;
                var z = i % _colliderMeshData.VertexSideCount * _colliderMeshData.LODFactor;
                // colliderVertices[i].y = _heightMap[(int) ((z + zChunkOffset) * (_settings.MapSize / _settings.Spacing + 1) + xChunkOffset + x)];
            }

            _meshData.Vertices = vertices;
            _mesh.SetVertices(vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
            _mesh.RecalculateBounds();
            
            _colliderMeshData.Vertices = colliderVertices;
            _colliderMesh.SetVertices(colliderVertices);
            _colliderMesh.RecalculateNormals();
            _colliderMesh.RecalculateBounds();
            
            if (_meshCollider.enabled) _meshCollider.sharedMesh = _colliderMesh;
        }

        private void UpdateMesh()
        {
            var numberOfVertices = _meshData.VertexSideCount * _meshData.VertexSideCount;
            var numberOfTriangles = (_meshData.VertexSideCount - 1) * (_meshData.VertexSideCount - 1) * 6;
            var vertices = new Vector3[numberOfVertices];
            var triangles = new int[numberOfTriangles];
            var uvs = new Vector2[numberOfVertices];
            
            var colliderNumberOfVertices = _colliderMeshData.VertexSideCount * _colliderMeshData.VertexSideCount;
            var colliderNumberOfTriangles = (_colliderMeshData.VertexSideCount - 1) * (_colliderMeshData.VertexSideCount - 1) * 6;
            var colliderVertices = new Vector3[colliderNumberOfVertices];
            var colliderTriangles = new int[colliderNumberOfTriangles];

            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = i / _meshData.VertexSideCount * _meshData.LODFactor;
                var z = i % _meshData.VertexSideCount * _meshData.LODFactor;
                var y = Mathf.PerlinNoise((x + xChunkOffset) * _noiseScale, (z + zChunkOffset) * _noiseScale) 
                        * _heightScale - (_heightScale / 2) + _averageHeight;
                vertices[i] = new Vector3(x * _settings.Spacing, y, z * _settings.Spacing);
                uvs[i] = new Vector2((float) x / _meshData.VertexSideCount, (float) z / _meshData.VertexSideCount);
            }
            
            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = i / _colliderMeshData.VertexSideCount * _colliderMeshData.LODFactor;
                var z = i % _colliderMeshData.VertexSideCount * _colliderMeshData.LODFactor;
                var y = Mathf.PerlinNoise((x + xChunkOffset) * _noiseScale, (z + zChunkOffset) * _noiseScale) 
                        * _heightScale - (_heightScale / 2) + _averageHeight;
                colliderVertices[i] = new Vector3(x * _settings.Spacing, y, z * _settings.Spacing);
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
            _meshData.UVs = uvs;
            _colliderMeshData.Vertices = colliderVertices;
            _colliderMeshData.Triangles = colliderTriangles;
            
            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles, 0);
            _mesh.SetUVs(0, uvs);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
            
            _colliderMesh.Clear();
            _colliderMesh.SetVertices(colliderVertices);
            _colliderMesh.SetTriangles(colliderTriangles, 0);
            _colliderMesh.RecalculateNormals();
        }
    }
}