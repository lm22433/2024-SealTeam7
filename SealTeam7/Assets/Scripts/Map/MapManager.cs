using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

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

        [Header("")]
        [Header("Map Settings")]
        [Header("")]
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private int size;
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
        [SerializeField] private bool takeSnapshot;
        [SerializeField] private Texture2D texture;
        
        private NoiseGenerator _noiseGenerator;
        private KinectAPI _kinect;
        private float[] _heightMap;
        private List<Chunk> _chunks;
        private float _spacing;
        
        private void Awake()
        {
            _spacing = (float) size / chunkRow / chunkSize;
            _chunks = new List<Chunk>(chunkRow);
            _heightMap = new float[(int) (size / _spacing + 1) * (int) (size / _spacing + 1)];
            
            texture = new Texture2D((int) (size / _spacing) + 1, (int) (size / _spacing) + 1, TextureFormat.RGBA32, false);
            
            var chunkParent = new GameObject("Chunks") { transform = { parent = transform } };

            ChunkSettings chunkSettings = new ChunkSettings
            {
                Size = chunkSize,
                MapSize = size,
                Spacing = _spacing,
                LerpFactor = lerpFactor,
                LOD = lodInfo.lod,
                ColliderLOD = lodInfo.colliderLod
            };
            
            if (isKinectPresent) _kinect = new KinectAPI(heightScale, minimumSandDepth, maximumSandDepth, irThreshold, similarityThreshold, width, height, xOffsetStart, xOffsetEnd, yOffsetStart, yOffsetEnd, ref _heightMap);
            else _noiseGenerator = new NoiseGenerator((int) (size / _spacing), _spacing, noiseSpeed, noiseScale, heightScale, ref _heightMap);

            for (int z = 0; z < chunkRow; z++)
            {
                for (int x = 0; x < chunkRow; x++)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(x * chunkSize * _spacing, 0f, z * chunkSize * _spacing), Quaternion.identity, chunkParent.transform).GetComponent<Chunk>();
                    chunkSettings.X = x;
                    chunkSettings.Z = z;
                    chunk.Setup(chunkSettings, ref _heightMap);
                    _chunks.Add(chunk);
                }
            }
        }

        private void OnApplicationQuit()
        {
            _noiseGenerator.Stop();
        }

        private void Update()
        {
            if (takeSnapshot)
            {
                Color32[] col = new Color32[_heightMap.Length];
                for(int i = 0; i < _heightMap.Length; i++)
                {
                    try
                    {
                        col[i] = new Color32(Convert.ToByte(_heightMap[i] / heightScale * 255), 0, 0, Convert.ToByte(255));   
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        col[i] = new Color32();
                    }
                }

                texture.SetPixels32(col);
                texture.Apply();

                takeSnapshot = false;
            }
            
            if (!paused)
            {
                _noiseGenerator.AdvanceTime(Time.deltaTime);                
            }
        }
    }
}