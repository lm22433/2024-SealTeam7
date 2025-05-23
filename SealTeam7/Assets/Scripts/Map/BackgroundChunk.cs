using Game;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.Rendering;

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
        public float BaseHeightScale;
        public float BaseNoiseScale;
        public float RoughnessHeightScale;
        public float RoughnessNoiseScale;
        public InterpolationDirection InterpolationDirection;
        // Margin width measured in world units
        public int InterpolationMargin;
    }

    public class BackgroundChunk : MonoBehaviour
    {
        private BackgroundChunkSettings _settings;
        private float[,] _heightMap;
        
        private Mesh _mesh;
        private Mesh _colliderMesh;
        private SavedMeshData _savedMeshData;
        private int _lodFactor;
        private int _vertexSideCount;
        private int _colliderLodFactor;
        private int _colliderVertexSideCount;
        private MeshFilter _meshFilter;

        private float _averageHeight;
        private float _heightScale;
        private float _noiseScale;
        // Margin width measured as number of edges
        private int _interpolationMargin;
        // Margin width measured as number of edges
        private int _colliderInterpolationMargin;
        private int _heightMapWidth;
        private float[,] _perlinRoughness;
        private bool _recalcedTangents;
        private bool _meshNeedsUpdate;

        public void Setup(BackgroundChunkSettings s, ref float[,] heightMap)
        {
            _settings = s;
            
            _lodFactor = _settings.LODInfo.backgroundLod == 0 ? 1 : _settings.LODInfo.backgroundLod * 2;
            _vertexSideCount = _settings.Size / _lodFactor + 1;

            _colliderLodFactor = _settings.LODInfo.colliderLod == 0 ? 1 : _settings.LODInfo.colliderLod * 2;
            _colliderVertexSideCount = _settings.Size / _colliderLodFactor + 1;
            
            _perlinRoughness = new float[_settings.MapSize + 1, _settings.MapSize + 1];
            
            _mesh = new Mesh { name = "Generated Mesh", indexFormat = IndexFormat.UInt32 };
            _mesh.MarkDynamic();
            _colliderMesh = new Mesh { name = "Generated Collider Mesh", indexFormat = IndexFormat.UInt32 };
            _colliderMesh.MarkDynamic();
            _meshFilter = GetComponent<MeshFilter>();
            UpdateMesh();
            _meshFilter.sharedMesh = _mesh;
            
            _interpolationMargin = (int)((_settings.InterpolationMargin / (float)_settings.Size) * (_vertexSideCount - 1));
            _colliderInterpolationMargin = (int)((_settings.InterpolationMargin / (float)_settings.Size) * (_colliderVertexSideCount - 1));
            _heightMap = heightMap;
            _heightMapWidth = (int)(_settings.MapSize / _settings.Spacing + 1);
        }

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive())
            {
                _recalcedTangents = false;
                return;
            }
            
            if (_meshNeedsUpdate)
            {
                // _savedMeshData.Normals = _mesh.normals;
                _mesh.SetVertices(_savedMeshData.Vertices);
                // _mesh.RecalculateNormals();
                // if (!_recalcedTangents) _mesh.RecalculateTangents();
                _mesh.RecalculateBounds();

                _colliderMesh.SetVertices(_savedMeshData.ColliderVertices);
                _colliderMesh.RecalculateBounds();

                _meshNeedsUpdate = false;
            }

            _recalcedTangents = true;
        }

        public void UpdateHeights()
        {
            InterpolateMargin(_savedMeshData.Vertices, _interpolationMargin, _vertexSideCount, 
                _lodFactor, _settings.InterpolationDirection);
            // InterpolateMargin(_savedMeshData.ColliderVertices, _colliderInterpolationMargin, _colliderVertexSideCount, 
            //     _colliderLodFactor, _settings.InterpolationDirection);
            
            _meshNeedsUpdate = true;
        }

        private void InterpolateMargin(Vector3[] vertices, int interpolationMargin, int vertexSideCount, int lodFactor, 
            InterpolationDirection interpolationDirection)
        {          
            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            switch (interpolationDirection)
            {
                case InterpolationDirection.LeftEdge:

                    Parallel.For(0, vertexSideCount, z =>
                    {
                        for (int x = 0; x < interpolationMargin; x++)
                        {
                            // t always increases towards the centre, so a is always on the background chunk and b is always on the
                            // play region chunk
                            InterpolateMarginEdgeKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: z,
                                aX: interpolationMargin,
                                bZ: z * lodFactor + zChunkOffset,
                                bX: _heightMapWidth - 1,
                                t: (interpolationMargin - x) / (float)interpolationMargin);
                        }
                    });
                    break;
                
                case InterpolationDirection.RightEdge:

                    Parallel.For(0, vertexSideCount, z =>
                    {
                        for (int x = vertexSideCount - interpolationMargin; x < vertexSideCount; x++)
                        {
                            InterpolateMarginEdgeKernel(vertices, vertexSideCount, lodFactor, z, x, 
                                aZ: z, 
                                aX: vertexSideCount - interpolationMargin - 1, 
                                bZ: z * lodFactor + zChunkOffset, 
                                bX: 0, 
                                t: (x - (vertexSideCount - interpolationMargin - 1)) / (float)interpolationMargin);
                        }
                    });
                    break;

                case InterpolationDirection.BottomEdge:

                    Parallel.For(0, interpolationMargin, z =>
                    {
                        for (int x = 0; x < vertexSideCount; x++)
                        {
                            InterpolateMarginEdgeKernel(vertices, vertexSideCount, lodFactor, z, x, 
                                aZ: interpolationMargin, 
                                aX: x, 
                                bZ: _heightMapWidth - 1, 
                                bX: x * lodFactor + xChunkOffset, 
                                t: (interpolationMargin - z) / (float)interpolationMargin);
                        }
                    });
                    break;

                case InterpolationDirection.TopEdge:

                    Parallel.For(vertexSideCount - interpolationMargin, vertexSideCount, z =>
                    {
                        for (int x = 0; x < vertexSideCount; x++)
                        {
                            InterpolateMarginEdgeKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: vertexSideCount - interpolationMargin - 1,
                                aX: x,
                                bZ: 0,
                                bX: x * lodFactor + xChunkOffset,
                                t: (z - (vertexSideCount - interpolationMargin - 1)) / (float)interpolationMargin);
                        }
                    });
                    break;

                case InterpolationDirection.BottomLeftCorner:

                    // Diagonal
                    for (int z = 0; z < interpolationMargin; z++)
                    {
                        InterpolateMarginEdgeKernel(vertices, vertexSideCount, lodFactor, 
                            z: z, 
                            x: z, 
                            aZ: interpolationMargin, 
                            aX: interpolationMargin, 
                            bZ: _heightMapWidth - 1, 
                            bX: _heightMapWidth - 1, 
                            t: (interpolationMargin - z) / (float)interpolationMargin);
                    }

                    // Bottom/right triangle
                    Parallel.For(0, interpolationMargin, z =>
                    {
                        for (int x = z; x < interpolationMargin; x++)
                        {
                            InterpolateMarginTriangleKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: z,
                                aX: interpolationMargin,
                                bZ: z,
                                bX: z,
                                t: (interpolationMargin - x) / (float)(interpolationMargin - z));
                        }
                    });
                    
                    // Top/left triangle
                    Parallel.For(0, interpolationMargin, x =>
                    {
                        for (int z = x; z < interpolationMargin; z++)
                        {
                            InterpolateMarginTriangleKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: interpolationMargin,
                                aX: x,
                                bZ: x,
                                bX: x,
                                t: (interpolationMargin - z) / (float)(interpolationMargin - x));
                        }
                    });
                    break;
                
                case InterpolationDirection.BottomRightCorner:

                    // Diagonal
                    for (int z = 0; z < interpolationMargin; z++)
                    {
                        InterpolateMarginEdgeKernel(vertices, vertexSideCount, lodFactor, 
                            z: z, 
                            x: vertexSideCount - 1 - z, 
                            aZ: interpolationMargin, 
                            aX: vertexSideCount - interpolationMargin - 1, 
                            bZ: _heightMapWidth - 1, 
                            bX: 0, 
                            t: (interpolationMargin - z) / (float)interpolationMargin);
                    }

                    // Bottom/left triangle
                    Parallel.For(0, interpolationMargin, z =>
                    {
                        for (int x = vertexSideCount - interpolationMargin; x < vertexSideCount - z; x++)
                        {
                            InterpolateMarginTriangleKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: z,
                                aX: vertexSideCount - interpolationMargin - 1,
                                bZ: z,
                                bX: vertexSideCount - 1 - z,
                                t: (x - (vertexSideCount - interpolationMargin - 1)) / (float)(interpolationMargin - z));
                        }
                    });
                    
                    // Top/right triangle
                    Parallel.For(vertexSideCount - interpolationMargin, vertexSideCount, x =>
                    {
                        for (int z = vertexSideCount - 1 - x; z < interpolationMargin; z++)
                        {
                            InterpolateMarginTriangleKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: interpolationMargin,
                                aX: x,
                                bZ: vertexSideCount - 1 - x,
                                bX: x,
                                t: (interpolationMargin - z) / (float)(x - (vertexSideCount - interpolationMargin) + 1));
                        }
                    });
                    break;
                
                case InterpolationDirection.TopLeftCorner:

                    // Diagonal
                    for (int x = 0; x < interpolationMargin; x++)
                    {
                        InterpolateMarginEdgeKernel(vertices, vertexSideCount, lodFactor, 
                            z: vertexSideCount - 1 - x, 
                            x: x, 
                            aZ: vertexSideCount - interpolationMargin - 1, 
                            aX: interpolationMargin, 
                            bZ: 0, 
                            bX: _heightMapWidth - 1, 
                            t: (interpolationMargin - x) / (float)interpolationMargin);
                    }

                    // Bottom/left triangle
                    Parallel.For(0, interpolationMargin, x =>
                    {
                        for (int z = vertexSideCount - interpolationMargin; z < vertexSideCount - x; z++)
                        {
                            InterpolateMarginTriangleKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: vertexSideCount - interpolationMargin - 1,
                                aX: x,
                                bZ: vertexSideCount - 1 - x,
                                bX: x,
                                t: (z - (vertexSideCount - interpolationMargin - 1)) / (float)(interpolationMargin - x));
                        }
                    });
                    
                    // Top/right triangle
                    Parallel.For(vertexSideCount - interpolationMargin, vertexSideCount, z =>
                    {
                        for (int x = vertexSideCount - 1 - z; x < interpolationMargin; x++)
                        {
                            InterpolateMarginTriangleKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: z,
                                aX: interpolationMargin,
                                bZ: z,
                                bX: vertexSideCount - 1 - z,
                                t: (interpolationMargin - x) / (float)(z - (vertexSideCount - interpolationMargin) + 1));
                        }
                    });
                    break;
                
                case InterpolationDirection.TopRightCorner:

                    // Diagonal
                    for (int i = 0; i < interpolationMargin; i++)
                    {
                        InterpolateMarginEdgeKernel(vertices, vertexSideCount, lodFactor, 
                            z: vertexSideCount - interpolationMargin + i, 
                            x: vertexSideCount - interpolationMargin + i, 
                            aZ: vertexSideCount - interpolationMargin - 1, 
                            aX: vertexSideCount - interpolationMargin - 1, 
                            bZ: 0, 
                            bX: 0, 
                            t: (i + 1)/(float)interpolationMargin);
                    }

                    // Bottom/right triangle
                    Parallel.For(vertexSideCount - interpolationMargin, vertexSideCount, x =>
                    {
                        for (int z = vertexSideCount - interpolationMargin; z < x + 1; z++)
                        {
                            InterpolateMarginTriangleKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: vertexSideCount - interpolationMargin - 1,
                                aX: x,
                                bZ: x,
                                bX: x,
                                t: (z - (vertexSideCount - interpolationMargin - 1)) / (float)(x - (vertexSideCount - interpolationMargin - 1)));
                        }
                    });
                    
                    // Top/left triangle
                    Parallel.For(vertexSideCount - interpolationMargin, vertexSideCount, z =>
                    {
                        for (int x = vertexSideCount - interpolationMargin; x < z + 1; x++)
                        {
                            InterpolateMarginTriangleKernel(vertices, vertexSideCount, lodFactor, z, x,
                                aZ: z,
                                aX: vertexSideCount - interpolationMargin - 1,
                                bZ: z,
                                bX: z,
                                t: (x - (vertexSideCount - interpolationMargin - 1)) / (float)(z - (vertexSideCount - interpolationMargin) + 1));
                        }
                    });
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InterpolateMarginEdgeKernel(Vector3[] vertices, int vertexSideCount,
            int lodFactor, int z, int x, int aZ, int aX, int bZ, int bX, float t)
        {
            // vertices is z-major, heightMap is x-major
            var a = vertices[aZ + aX*vertexSideCount].y;
            var b = _heightMap[bZ, bX];
            var y = Mathf.SmoothStep(a, b, t);
            
            float yRoughness;
            if (z != 0 && x != 0 && z != vertexSideCount - 1 && x != vertexSideCount - 1)
            {
                yRoughness = _perlinRoughness[z * lodFactor, x * lodFactor];
            }
            else yRoughness = 0;
            
            vertices[z + x*vertexSideCount].y = y + yRoughness;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InterpolateMarginTriangleKernel(Vector3[] vertices, int vertexSideCount,
            int lodFactor, int z, int x, int aZ, int aX, int bZ, int bX, float t)
        {
            var a = vertices[aZ + aX*vertexSideCount].y;
            var b = vertices[bZ + bX*vertexSideCount].y;
            var y = Mathf.SmoothStep(a, b, t);
            
            float yRoughness;
            if (z != 0 && x != 0 && z != vertexSideCount - 1 && x != vertexSideCount - 1)
            {
                yRoughness = _perlinRoughness[z * lodFactor, x * lodFactor];
            }
            else yRoughness = 0;
            
            vertices[z + x*vertexSideCount].y = y + yRoughness;
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
            int zExtraOffset = 5 * _settings.Size;  // Ensures positive region of Perlin noise
            int xExtraOffset = 5 * _settings.Size;

            for (int i = 0; i < (_settings.MapSize + 1) * (_settings.MapSize + 1); i++)
            {
                var x = i / (_settings.MapSize + 1);
                var z = i % (_settings.MapSize + 1);
                var perlinX = x + xChunkOffset + xExtraOffset;
                var perlinY = z + zChunkOffset + zExtraOffset;
                var yRoughness = Mathf.PerlinNoise(perlinX*_settings.RoughnessNoiseScale, perlinY*_settings.RoughnessNoiseScale) 
                    *_settings.RoughnessHeightScale - _settings.RoughnessHeightScale/2;
                _perlinRoughness[z,x] = yRoughness;
            }
            
            for (int i = 0; i < numberOfVertices; i++)
            {
                var x = i / _vertexSideCount * _lodFactor;
                var z = i % _vertexSideCount * _lodFactor;
                var perlinX = x + xChunkOffset + xExtraOffset;
                var perlinY = z + zChunkOffset + zExtraOffset;
                var y = Mathf.PerlinNoise(perlinX*_settings.BaseNoiseScale, perlinY*_settings.BaseNoiseScale) 
                    *_settings.BaseHeightScale - _settings.BaseHeightScale/2 + _settings.AverageHeight;
                
                float yRoughness;
                if (z != 0 && x != 0 && z != _vertexSideCount - 1 && x != _vertexSideCount - 1)
                {
                    yRoughness = _perlinRoughness[z, x];
                }
                else yRoughness = 0;
                
                vertices[i] = new Vector3(x * _settings.Spacing, y + yRoughness, z * _settings.Spacing);
                uvs[i] = new Vector2((float) x / _vertexSideCount, (float) z / _vertexSideCount);
            }
            
            for (int i = 0; i < colliderNumberOfVertices; i++)
            {
                var x = i / _colliderVertexSideCount * _colliderLodFactor;
                var z = i % _colliderVertexSideCount * _colliderLodFactor;
                var perlinX = x + xChunkOffset + xExtraOffset;
                var perlinY = z + zChunkOffset + zExtraOffset;
                var y = Mathf.PerlinNoise(perlinX*_settings.BaseNoiseScale, perlinY*_settings.BaseNoiseScale) 
                    *_settings.BaseHeightScale - _settings.BaseHeightScale/2 + _settings.AverageHeight;
                
                float yRoughness;
                if (z != 0 && x != 0 && z != _colliderVertexSideCount - 1 && x != _colliderVertexSideCount - 1)
                {
                    yRoughness = _perlinRoughness[z, x];
                }
                else yRoughness = 0;

                colliderVertices[i] = new Vector3(x * _settings.Spacing, y + yRoughness, z * _settings.Spacing);
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
            // _mesh.RecalculateTangents();
            
            _colliderMesh.Clear();
            _colliderMesh.SetVertices(colliderVertices);
            _colliderMesh.SetTriangles(colliderTriangles, 0);
            
            _savedMeshData.Vertices = vertices;
            _savedMeshData.Normals = _mesh.normals;
            _savedMeshData.ColliderVertices = colliderVertices;
        }
    }
}