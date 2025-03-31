using System.Collections;
using Game;
using UnityEngine;
using UnityEngine.Rendering;

namespace Map
{
    public struct ChunkSettings
    {
        public int Size;
        public int MapSize;
        public float MapSpacing;
        public int X;
        public int Z;
        public LODInfo LODInfo;
        public bool ColliderEnabled;
    }

    public struct SavedMeshData
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector3[] ColliderVertices;
    }

    public class Chunk : MonoBehaviour
    {
        private ChunkSettings _settings;
        private float[,] _heightMap;

        private Mesh _mesh;
        private Mesh _colliderMesh;
        private SavedMeshData _savedMesh;
        private int _lodFactor;
        private int _vertexSideCount;
        private int _colliderLodFactor;
        private int _colliderVertexSideCount;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private bool _recalcedTangents;
        private bool _meshNeedsUpdate;

        public void Setup(ChunkSettings s, ref float[,] heightMap)
        {
            _settings = s;

            _savedMesh = new SavedMeshData();

            _lodFactor = _settings.LODInfo.lod == 0 ? 1 : _settings.LODInfo.lod * 2;
            _vertexSideCount = _settings.Size / _lodFactor + 1;

            _colliderLodFactor = _settings.LODInfo.colliderLod == 0 ? 1 : _settings.LODInfo.colliderLod * 2;
            _colliderVertexSideCount = _settings.Size / _colliderLodFactor + 1;

            _heightMap = heightMap;

            _mesh = new Mesh { name = "Generated Mesh", indexFormat = IndexFormat.UInt32 };
            _mesh.MarkDynamic();

            _colliderMesh = new Mesh { name = "Generated Collider Mesh", indexFormat = IndexFormat.UInt32 };
            _colliderMesh.MarkDynamic();

            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();

            UpdateMesh();

            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _colliderMesh;

            if (!_settings.ColliderEnabled) _meshCollider.enabled = false;
        }

        public Vector3[] GetNormals() => _savedMesh.Normals;

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive())
            {
                _recalcedTangents = false;
                return;
            }

            if (_meshNeedsUpdate)
            {
                _mesh.SetVertices(_savedMesh.Vertices);
                _mesh.RecalculateNormals();
                _savedMesh.Normals = _mesh.normals;

                if (!_recalcedTangents) _mesh.RecalculateTangents();
                _mesh.RecalculateBounds();

                _colliderMesh.SetVertices(_savedMesh.ColliderVertices);
                _colliderMesh.RecalculateBounds();

                if (_meshCollider.enabled) _meshCollider.sharedMesh = _colliderMesh;

                _meshNeedsUpdate = false;
            }
            _recalcedTangents = true;
        }

        public void UpdateHeights()
        {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var vertices = _savedMesh.Vertices;

            var colliderNumberOfVertices = _colliderVertexSideCount * _colliderVertexSideCount;
            var colliderVertices = _savedMesh.ColliderVertices;

            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = i / _vertexSideCount * _lodFactor;
                var z = i % _vertexSideCount * _lodFactor;
                vertices[i].y = _heightMap[z + zChunkOffset, xChunkOffset + x];
            }

            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = i / _colliderVertexSideCount * _colliderLodFactor;
                var z = i % _colliderVertexSideCount * _colliderLodFactor;
                colliderVertices[i].y = _heightMap[z + zChunkOffset, xChunkOffset + x];
            }

            _savedMesh.Vertices = vertices;
            _savedMesh.ColliderVertices = colliderVertices;

            _meshNeedsUpdate = true;
        }

        private void UpdateMesh()
        {
            var numberOfVertices = _vertexSideCount * _vertexSideCount;
            var numberOfTriangles = (_vertexSideCount - 1) * (_vertexSideCount - 1) * 6;
            var vertices = new Vector3[numberOfVertices];
            var triangles = new int[numberOfTriangles];
            var uvs = new Vector2[numberOfVertices];

            var colliderNumberOfVertices = _colliderVertexSideCount * _colliderVertexSideCount;
            var colliderNumberOfTriangles = (_colliderVertexSideCount - 1) * (_colliderVertexSideCount - 1) * 6;
            var colliderVertices = new Vector3[colliderNumberOfVertices];
            var colliderTriangles = new int[colliderNumberOfTriangles];

            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = i / _vertexSideCount * _lodFactor;
                var z = i % _vertexSideCount * _lodFactor;
                vertices[i] = new Vector3(
                    x * _settings.MapSpacing,
                    _heightMap[z + zChunkOffset, xChunkOffset + x],
                    z * _settings.MapSpacing);
                uvs[i] = new Vector2((float)x / _vertexSideCount, (float)z / _vertexSideCount);
            }

            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = i / _colliderVertexSideCount * _colliderLodFactor;
                var z = i % _colliderVertexSideCount * _colliderLodFactor;

                colliderVertices[i] = new Vector3(
                    x * _settings.MapSpacing,
                    _heightMap[z + zChunkOffset, xChunkOffset + x],
                    z * _settings.MapSpacing);
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

            for (int i = 0; i < colliderNumberOfTriangles; i++)
            {
                var baseIndex = i / 6;
                baseIndex += baseIndex / (_colliderVertexSideCount - 1);
                colliderTriangles[i] =
                    i % 3 == 0 ? baseIndex :
                    i % 6 - 4 == 0 || i % 6 - 2 == 0 ? baseIndex + _colliderVertexSideCount + 1 :
                    i % 6 - 1 == 0 ? baseIndex + 1 :
                    i % 6 - 5 == 0 ? baseIndex + _colliderVertexSideCount : -1;
            }

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles, 0);
            _mesh.SetUVs(0, uvs);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();

            _colliderMesh.Clear();
            _colliderMesh.SetVertices(colliderVertices);
            _colliderMesh.SetTriangles(colliderTriangles, 0);

            _savedMesh.Vertices = vertices;
            _savedMesh.Normals = _mesh.normals;
            _savedMesh.ColliderVertices = colliderVertices;
        }
    }
}