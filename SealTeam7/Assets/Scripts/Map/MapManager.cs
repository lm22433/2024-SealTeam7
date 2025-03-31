using System;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using Enemies.Utils;
using Unity.Collections;
using UnityEngine;

namespace Map
{
    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public int colliderLod;
        public int pathingLod;
    }

    public class MapManager : MonoBehaviour
    {
        [Header("Noise Settings")]
        [Header("")]
        [SerializeField] private float noiseSpeed;
        [SerializeField] private float noiseScale;

        [Header("")]
        [Header("Kinect Settings")]
        [Header("")]
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField, Range(0f, 1920f)] private int xOffsetStart;
        [SerializeField, Range(0f, 1920f)] private int xOffsetEnd;
        [SerializeField, Range(0f, 1080f)] private int yOffsetStart;
        [SerializeField, Range(0f, 1080f)] private int yOffsetEnd;
        [Header("")]
        [SerializeField, Range(300f, 2000f)] private ushort minimumSandDepth;
        [SerializeField, Range(600f, 2000f)] private ushort maximumSandDepth;
        [Header("")]
        [SerializeField, Range(0, 255f)] private int irThreshold;
        [Header("")]
        [SerializeField, Range(0.5f, 1f)] private float similarityThreshold;

        [Header("")]
        [Header("Blur Settings")]
        [Header("")]
        [SerializeField] private int kernelSize;
        [SerializeField] private float gaussianStrength;

        [Header("")]
        [Header("Map Settings")]
        [Header("")]
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private int mapSize;
        [SerializeField] private int chunkRow;
        [SerializeField] private int chunkSize;
        [SerializeField] private float heightScale;
        [SerializeField] private float lerpFactor;
        [SerializeField] private LODInfo lodInfo;

        [Header("")]
        [Header("Infinite Sand Settings")]
        [Header("")]
        [SerializeField] private GameObject bgChunkPrefab;
        [SerializeField] private float bgAverageHeight;
        [SerializeField] private float bgBaseHeightScale;
        [SerializeField] private float bgBaseNoiseScale;
        [SerializeField] private float bgRoughnessHeightScale;
        [SerializeField] private float bgRoughnessNoiseScale;
        [SerializeField] private int bgInterpolationMargin;
        [SerializeField] private int bgChunkMargin;

        [Header("")]
        [Header("Environment Settings")]
        [Header("")]
        [SerializeField] private bool isKinectPresent;
        [Header("")]
        [Header("Debug")]
        [Header("")]
        [SerializeField] private bool colliderEnabled;
        [SerializeField] private bool paused;
        [SerializeField] private bool takeSnapshot;
        [SerializeField] private Texture2D texture;

        private NoiseGenerator _noiseGenerator;
        private KinectAPI _kinect;
        private float[,] _heightMap;
        private List<Chunk> _chunks;
        private List<BackgroundChunk> _bgChunks;
        private float _mapSpacing;

        private static MapManager _instance;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            _mapSpacing = (float)mapSize / chunkRow / chunkSize;
            _chunks = new List<Chunk>(chunkRow);
            _bgChunks = new List<BackgroundChunk>(chunkRow);
            _heightMap = new float[Mathf.RoundToInt(mapSize / _mapSpacing + 1), Mathf.RoundToInt(mapSize / _mapSpacing + 1)];

            var chunkParent = new GameObject("Chunks") { transform = { parent = transform } };

            texture = new Texture2D((int)(mapSize / _mapSpacing + 1), (int)(mapSize / _mapSpacing + 1), TextureFormat.RGBA32, false);

            ChunkSettings chunkSettings = new ChunkSettings
            {
                Size = chunkSize,
                MapSize = mapSize,
                MapSpacing = _mapSpacing,
                LODInfo = lodInfo,
                ColliderEnabled = colliderEnabled
            };

            BackgroundChunkSettings backgroundChunkSettings = new BackgroundChunkSettings
            {
                Size = chunkSize,
                MapSize = mapSize,
                Spacing = _mapSpacing,
                LODInfo = lodInfo,
                ColliderEnabled = colliderEnabled,
                AverageHeight = bgAverageHeight,
                BaseHeightScale = bgBaseHeightScale,
                BaseNoiseScale = bgBaseNoiseScale,
                RoughnessHeightScale = bgRoughnessHeightScale,
                RoughnessNoiseScale = bgRoughnessNoiseScale,
                InterpolationMargin = bgInterpolationMargin
            };

            if (isKinectPresent) _kinect = new KinectAPI(heightScale, lerpFactor, minimumSandDepth, maximumSandDepth, irThreshold, similarityThreshold, width, height, xOffsetStart, xOffsetEnd, yOffsetStart, yOffsetEnd, ref _heightMap, kernelSize, gaussianStrength, OnHeightUpdate);
            else _noiseGenerator = new NoiseGenerator((int)(mapSize / _mapSpacing), noiseSpeed, noiseScale, heightScale, ref _heightMap, OnHeightUpdate);

            for (int z = -bgChunkMargin; z < chunkRow + bgChunkMargin; z++)
            {
                for (int x = -bgChunkMargin; x < chunkRow + bgChunkMargin; x++)
                {
                    // Play region chunks
                    if (z >= 0 && z < chunkRow && x >= 0 && x < chunkRow)
                    {
                        var chunk = Instantiate(chunkPrefab,
                            new Vector3(x * chunkSize * _mapSpacing, 0f, z * chunkSize * _mapSpacing),
                            Quaternion.identity, chunkParent.transform).GetComponent<Chunk>();
                        chunkSettings.X = x;
                        chunkSettings.Z = z;
                        chunk.Setup(chunkSettings, ref _heightMap);
                        _chunks.Add(chunk);
                    }
                    // Background chunks
                    else
                    {
                        var chunk = Instantiate(bgChunkPrefab,
                            new Vector3(x * chunkSize * _mapSpacing, 0f, z * chunkSize * _mapSpacing),
                            Quaternion.identity, chunkParent.transform).GetComponent<BackgroundChunk>();
                        backgroundChunkSettings.X = x;
                        backgroundChunkSettings.Z = z;
                        if (x == -1 && z == -1) backgroundChunkSettings.InterpolationDirection = InterpolationDirection.TopRightCorner;
                        else if (x == -1 && z == chunkRow) backgroundChunkSettings.InterpolationDirection = InterpolationDirection.BottomRightCorner;
                        else if (x == chunkRow && z == -1) backgroundChunkSettings.InterpolationDirection = InterpolationDirection.TopLeftCorner;
                        else if (x == chunkRow && z == chunkRow) backgroundChunkSettings.InterpolationDirection = InterpolationDirection.BottomLeftCorner;
                        else if (x == -1 && z >= 0 && z < chunkRow) backgroundChunkSettings.InterpolationDirection = InterpolationDirection.RightEdge;
                        else if (x == chunkRow && z >= 0 && z < chunkRow) backgroundChunkSettings.InterpolationDirection = InterpolationDirection.LeftEdge;
                        else if (x >= 0 && x < chunkRow && z == -1) backgroundChunkSettings.InterpolationDirection = InterpolationDirection.TopEdge;
                        else if (x >= 0 && x < chunkRow && z == chunkRow) backgroundChunkSettings.InterpolationDirection = InterpolationDirection.BottomEdge;
                        else backgroundChunkSettings.InterpolationDirection = InterpolationDirection.None;
                        chunk.Setup(backgroundChunkSettings, ref _heightMap);
                        _bgChunks.Add(chunk);
                    }
                }
            }
        }

        private void OnHeightUpdate()
        {
            foreach (var chunk in _chunks)
            {
                chunk.UpdateHeights();
            }

            foreach (var chunk in _bgChunks)
            {
                chunk.UpdateHeights();
            }
        }

        private void OnApplicationQuit()
        {
            Quit();
        }

        public void Quit()
        {
            if (isKinectPresent) _kinect.StopKinect();
            else _noiseGenerator.Stop();
        }

        public float GetHeight(Vector3 position)
        {
            var percentX = position.x / (mapSize * _mapSpacing);
            var percentZ = position.z / (mapSize * _mapSpacing);
            percentX = Mathf.Clamp01(percentX);
            percentZ = Mathf.Clamp01(percentZ);

            var x = percentX * mapSize;
            var z = percentZ * mapSize;

            // BILINEAR INTERPOLATION

            // get corners of square
            var x1 = Mathf.FloorToInt(percentX * mapSize);
            var z1 = Mathf.FloorToInt(percentZ * mapSize);
            var x2 = Mathf.CeilToInt(percentX * mapSize);
            var z2 = Mathf.CeilToInt(percentZ * mapSize);
            var Q11 = _heightMap[z1, x1];
            var Q21 = _heightMap[z2, x1];
            var Q12 = _heightMap[z1, x2];
            var Q22 = _heightMap[z2, x2];

            // if one axis is an integer use normal linear interpolation
            if (x1 == x2) return Mathf.Lerp(Q12, Q11, z2 - z);
            if (z1 == z2) return Mathf.Lerp(Q22, Q12, z2 - z);
            // from wikipedia
            return 1f / ((x2 - x1) * (z2 - z1)) * math.mul(math.mul(new float2(x2 - x, x - x1), new float2x2(new float2(Q11, Q21), new float2(Q12, Q22))), new float2(z2 - z, z - z1));
        }

        // Only gets nearest vertex normal
        public Vector3 GetNormal(Vector3 position)
        {
            var normals = _chunks[0].GetNormals();

            var percentX = position.x / (mapSize * _mapSpacing);
            var percentZ = position.z / (mapSize * _mapSpacing);
            percentX = Mathf.Clamp01(percentX);
            percentZ = Mathf.Clamp01(percentZ);

            var normalSideCount = Mathf.FloorToInt(Mathf.Sqrt(normals.Length) - 1);
            var x = Mathf.FloorToInt(percentX * normalSideCount);
            var z = Mathf.FloorToInt(percentZ * normalSideCount);

            return normals[z * (normalSideCount + 1) + x];
        }

        private void Update()
        {
            if (takeSnapshot)
            {
                Color32[] col = new Color32[_heightMap.Length];
                var i = 0;
                foreach (var h in _heightMap)
                {
                    try
                    {
                        col[i] = new Color32(Convert.ToByte(h / heightScale * 255f), 0, 0, Convert.ToByte(255));
                        i++;
                    }
                    catch (OverflowException e)
                    {
                        Debug.LogWarning(e.Message);
                        col[i] = new Color32();
                        i++;
                    }
                }

                texture.SetPixels32(col);
                texture.Apply();

                takeSnapshot = false;
            }

            if (!paused && !isKinectPresent)
            {
                _noiseGenerator.AdvanceTime(Time.deltaTime);
            }
        }

        public static MapManager GetInstance() => _instance;
        public ref float[,] GetHeightMap() => ref _heightMap;
        public int GetMapSize() => mapSize;
        public float GetMapSpacing() => _mapSpacing;
        public int GetPathingLodFactor() => lodInfo.pathingLod == 0 ? 1 : lodInfo.pathingLod * 2;
    }
}