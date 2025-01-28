using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Map
{
    [Serializable]
    public struct LOD
    {
        public float maxViewDistance;
        public ushort lod;
    }
    
    [Serializable]
    public struct MapSettings {
        public ushort size;
        public ushort chunks;
        public ushort chunkSize;
        public float heightScale;
        public float lerpFactor;
        public LOD[] lodLevels;
        public float playerMoveThreshold;
    }
    
    public class MapGenerator : MonoBehaviour {
        [SerializeField] private MapSettings settings;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private GameObject player;
        private NoiseGenerator _noise;
        private List<Chunk> _chunks;
        private float _spacing;
        private ushort _chunkRow;
        private float _sqrPlayerMoveThreshold;
        private Vector2 _playerPosition;
        private Vector2 _playerPositionOld;
    
        private void Awake()
        {
            _sqrPlayerMoveThreshold = settings.playerMoveThreshold * settings.playerMoveThreshold;
            _chunkRow = (ushort) math.sqrt(settings.chunks);
            _chunks = new List<Chunk>(_chunkRow);
            
            _noise = GetComponent<NoiseGenerator>();
            _spacing = ((float) settings.size / _chunkRow / settings.chunkSize);
            
            ChunkSettings chunkSettings = new ChunkSettings
            {
                size = settings.chunkSize,
                spacing = _spacing,
                heightScale = settings.heightScale,
                lerpFactor = settings.lerpFactor,
                lod = settings.lodLevels[^1].lod
            };

            _noise.StartNoise(settings.size, settings.chunkSize);
            chunkPrefab.GetComponent<Chunk>().SetSettings(chunkSettings);
            
            for (float x = 0; x < settings.size; x += settings.chunkSize * _spacing) {
                for (float z = 0; z < settings.size; z += settings.chunkSize * _spacing)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(x - 0.5f * settings.size, 0f, z - 0.5f * settings.size), Quaternion.identity, transform).GetComponent<Chunk>();
                    chunkSettings.x = (ushort) (x / (settings.chunkSize * _spacing));
                    chunkSettings.z = (ushort) (z / (settings.chunkSize * _spacing));
                    chunk.SetSettings(chunkSettings);
                    _chunks.Add(chunk);
                }
            }
            
            UpdateChunkLods();
        }

        private void UpdateChunkLods()
        {
            foreach (var chunk in _chunks)
            {
                var chunkPosition = new Vector2(chunk.transform.position.x, chunk.transform.position.z);
                var playerOffset = (chunkPosition - _playerPosition).SqrMagnitude();

                if (playerOffset > settings.lodLevels[^1].maxViewDistance) chunk.SetVisible(false);
                
                foreach (var lod in settings.lodLevels)
                {
                    if (playerOffset <= lod.maxViewDistance * lod.maxViewDistance)
                    {
                        chunk.SetVisible(true);
                        chunk.SetLod(lod.lod);
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            _playerPosition = player.transform.position;
            if ((_playerPositionOld - _playerPosition).SqrMagnitude() > _sqrPlayerMoveThreshold)
            {
                _playerPositionOld = _playerPosition;
                UpdateChunkLods();
            }
        }
    }
}