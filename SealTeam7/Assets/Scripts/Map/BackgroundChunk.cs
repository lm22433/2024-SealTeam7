using System;
using Game;
using Unity.Mathematics;
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
        internal struct InterpolateMarginDiagonalKernelReturnType
        {
            public float GradPerpInner;
            public float GradPerpOuter;
            public float A;
            public float AGrad;
            public float B;
            public float BGrad;
        }
        
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
        private int _heightMapWidth;

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
            
            _interpolationMargin = (int)((_settings.InterpolationMargin / (float)_settings.Size) * (_meshData.VertexSideCount - 1));
            _colliderInterpolationMargin = (int)((_settings.InterpolationMargin / (float)_settings.Size) * (_colliderMeshData.VertexSideCount - 1));
            _heightMap = heightMap;
            _heightMapWidth = (int)(_settings.MapSize / _settings.Spacing + 1);
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
            int zChunkOffset = _settings.Z * _settings.Size;
            int xChunkOffset = _settings.X * _settings.Size;

            switch (interpolationDirection)
            {
                case InterpolationDirection.LeftEdge:

                    for (int z = 0; z < vertexSideCount; z++)
                    {
                        for (int x = 0; x < interpolationMargin; x++)
                        {
                            // t always increases towards the centre, so a is always on the background chunk and b is always on the
                            // play region chunk
                            InterpolateMarginEdgeKernel(vertices, interpolationMargin, vertexSideCount, z, x,
                                aZ: z, 
                                aX: interpolationMargin, 
                                aPrevZ: z, 
                                aPrevX: interpolationMargin + 1, 
                                bZ: z * lodFactor + zChunkOffset, 
                                bX: _heightMapWidth - 1, 
                                bNextZ: z * lodFactor + zChunkOffset, 
                                bNextX: _heightMapWidth - 1 - lodFactor, 
                                t: (interpolationMargin - x) / (float)interpolationMargin);
                        }
                    }
                    break;
                
                case InterpolationDirection.RightEdge:

                    for (int z = 0; z < vertexSideCount; z++)
                    {
                        for (int x = vertexSideCount - interpolationMargin; x < vertexSideCount; x++)
                        {
                            InterpolateMarginEdgeKernel(vertices, interpolationMargin, vertexSideCount, z, x, 
                                aZ: z, 
                                aX: vertexSideCount - interpolationMargin - 1, 
                                aPrevZ: z, 
                                aPrevX: vertexSideCount - interpolationMargin - 2, 
                                bZ: z * lodFactor + zChunkOffset, 
                                bX: 0, 
                                bNextZ: z * lodFactor + zChunkOffset, 
                                bNextX: lodFactor, 
                                t: (x - (vertexSideCount - interpolationMargin - 1)) / (float)interpolationMargin);
                        }
                    }
                    break;

                case InterpolationDirection.BottomEdge:

                    for (int z = 0; z < interpolationMargin; z++)
                    {
                        for (int x = 0; x < vertexSideCount; x++)
                        {
                            InterpolateMarginEdgeKernel(vertices, interpolationMargin, vertexSideCount, z, x, 
                                aZ: interpolationMargin, 
                                aX: x, 
                                aPrevZ: interpolationMargin + 1, 
                                aPrevX: x, 
                                bZ: _heightMapWidth - 1, 
                                bX: x * lodFactor + xChunkOffset, 
                                bNextZ: _heightMapWidth - 1 - lodFactor, 
                                bNextX: x * lodFactor + xChunkOffset, 
                                t: (interpolationMargin - z) / (float)interpolationMargin);
                        }
                    }
                    break;

                case InterpolationDirection.TopEdge:

                    for (int z = vertexSideCount - interpolationMargin; z < vertexSideCount; z++)
                    {
                        for (int x = 0; x < vertexSideCount; x++)
                        {
                            InterpolateMarginEdgeKernel(vertices, interpolationMargin, vertexSideCount, z, x, 
                                aZ: vertexSideCount - interpolationMargin - 1, 
                                aX: x, 
                                aPrevZ: vertexSideCount - interpolationMargin - 2, 
                                aPrevX: x, 
                                bZ: 0, 
                                bX: x * lodFactor + xChunkOffset, 
                                bNextZ: lodFactor, 
                                bNextX: x * lodFactor + xChunkOffset, 
                                t: (z - (vertexSideCount - interpolationMargin - 1)) / (float)interpolationMargin);
                        }
                    }
                    break;

                case InterpolationDirection.BottomLeftCorner:

                    // Diagonal
                    var dkrt = new InterpolateMarginDiagonalKernelReturnType();
                    for (int z = 0; z < interpolationMargin; z++)
                    {
                        // Returned dkrt is the same for all iterations
                        dkrt = InterpolateMarginDiagonalKernel(vertices, interpolationMargin, vertexSideCount, 
                            z: z, 
                            x: z, 
                            aZ: interpolationMargin, 
                            aX: interpolationMargin, 
                            aPrevZ: interpolationMargin + 1, 
                            aPrevX: interpolationMargin + 1, 
                            bZ: _heightMapWidth - 1, 
                            bX: _heightMapWidth - 1, 
                            bNextZ: _heightMapWidth - 1 - lodFactor, 
                            bNextX: _heightMapWidth - 1 - lodFactor, 
                            t: (interpolationMargin - z) / (float)interpolationMargin);
                    }

                    // Bottom/right triangle
                    for (int z = 0; z < interpolationMargin; z++)
                    {
                        var bGradPerp = Mathf.SmoothStep(dkrt.GradPerpInner, dkrt.GradPerpOuter, z/(float)interpolationMargin);
                        for (int x = z; x < interpolationMargin; x++)
                        {
                            InterpolateMarginTriangleKernel(vertices, interpolationMargin, vertexSideCount, z, x,
                                aZ: z,
                                aX: interpolationMargin,
                                aPrevZ: z,
                                aPrevX: interpolationMargin + 1,
                                bZ: z,
                                bX: z,
                                t: (interpolationMargin - x) / (float)(interpolationMargin - z),  // denominator=tUnitLength
                                tUnitLength: interpolationMargin - z, 
                                dkrt: dkrt,
                                bGradPerp: bGradPerp,
                                bGradDir: new Vector2(1, 1));
                        }
                    }
                    
                    // Top/left triangle
                    for (int x = 0; x < interpolationMargin; x++)
                    {
                        var bGradPerp = Mathf.SmoothStep(dkrt.GradPerpInner, dkrt.GradPerpOuter, x/(float)interpolationMargin);
                        for (int z = x; z < interpolationMargin; z++)
                        {
                            InterpolateMarginTriangleKernel(vertices, interpolationMargin, vertexSideCount, z, x,
                                aZ: interpolationMargin,
                                aX: x,
                                aPrevZ: interpolationMargin + 1,
                                aPrevX: x,
                                bZ: x,
                                bX: x,
                                t: (interpolationMargin - z) / (float)(interpolationMargin - x),
                                tUnitLength: interpolationMargin - x,
                                dkrt: dkrt,
                                bGradPerp: bGradPerp,
                                bGradDir: new Vector2(1, -1));
                        }
                    }
                    break;
                
                case InterpolationDirection.BottomRightCorner:

                    // Diagonal
                    dkrt = new InterpolateMarginDiagonalKernelReturnType();
                    for (int z = 0; z < interpolationMargin; z++)
                    {
                        // Returned dkrt is the same for all iterations
                        dkrt = InterpolateMarginDiagonalKernel(vertices, interpolationMargin, vertexSideCount, 
                            z: z, 
                            x: vertexSideCount - 1 - z, 
                            aZ: interpolationMargin, 
                            aX: vertexSideCount - interpolationMargin - 1, 
                            aPrevZ: interpolationMargin + 1, 
                            aPrevX: vertexSideCount - interpolationMargin - 2, 
                            bZ: _heightMapWidth - 1, 
                            bX: 0, 
                            bNextZ: _heightMapWidth - 1 - lodFactor, 
                            bNextX: lodFactor, 
                            t: (interpolationMargin - z) / (float)interpolationMargin);
                    }

                    // Bottom/left triangle
                    for (int z = 0; z < interpolationMargin; z++)
                    {
                        var bGradPerp = Mathf.SmoothStep(dkrt.GradPerpInner, dkrt.GradPerpOuter, z/(float)interpolationMargin);
                        for (int x = vertexSideCount - interpolationMargin; x < vertexSideCount - z; x++)
                        {
                            InterpolateMarginTriangleKernel(vertices, interpolationMargin, vertexSideCount, z, x,
                                aZ: z,
                                aX: vertexSideCount - interpolationMargin - 1,
                                aPrevZ: z,
                                aPrevX: vertexSideCount - interpolationMargin - 2,
                                bZ: z,
                                bX: vertexSideCount - 1 - z,
                                t: (x - (vertexSideCount - interpolationMargin - 1)) / (float)(interpolationMargin - z),  // denominator=tUnitLength
                                tUnitLength: interpolationMargin - z, 
                                dkrt: dkrt,
                                bGradPerp: bGradPerp,
                                bGradDir: new Vector2(1, 1));
                        }
                    }
                    
                    // Top/right triangle
                    for (int x = vertexSideCount - interpolationMargin; x < vertexSideCount; x++)
                    {
                        var bGradPerp = Mathf.SmoothStep(dkrt.GradPerpInner, dkrt.GradPerpOuter, (vertexSideCount - 1 - x)/(float)interpolationMargin);
                        for (int z = interpolationMargin - (x - (vertexSideCount - interpolationMargin) + 1); z < interpolationMargin; z++)
                        {
                            InterpolateMarginTriangleKernel(vertices, interpolationMargin, vertexSideCount, z, x,
                                aZ: interpolationMargin,
                                aX: x,
                                aPrevZ: interpolationMargin + 1,
                                aPrevX: x,
                                bZ: interpolationMargin - (x - (vertexSideCount - interpolationMargin)) - 1,
                                bX: x,
                                t: (interpolationMargin - z) / (float)(x - (vertexSideCount - interpolationMargin) + 1),
                                tUnitLength: x - (vertexSideCount - interpolationMargin) + 1,
                                dkrt: dkrt,
                                bGradPerp: bGradPerp,
                                bGradDir: new Vector2(1, -1));
                        }
                    }
                    break;

                default:
                    // Not possible
                    break;
            }
        }

        private void InterpolateMarginEdgeKernel(Vector3[] vertices, int interpolationMargin, int vertexSideCount, int z, int x,
            int aZ, int aX, int aPrevZ, int aPrevX, int bZ, int bX, int bNextZ, int bNextX, float t)
        {
            // vertices is z-major, heightMap is x-major
            var a = vertices[aZ + aX*vertexSideCount].y;
            var aPrev = vertices[aPrevZ + aPrevX*vertexSideCount].y;
            var aGrad = (a - aPrev) / _settings.Spacing * interpolationMargin;  // Gradient is difference in y per unit of t
            var b = _heightMap[bZ*_heightMapWidth + bX];
            var bNext = _heightMap[bNextZ*_heightMapWidth + bNextX];
            var bGrad = (bNext - b) / _settings.Spacing * interpolationMargin;

            // if (vertices.Equals(_colliderMeshData.Vertices) && interpolationDirection == InterpolationDirection.LeftEdge) {
            //     if (z == 0) Debug.Log("a: " + a + " aPrev: " + aPrev + " m_a: " + aGrad + " b: " + b + " bNext: " + bNext + " m_b: " + bGrad + " spacing: " + _settings.Spacing);
            //     if (z == 0) Debug.Log("t: " + t);
            // }

            // Cubic Hermite interpolation
            var y = a + aGrad * t + (3 * (b - a) - 2 * aGrad - bGrad) * t * t + (2 * (a - b) + aGrad + bGrad) * t * t * t;
            vertices[z + x*vertexSideCount].y = y;
        }
        
        private InterpolateMarginDiagonalKernelReturnType InterpolateMarginDiagonalKernel(
            Vector3[] vertices, int interpolationMargin, int vertexSideCount, int z, int x,
            int aZ, int aX, int aPrevZ, int aPrevX, int bZ, int bX, int bNextZ, int bNextX, float t)
        {
            // vertices is z-major, heightMap is x-major
            var a = vertices[aZ + aX*vertexSideCount].y;
            var aPrevAlongX = vertices[aZ + aPrevX*vertexSideCount].y;
            var aPrevAlongZ = vertices[aPrevZ + aX*vertexSideCount].y;
            var aGradX = (a - aPrevAlongX) / _settings.Spacing * (interpolationMargin * Mathf.Sqrt(2));
            var aGradZ = (a - aPrevAlongZ) / _settings.Spacing * (interpolationMargin * Mathf.Sqrt(2));
            var aGrad = (aGradX + aGradZ) / Mathf.Sqrt(2);
            var b = _heightMap[bZ*_heightMapWidth + bX];
            var bNextAlongX = _heightMap[bZ*_heightMapWidth + bNextX];
            var bNextAlongZ = _heightMap[bNextZ*_heightMapWidth + bX];
            var bGradX = (bNextAlongX - b) / _settings.Spacing * (interpolationMargin * Mathf.Sqrt(2));
            var bGradZ = (bNextAlongZ - b) / _settings.Spacing * (interpolationMargin * Mathf.Sqrt(2));
            var bGrad = (bGradX + bGradZ) / Mathf.Sqrt(2);

            // if (vertices.Equals(_colliderMeshData.Vertices) && interpolationDirection == InterpolationDirection.LeftEdge) {
            //     if (z == 0) Debug.Log("a: " + a + " aPrev: " + aPrev + " m_a: " + aGrad + " b: " + b + " bNext: " + bNext + " m_b: " + bGrad + " spacing: " + _settings.Spacing);
            //     if (z == 0) Debug.Log("t: " + t);
            // }

            // Cubic Hermite interpolation
            var y = a + aGrad * t + (3 * (b - a) - 2 * aGrad - bGrad) * t * t + (2 * (a - b) + aGrad + bGrad) * t * t * t;
            vertices[z + x*vertexSideCount].y = y;
            
            // Perpendicular gradients only need to be calculated once so I do them here
            var gradPerpInner = (bGradX - bGradZ) / 2;  // Extra factor of sqrt 2 to scale gradient properly
            var gradPerpOuter = (aGradX - aGradZ) / 2;
            
            return new InterpolateMarginDiagonalKernelReturnType
            {
                GradPerpInner = gradPerpInner, 
                GradPerpOuter = gradPerpOuter,
                A = a, 
                AGrad = aGrad, 
                B = b, 
                BGrad = bGrad
            };
        }
        
        private void InterpolateMarginTriangleKernel(Vector3[] vertices, int interpolationMargin, int vertexSideCount, 
            int z, int x, int aZ, int aX, int aPrevZ, int aPrevX, int bZ, int bX, float t, int tUnitLength,
            InterpolateMarginDiagonalKernelReturnType dkrt, float bGradPerp, Vector2 bGradDir)
        {
            var diagT = tUnitLength/(float)interpolationMargin;
            var bGradPara = dkrt.AGrad + (-6*(dkrt.A - dkrt.B) - 4*dkrt.AGrad - 2*dkrt.BGrad)*diagT + 
                            (6*(dkrt.A - dkrt.B) + 3*dkrt.AGrad + 3*dkrt.BGrad)*diagT*diagT;
            var bGradParaScaled = bGradPara / interpolationMargin * tUnitLength / Mathf.Sqrt(2);
            var bGradPerpScaled = bGradPerp / interpolationMargin * tUnitLength;
            var bGrad = (bGradDir.x*bGradParaScaled + bGradDir.y*bGradPerpScaled) / Mathf.Sqrt(2);
            
            // vertices is z-major, heightMap is x-major
            var a = vertices[aZ + aX*vertexSideCount].y;
            var aPrev = vertices[aPrevZ + aPrevX*vertexSideCount].y;
            var aGrad = (a - aPrev) / _settings.Spacing * tUnitLength;  // Gradient is difference in y per unit of t
            var b = vertices[bZ + bX*vertexSideCount].y;
            
            // Cubic Hermite interpolation
            var y = a + aGrad * t + (3 * (b - a) - 2 * aGrad - bGrad) * t * t + (2 * (a - b) + aGrad + bGrad) * t * t * t;
            vertices[z + x*vertexSideCount].y = y;
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