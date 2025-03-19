using Game;
using UnityEngine;

namespace Map
{
    public enum InterpolationDirection
    {
        None,
        RightEdge,
        LeftEdge,
        TopEdge,
        BottomEdge,
        TopRightCorner,
        TopLeftCorner,
        BottomRightCorner,
        BottomLeftCorner
    }

    public struct BackgroundChunkSettings
    {
        public int Size;
        public int MapSize;
        public float Spacing;
        public int X;
        public int Z;
        public LODInfo LODInfo;
        public bool ColliderEnabled;
        public float AverageHeight;
        public float HeightScale;
        public float NoiseScale;
        public InterpolationDirection InterpolationDirection;
        // Margin width measured in world units
        public int InterpolationMargin;
    }

    public class BackgroundChunk : MonoBehaviour
    {
        private BackgroundChunkSettings _settings;
        private float[] _heightMap;
        
        private Mesh _mesh;
        private Mesh _colliderMesh;
        private MeshData _meshData;
        private MeshData _colliderMeshData;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;

        private float _averageHeight;
        private float _heightScale;
        private float _noiseScale;
        // Margin width measured as number of edges
        private int _interpolationMargin;
        // Margin width measured as number of edges
        private int _colliderInterpolationMargin;

        public void Setup(BackgroundChunkSettings s, ref float[] heightMap)
        {
            _settings = s;
            
            _meshData = new MeshData
            {
                LODFactor = _settings.LODInfo.lod == 0 ? 1 : _settings.LODInfo.lod * 2,
                VertexSideCount = _settings.Size / (_settings.LODInfo.lod == 0 ? 1 : _settings.LODInfo.lod * 2) + 1,
            };

            _colliderMeshData = new MeshData
            {
                LODFactor = _settings.LODInfo.colliderLod == 0 ? 1 : _settings.LODInfo.colliderLod * 2,
                VertexSideCount = _settings.Size / (_settings.LODInfo.colliderLod == 0 ? 1 : _settings.LODInfo.colliderLod * 2) + 1,
            };

            _interpolationMargin = (int)((_settings.InterpolationMargin / (float)_settings.Size) * (_meshData.VertexSideCount - 1));
            _colliderInterpolationMargin = (int)((_settings.InterpolationMargin / (float)_settings.Size) * (_colliderMeshData.VertexSideCount - 1));

            _heightMap = heightMap;
            
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
            var vertices = _meshData.Vertices;
            var colliderVertices = _colliderMeshData.Vertices;
            int zChunkOffset = _settings.Z * _settings.Size;
            int heightMapWidth = (int) (_settings.MapSize / _settings.Spacing + 1);

            switch (_settings.InterpolationDirection)
            {
                case InterpolationDirection.RightEdge:
                {
                    InterpolateMargin(_meshData.Vertices, _interpolationMargin, _meshData.VertexSideCount, _meshData.LODFactor);
                    InterpolateMargin(_colliderMeshData.Vertices, _colliderInterpolationMargin, _colliderMeshData.VertexSideCount, _colliderMeshData.LODFactor);
                    break;
                }
                case InterpolationDirection.LeftEdge:
                {
                    for (int z = 0; z < _meshData.VertexSideCount; z++)
                    {
                        // vertices indexing is z-major, heightMap indexing is x-major
                        var a = vertices[z + _interpolationMargin * _meshData.VertexSideCount].y;
                        var aPrev = vertices[z + (_interpolationMargin + 1) * _meshData.VertexSideCount].y;
                        var m_a = (a - aPrev) / (float)_settings.Spacing * _interpolationMargin;  // Gradient is difference in y per unit of t
                        var b = _heightMap[(z * _meshData.LODFactor + zChunkOffset) * heightMapWidth + heightMapWidth - 1];
                        var bNext = _heightMap[(z * _meshData.LODFactor + zChunkOffset) * heightMapWidth + heightMapWidth - 1 - _meshData.LODFactor];
                        var m_b = (bNext - b) / (float)_settings.Spacing * _interpolationMargin;
                        if (z == 0) Debug.Log("a: " + a + " aPrev: " + aPrev + " m_a: " + m_a + " b: " + b + " bNext: " + bNext + " m_b: " + m_b + " spacing: " + _settings.Spacing);
                        for (int x = 0; x < _interpolationMargin; x++)
                        {
                            var t = (_interpolationMargin - x) / (float)_interpolationMargin;
                            if (z == 0) Debug.Log("t: " + t);
                            // Cubic Hermite interpolation
                            var y = a + m_a * t + (3 * (b - a) - 2 * m_a - m_b) * t * t + (2 * (a - b) + m_a + m_b) * t * t * t;
                            vertices[z + x * _meshData.VertexSideCount].y = y;
                        }
                    }

                    for (int z = 0; z < _colliderMeshData.VertexSideCount; z++)
                    {
                        // vertices indexing is z-major, heightMap indexing is x-major
                        var a = colliderVertices[z + _colliderInterpolationMargin * _colliderMeshData.VertexSideCount].y;
                        var aPrev = colliderVertices[z + (_colliderInterpolationMargin + 1) * _colliderMeshData.VertexSideCount].y;
                        var m_a = (a - aPrev) / (float)_settings.Spacing * _colliderInterpolationMargin;  // Gradient is difference in y per unit of t
                        var b = _heightMap[(int) ((z * _colliderMeshData.LODFactor + zChunkOffset) * heightMapWidth + heightMapWidth - 1)];
                        var bNext = _heightMap[(int) ((z * _colliderMeshData.LODFactor + zChunkOffset) * heightMapWidth + heightMapWidth - 1 - _colliderMeshData.LODFactor)];
                        var m_b = (bNext - b) / (float)_settings.Spacing * _colliderInterpolationMargin;
                        if (z == 0) Debug.Log("a: " + a + " aPrev: " + aPrev + " m_a: " + m_a + " b: " + b + " bNext: " + bNext + " m_b: " + m_b + " spacing: " + _settings.Spacing);
                        for (int x = 0; x < _colliderInterpolationMargin; x++)
                        {
                            var t = (_colliderInterpolationMargin - x) / (float)_colliderInterpolationMargin;
                            if (z == 0) Debug.Log("t: " + t);
                            // Cubic Hermite interpolation
                            var y = a + m_a * t + (3 * (b - a) - 2 * m_a - m_b) * t * t + (2 * (a - b) + m_a + m_b) * t * t * t;
                            colliderVertices[z + x * _colliderMeshData.VertexSideCount].y = y;
                        }
                    }
                    break;
                }
                case InterpolationDirection.None:
                {
                    break;
                }
            }

            _mesh.SetVertices(vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
            _mesh.RecalculateBounds();
            
            _colliderMesh.SetVertices(colliderVertices);
            _colliderMesh.RecalculateNormals();
            _colliderMesh.RecalculateBounds();
            
            if (_meshCollider.enabled) _meshCollider.sharedMesh = _colliderMesh;
        }

        private void InterpolateMargin(Vector3[] vertices, int interpolationMargin, int vertexSideCount, int lodFactor)
        {
            int zChunkOffset = _settings.Z * _settings.Size;
            int heightMapWidth = (int) (_settings.MapSize / _settings.Spacing + 1);
            
            for (int z = 0; z < vertexSideCount; z++)
            {
                // vertices indexing is z-major, heightMap indexing is x-major
                var a = vertices[z + (vertexSideCount - interpolationMargin - 1) * vertexSideCount].y;
                var aPrev = vertices[z + (vertexSideCount - interpolationMargin - 2) * vertexSideCount].y;
                var aGrad = (a - aPrev) / _settings.Spacing * interpolationMargin;  // Gradient is difference in y per unit of t
                var b = _heightMap[(z * lodFactor + zChunkOffset) * heightMapWidth];
                var bNext = _heightMap[(z * lodFactor + zChunkOffset) * heightMapWidth + lodFactor];
                var bGrad = (bNext - b) / _settings.Spacing * interpolationMargin;
                if (z == 0) Debug.Log("a: " + a + " aPrev: " + aPrev + " m_a: " + aGrad + " b: " + b + " bNext: " + bNext + " m_b: " + bGrad + " spacing: " + _settings.Spacing);
                for (int x = vertexSideCount - interpolationMargin; x < vertexSideCount; x++)
                {
                    var t = (x - (vertexSideCount - interpolationMargin - 1)) / (float)interpolationMargin;
                    if (z == 0) Debug.Log("t: " + t);
                    // Cubic Hermite interpolation
                    var y = a + aGrad * t + (3 * (b - a) - 2 * aGrad - bGrad) * t * t + (2 * (a - b) + aGrad + bGrad) * t * t * t;
                    vertices[z + x * vertexSideCount].y = y;
                }
            }
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
            int zExtraOffset = 5 * _settings.Size;  // Ensures positive region of Perlin noise
            int xExtraOffset = 5 * _settings.Size;

            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = i / _meshData.VertexSideCount * _meshData.LODFactor;
                var z = i % _meshData.VertexSideCount * _meshData.LODFactor;
                var y = Mathf.PerlinNoise((x + xChunkOffset + xExtraOffset) * _settings.NoiseScale, (z + zChunkOffset + zExtraOffset) * _settings.NoiseScale) 
                        * _settings.HeightScale - (_settings.HeightScale / 2) + _settings.AverageHeight;
                vertices[i] = new Vector3(x * _settings.Spacing, y, z * _settings.Spacing);
                uvs[i] = new Vector2((float) x / _meshData.VertexSideCount, (float) z / _meshData.VertexSideCount);
            }
            
            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = i / _colliderMeshData.VertexSideCount * _colliderMeshData.LODFactor;
                var z = i % _colliderMeshData.VertexSideCount * _colliderMeshData.LODFactor;
                var y = Mathf.PerlinNoise((x + xChunkOffset + xExtraOffset) * _settings.NoiseScale, (z + zChunkOffset + zExtraOffset) * _settings.NoiseScale) 
                        * _settings.HeightScale - (_settings.HeightScale / 2) + _settings.AverageHeight;
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