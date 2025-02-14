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
    }

    public class Chunk : MonoBehaviour
    {
        private ChunkSettings _settings;
        private int _lodFactor;
        private float[] _heightMap;
        private int _vertexSideCount;
        private Mesh _mesh;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private bool _running;

        public void Setup(ChunkSettings s, ref float[] heightMap)
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
            // _meshCollider.enabled = false;

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
            var vertices = _mesh.vertices;

            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = (int)(i / _vertexSideCount) * _lodFactor;
                var z = (int)(i % _vertexSideCount) * _lodFactor;
                vertices[i].y = Mathf.Lerp(vertices[i].y, _heightMap[(z + zChunkOffset) * _settings.MapSize + xChunkOffset + x], _settings.LerpFactor);
                // vertices[i].y = _heightMap[(z + zChunkOffset) * _settings.MapSize + xChunkOffset + x];
            }

            _mesh.SetVertices(vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            
            _meshCollider.sharedMesh = _mesh;
        }

        private void UpdateMesh()
        {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var numberOfTriangles = (_vertexSideCount - 1) * (_vertexSideCount - 1) * 6;

            var vertices = new Vector3[numberOfVertices];
            var triangles = new int[numberOfTriangles];

            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = (int)(i / _vertexSideCount * _lodFactor);
                var z = (int)(i % _vertexSideCount * _lodFactor);
                vertices[i] = new Vector3(x * _settings.Spacing,
                    _heightMap[(z + zChunkOffset) * _settings.MapSize + xChunkOffset + x], z * _settings.Spacing);
            }

            for (int i = 0; i < numberOfTriangles; i++)
            {
                var baseIndex = i / 6;
                baseIndex += baseIndex / (_vertexSideCount - 1);
                triangles[i] =
                    i % 3 == 0 ? baseIndex :
                    i % 6 - 4 == 0 || i % 6 - 2 == 0 ? baseIndex + _vertexSideCount + 1 :
                    i % 6 - 1 == 0 ? baseIndex + 1 :
                    i % 6 - 5 == 0 ? baseIndex + _vertexSideCount : -1;
            }

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateNormals();
        }
    }
}