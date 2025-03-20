using System;
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
            InterpolateMargin(_meshData.Vertices, _interpolationMargin, _meshData.VertexSideCount, 
                _meshData.LODFactor, _settings.InterpolationDirection);
            InterpolateMargin(_colliderMeshData.Vertices, _colliderInterpolationMargin, _colliderMeshData.VertexSideCount, 
                _colliderMeshData.LODFactor, _settings.InterpolationDirection);

            _mesh.SetVertices(_meshData.Vertices);
            _mesh.RecalculateNormals();
            _mesh.RecalculateTangents();
            _mesh.RecalculateBounds();
            
            _colliderMesh.SetVertices(_colliderMeshData.Vertices);
            _colliderMesh.RecalculateNormals();
            _colliderMesh.RecalculateBounds();
            
            if (_meshCollider.enabled) _meshCollider.sharedMesh = _colliderMesh;
        }

        private void InterpolateMargin(Vector3[] vertices, int interpolationMargin, int vertexSideCount, int lodFactor, 
            InterpolationDirection interpolationDirection)
        {
            // Temporary
            if (interpolationDirection != InterpolationDirection.RightEdge && 
                interpolationDirection != InterpolationDirection.LeftEdge &&
                interpolationDirection != InterpolationDirection.TopEdge &&
                interpolationDirection != InterpolationDirection.BottomEdge) return;
            
            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;
            int heightMapWidth = (int) (_settings.MapSize / _settings.Spacing + 1);

            int startX;
            int endX;
            int startZ;
            int endZ;

            switch (interpolationDirection)
            {
                case InterpolationDirection.LeftEdge:
                    startX = 0;
                    endX = interpolationMargin;
                    startZ = 0;
                    endZ = vertexSideCount;
                    break;
                case InterpolationDirection.RightEdge:
                    startX = vertexSideCount - interpolationMargin;
                    endX = vertexSideCount;
                    startZ = 0;
                    endZ = vertexSideCount;
                    break;
                case InterpolationDirection.BottomEdge:
                    startX = 0;
                    endX = vertexSideCount;
                    startZ = 0;
                    endZ = interpolationMargin;
                    break;
                case InterpolationDirection.TopEdge:
                    startX = 0;
                    endX = vertexSideCount;
                    startZ = vertexSideCount - interpolationMargin;
                    endZ = vertexSideCount;
                    break;
                default:
                    // Not possible
                    throw new ArgumentOutOfRangeException();
            }

            for (int z = startZ; z < endZ; z++)
            {
                // t always increases towards the centre, so a is always on the background chunk and b is always on the
                // play region chunk
                int aZ;
                int aPrevZ;
                int bZ;
                int bNextZ;

                switch (interpolationDirection)
                {
                    case InterpolationDirection.LeftEdge:
                        aZ = z;
                        aPrevZ = z;
                        bZ = z * lodFactor + zChunkOffset;
                        bNextZ = z * lodFactor + zChunkOffset;
                        break;
                    case InterpolationDirection.RightEdge:
                        aZ = z;
                        aPrevZ = z;
                        bZ = z * lodFactor + zChunkOffset;
                        bNextZ = z * lodFactor + zChunkOffset;
                        break;
                    case InterpolationDirection.BottomEdge:
                        aZ = _interpolationMargin;
                        aPrevZ = _interpolationMargin + 1;
                        bZ = heightMapWidth - 1;
                        bNextZ = heightMapWidth - 1 - lodFactor;
                        break;
                    case InterpolationDirection.TopEdge:
                        aZ = vertexSideCount - interpolationMargin - 1;
                        aPrevZ = vertexSideCount - interpolationMargin - 2;
                        bZ = 0;
                        bNextZ = lodFactor;
                        break;
                    default:
                        // Not possible
                        throw new ArgumentOutOfRangeException();
                }

                for (int x = startX; x < endX; x++)
                {
                    int aX;
                    int aPrevX;
                    int bX;
                    int bNextX;
                    float t;

                    switch (interpolationDirection)
                    {
                        case InterpolationDirection.LeftEdge:
                            aX = _interpolationMargin;
                            aPrevX = _interpolationMargin + 1;
                            bX = heightMapWidth - 1;
                            bNextX = heightMapWidth - 1 - lodFactor;
                            t = (_interpolationMargin - x) / (float)interpolationMargin;
                            break;
                        case InterpolationDirection.RightEdge:
                            aX = vertexSideCount - interpolationMargin - 1;
                            aPrevX = vertexSideCount - interpolationMargin - 2;
                            bX = 0;
                            bNextX = lodFactor;
                            t = (x - (vertexSideCount - interpolationMargin - 1)) / (float)interpolationMargin;
                            break;
                        case InterpolationDirection.BottomEdge:
                            aX = x;
                            aPrevX = x;
                            bX = x * lodFactor + xChunkOffset;
                            bNextX = x * lodFactor + xChunkOffset;
                            t = (_interpolationMargin - z) / (float)interpolationMargin;
                            break;
                        case InterpolationDirection.TopEdge:
                            aX = x;
                            aPrevX = x;
                            bX = x * lodFactor + xChunkOffset;
                            bNextX = x * lodFactor + xChunkOffset;
                            t = (z - (vertexSideCount - interpolationMargin - 1)) / (float)interpolationMargin;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();  // Not possible
                    }

                    // vertices is z-major, heightMap is x-major
                    var a = vertices[aZ + aX*vertexSideCount].y;
                    var aPrev = vertices[aPrevZ + aPrevX*vertexSideCount].y;
                    var aGrad = (a - aPrev) / _settings.Spacing * interpolationMargin;  // Gradient is difference in y per unit of t
                    var b = _heightMap[bZ*heightMapWidth + bX];
                    var bNext = _heightMap[bNextZ*heightMapWidth + bNextX];
                    var bGrad = (bNext - b) / _settings.Spacing * interpolationMargin;
                    if (z == 0) Debug.Log("a: " + a + " aPrev: " + aPrev + " m_a: " + aGrad + " b: " + b + " bNext: " + bNext + " m_b: " + bGrad + " spacing: " + _settings.Spacing);
                    if (z == 0) Debug.Log("t: " + t);

                    // Cubic Hermite interpolation
                    var y = a + aGrad * t + (3 * (b - a) - 2 * aGrad - bGrad) * t * t + (2 * (a - b) + aGrad + bGrad) * t * t * t;
                    vertices[z + x*vertexSideCount].y = y;
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
                var perlinX = (x + xChunkOffset + xExtraOffset) * _settings.NoiseScale;
                var perlinY = (z + zChunkOffset + zExtraOffset) * _settings.NoiseScale;
                var y = Mathf.PerlinNoise(perlinX, perlinY) * _settings.HeightScale
                    - (_settings.HeightScale / 2) + _settings.AverageHeight;
                vertices[i] = new Vector3(x * _settings.Spacing, y, z * _settings.Spacing);
                uvs[i] = new Vector2((float) x / _meshData.VertexSideCount, (float) z / _meshData.VertexSideCount);
            }
            
            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = i / _colliderMeshData.VertexSideCount * _colliderMeshData.LODFactor;
                var z = i % _colliderMeshData.VertexSideCount * _colliderMeshData.LODFactor;
                var perlinX = (x + xChunkOffset + xExtraOffset) * _settings.NoiseScale;
                var perlinY = (z + zChunkOffset + zExtraOffset) * _settings.NoiseScale;
                var y = Mathf.PerlinNoise(perlinX, perlinY) * _settings.HeightScale 
                    - (_settings.HeightScale / 2) + _settings.AverageHeight;
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