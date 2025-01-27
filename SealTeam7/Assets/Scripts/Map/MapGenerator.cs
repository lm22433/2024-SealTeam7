using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Map
{
    [Serializable]
    public struct MapSettings {
        public ushort size;
        public ushort chunks;
        public ushort chunkSize;
        public float heightScale;
        public float lerpFactor;
        public ushort lodSwitchDistance;
    }
    
    public class MapGenerator : MonoBehaviour {
        [SerializeField] private MapSettings settings;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private GameObject player;
        private NoiseGenerator _noise;
        private List<Chunk> _chunks;
        private float _spacing;
        private ushort _chunkRow;
        private ushort _chunkWithPlayer;
    
        private void Awake()
        {
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
                hasPlayer = false,
                lodFactor = 4
            };

            _noise.StartNoise(settings.size, settings.chunkSize);
            chunkPrefab.GetComponent<Chunk>().SetSettings(chunkSettings);
            
            for (float x = 0; x < settings.size; x += settings.chunkSize * _spacing) {
                for (float z = 0; z < settings.size; z += settings.chunkSize * _spacing)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(x, 0f, z), Quaternion.identity, transform).GetComponent<Chunk>();
                    chunkSettings.x = (ushort) (x / (settings.chunkSize * _spacing));
                    chunkSettings.z = (ushort) (z / (settings.chunkSize * _spacing));
                    chunk.SetSettings(chunkSettings);
                    _chunks.Add(chunk);
                }
            }
            
            _chunks[_chunkWithPlayer].SetPlayer(true);
            _chunks[_chunkWithPlayer].SetLod(0);
        }

        private void UpdateChunkLods(ushort newChunkWithPlayer)
        {
            /*
            foreach (var chunk in _chunks)
            {
                Vector3.SqrMagnitude(chunk.transform.position - player.transform.position);
                chunk.SetLod(0);
            }
            */
            
            _chunks[_chunkWithPlayer].SetPlayer(false);
            _chunks[_chunkWithPlayer].SetLod(2);
            _chunkWithPlayer = newChunkWithPlayer;
            _chunks[_chunkWithPlayer].SetPlayer(true);
            _chunks[_chunkWithPlayer].SetLod(0);
        }

        private void Update()
        {
            var playerPos = player.transform.position;
            var x = (ushort) Mathf.Floor(playerPos.x / (settings.chunkSize * _spacing));
            var z = (ushort) Mathf.Floor(playerPos.z / (settings.chunkSize * _spacing));
            if (x * _chunkRow + z != _chunkWithPlayer) UpdateChunkLods((ushort) (x * _chunkRow + z));
        }
    }
}