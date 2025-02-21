using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Map
{
    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public int colliderLod;
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

        [Header("Blur Settings")]
        [SerializeField] private int _kernelSize;
        [SerializeField] private float _guassianStrength;

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
        [Header("Environment Settings")]
        [Header("")]
        [SerializeField] private bool isKinectPresent;
        [Header("")]
        [Header("Debug")]
        [Header("")]
        [SerializeField] private bool paused;
        [SerializeField] private bool colliderEnabled;
        
        private NoiseGenerator _noiseGenerator;
        private KinectAPI _kinect;
        private float[] _heightMap;
        private List<Chunk> _chunks;
        private float _mapSpacing;
        
        private void Awake()
        {
            _mapSpacing = (float) mapSize / chunkRow / chunkSize;
            _chunks = new List<Chunk>(chunkRow);
            _heightMap = new float[(int) (mapSize / _mapSpacing + 1) * (int) (mapSize / _mapSpacing + 1)];
            
            var chunkParent = new GameObject("Chunks") { transform = { parent = transform } };

            ChunkSettings chunkSettings = new ChunkSettings
            {
                Size = chunkSize,
                MapSize = mapSize,
                Spacing = _mapSpacing,
                LODInfo = lodInfo,
                ColliderEnabled = colliderEnabled
            };
            
            if (isKinectPresent) _kinect = new KinectAPI(heightScale, lerpFactor, minimumSandDepth, maximumSandDepth, irThreshold, similarityThreshold, width, height, xOffsetStart, xOffsetEnd, yOffsetStart, yOffsetEnd, ref _heightMap, _kernelSize, _guassianStrength);
            else _noiseGenerator = new NoiseGenerator((int) (mapSize / _mapSpacing), noiseSpeed, noiseScale, heightScale, lerpFactor, ref _heightMap, lerpFactor, _kernelSize, _guassianStrength);

            for (int z = 0; z < chunkRow; z++)
            {
                for (int x = 0; x < chunkRow; x++)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(x * chunkSize * _mapSpacing, 0f, z * chunkSize * _mapSpacing), Quaternion.identity, chunkParent.transform).GetComponent<Chunk>();
                    chunkSettings.X = x;
                    chunkSettings.Z = z;
                    chunk.Setup(chunkSettings, ref _heightMap);
                    _chunks.Add(chunk);
                }
            }
        }

        private void OnApplicationQuit()
        {
            if(isKinectPresent) {
                _kinect.StopKinect();
            } else {
                _noiseGenerator.Stop();
            }
            
        }

        public float GetHeight(float worldX, float worldZ)
        {
            // just to be safe
            worldX = Mathf.Clamp(worldX, 0, mapSize);
            worldZ = Mathf.Clamp(worldZ, 0, mapSize);
            
            //TODO: [fix] interpolate between heights at each step to get true height
            var lower = _heightMap[Mathf.FloorToInt(worldZ / _mapSpacing) * (int) (mapSize / _mapSpacing + 1) + Mathf.FloorToInt(worldX / _mapSpacing)];
            var upper = _heightMap[Mathf.CeilToInt(worldZ / _mapSpacing) * (int) (mapSize / _mapSpacing + 1) + Mathf.CeilToInt(worldX / _mapSpacing)];

            var lerp = Mathf.Lerp(lower, upper, worldZ / _mapSpacing - Mathf.FloorToInt(worldZ / _mapSpacing));
            
            return lerp;
        }

        private void Update()
        {
            if (!paused)
            {
                //_noiseGenerator.AdvanceTime(Time.deltaTime);                
            }
        }
    }
}