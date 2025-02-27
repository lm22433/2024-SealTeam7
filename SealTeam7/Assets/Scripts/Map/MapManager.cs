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
        private float[] _heightMap;
        private List<Chunk> _chunks;
        private float _mapSpacing;
        
        private void Awake()
        {
            _mapSpacing = (float) mapSize / chunkRow / chunkSize;
            _chunks = new List<Chunk>(chunkRow);
            _heightMap = new float[(int) (mapSize / _mapSpacing + 1) * (int) (mapSize / _mapSpacing + 1)];
            
            var chunkParent = new GameObject("Chunks") { transform = { parent = transform } };

            texture = new Texture2D((int) (mapSize / _mapSpacing + 1), (int) (mapSize / _mapSpacing + 1), TextureFormat.RGBA32, false);
            
            ChunkSettings chunkSettings = new ChunkSettings
            {
                Size = chunkSize,
                MapSize = mapSize,
                Spacing = _mapSpacing,
                LODInfo = lodInfo,
                ColliderEnabled = colliderEnabled
            };
            
            if (isKinectPresent) _kinect = new KinectAPI(heightScale, lerpFactor, minimumSandDepth, maximumSandDepth, irThreshold, similarityThreshold, width, height, xOffsetStart, xOffsetEnd, yOffsetStart, yOffsetEnd, ref _heightMap, kernelSize, gaussianStrength);
            else _noiseGenerator = new NoiseGenerator((int) (mapSize / _mapSpacing), noiseSpeed, noiseScale, heightScale, ref _heightMap);

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
            if (isKinectPresent) _kinect.StopKinect();
            else _noiseGenerator.Stop();
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
            if (takeSnapshot)
            {

                Color32[] col = new Color32[_heightMap.Length];
                for(int i = 0; i < _heightMap.Length; i++)
                {
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
            
            if (!paused && !isKinectPresent)
            {
                _noiseGenerator.AdvanceTime(Time.deltaTime);                
            }
        }
        
        private void OnDrawGizmos()
        {
            if (_kinect != null)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = Color.blue;

                // Draw hand landmarks bounding boxes
                if (_kinect.BboxLeft.HasValue)
                {
                    // Debug.Log(_kinect.BboxLeft);
                    var bb = _kinect.BboxLeft.Value;
                    for (int i = 0; i < 4; i++)
                    {
                        Gizmos.DrawRay(new Vector3(bb.xMin + (i/4f)*bb.width, 0, bb.yMin), Vector3.up * 1000f);
                        Gizmos.DrawRay(new Vector3(bb.xMin, 0, bb.yMin + ((i+1)/4f)*bb.height), Vector3.up * 1000f);
                        Gizmos.DrawRay(new Vector3(bb.xMin + ((i+1)/4f)*bb.width, 0, bb.yMax), Vector3.up * 1000f);
                        Gizmos.DrawRay(new Vector3(bb.xMax, 0, bb.yMin + (i/4f)*bb.height), Vector3.up * 1000f);
                    }
                    Gizmos.DrawRay(new Vector3(bb.xMax, 0, bb.yMax), Vector3.up * 1000f);
                }
                if (_kinect.BboxRight.HasValue)
                {
                    // Debug.Log(_kinect.BboxRight);
                    var bb = _kinect.BboxRight.Value;
                    for (int i = 0; i < 4; i++)
                    {
                        Gizmos.DrawRay(new Vector3(bb.xMin + (i/4f)*bb.width, 0, bb.yMin), Vector3.up * 1000f);
                        Gizmos.DrawRay(new Vector3(bb.xMin, 0, bb.yMin + ((i+1)/4f)*bb.height), Vector3.up * 1000f);
                        Gizmos.DrawRay(new Vector3(bb.xMin + ((i+1)/4f)*bb.width, 0, bb.yMax), Vector3.up * 1000f);
                        Gizmos.DrawRay(new Vector3(bb.xMax, 0, bb.yMin + (i/4f)*bb.height), Vector3.up * 1000f);
                    }
                    Gizmos.DrawRay(new Vector3(bb.xMax, 0, bb.yMax), Vector3.up * 1000f);
                }

                // Draw hand landmarks
                /* TODO Think about which landmark(s) to use for finding y coordinate. Consider that if the region around the landmark is
                   TODO steep it will be a less reliable place to sample depth from. Also consider that landmarks
                   TODO may sometimes be outside of the map. And consider that landmarks might be occluded by other
                   TODO parts of the hand. */
                var handConnections = new[] {(0, 1), (0, 5), (9, 13), (13, 17), (5, 9), (0, 17), (1, 2), (2, 3), (3, 4),
                    (5, 6), (6, 7), (7, 8), (9, 10), (10, 11), (11, 12), (13, 14), (14, 15), (15, 16), (17, 18), 
                    (18, 19), (19, 20)};
                Debug.Log(_kinect.HandLandmarks);
                if (_kinect.HandLandmarks.Left != null)
                {
                    Gizmos.color = Color.green;
                    foreach (var connection in handConnections)
                    {
                        Gizmos.DrawLine(_kinect.HandLandmarks.Left[connection.Item1], 
                            _kinect.HandLandmarks.Left[connection.Item2]);
                    }
                    foreach (var landmark in _kinect.HandLandmarks.Left)
                    {
                        Gizmos.DrawSphere(landmark, 5f);
                    }
                }
                if (_kinect.HandLandmarks.Right != null)
                {
                    Gizmos.color = Color.red;
                    foreach (var connection in handConnections)
                    {
                        Gizmos.DrawLine(_kinect.HandLandmarks.Right[connection.Item1], 
                            _kinect.HandLandmarks.Right[connection.Item2]);
                    }
                    foreach (var landmark in _kinect.HandLandmarks.Right)
                    {
                        Gizmos.DrawSphere(landmark, 5f);
                    }
                }
            }
        }
    }
}