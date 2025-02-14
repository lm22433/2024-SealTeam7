using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Map
{
    [Serializable]
    public struct LODInfo
    {
        public int maxChunkDistance;
        public int lod;
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
        [Header("Map Settings")]
        [Header("")]
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private int size;
        [SerializeField] private int chunkRow;
        [SerializeField] private int chunkSize;
        [SerializeField] private float heightScale;
        [SerializeField] private float lerpFactor;
        [SerializeField] private LODInfo[] lodInfos;
        
        [Header("")]
        [Header("Environment Settings")]
        [Header("")]
        [SerializeField] private bool isKinectPresent;
        
        private NoiseGenerator _noise;
        private KinectAPI _kinect;
        private NativeArray<float> _heightMap;
        private List<Chunk> _chunks;
        private float _spacing;
        
        private void Awake()
        {
            texture = new Texture2D(size + 1, size + 1, TextureFormat.RGBA32, false);
            
            _spacing = (float) size / chunkRow / chunkSize;
            _chunks = new List<Chunk>(chunkRow);
            _heightMap = new NativeArray<float>((size + 1) * (size + 1), Allocator.Persistent);
            
            var chunkParent = new GameObject("Chunks") { transform = { parent = transform } };

            ChunkSettings chunkSettings = new ChunkSettings
            {
                Size = chunkSize,
                MapSize = size,
                Spacing = _spacing,
                LerpFactor = lerpFactor,
                LOD = lodInfos[^1].lod
            };
            
            if (isKinectPresent) _kinect = new KinectAPI(heightScale, minimumSandDepth, maximumSandDepth, irThreshold, similarityThreshold, width, height, xOffsetStart, xOffsetEnd, yOffsetStart, yOffsetEnd, ref _heightMap);
            else _noise = new NoiseGenerator(size, noiseSpeed, noiseScale, heightScale, ref _heightMap);
            
            for (float z = 0; z < size; z += chunkSize * _spacing) {
                for (float x = 0; x < size; x += chunkSize * _spacing)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(x, 0f, z), Quaternion.identity, chunkParent.transform).GetComponent<Chunk>();
                    chunkSettings.X = (int) (x / (chunkSize * _spacing));
                    chunkSettings.Z = (int) (z / (chunkSize * _spacing));
                    chunk.Setup(chunkSettings, ref _heightMap);
                    _chunks.Add(chunk);
                }
            }
        }

        ~MapManager()
        {
            _heightMap.Dispose();
        }
        
        [SerializeField] private bool takeSnapshot;
        [SerializeField] private Texture2D texture;
        private void Update() {
            if (takeSnapshot) {

                Color32[] col = new Color32[_heightMap.Length];
                for(int i = 0; i < _heightMap.Length; i++) {
                    try
                    {
                        col[i] = new Color32(Convert.ToByte(Mathf.Min(255, _heightMap[i] / heightScale * 255)), 0, 0, Convert.ToByte(255));   
                    }
                    catch (OverflowException e)
                    {
                        Debug.LogWarning(e.Message);
                        col[i] = new Color32();
                    }
                }

                texture.SetPixels32(col);
                texture.Apply();
                Debug.Log(_heightMap.Length);
                Debug.Log("Applied Texture");

                takeSnapshot = false;
            }
            
            _noise.AdvanceTime(Time.deltaTime);
        }
    }
}