using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

using Unity.Multiplayer;

namespace Map
{
    [Serializable]
    public struct MapSettings {
        public ushort size;
        public ushort chunks;
        public ushort chunkSize;
        public float heightScale;
        public float lerpFactor;
        public GameObject chunkPrefab;
    }
    
    public class MapGenerator : MonoBehaviour {
        [SerializeField] private MapSettings settings;
        [SerializeField] private GameObject player;
        private NoiseGenerator _noise;
        private List<ChunkGenerator> _chunks;
    
        private void Awake()
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server) {
                return;
            }

            var chunkRow = (int) math.sqrt(settings.chunks);
            _chunks = new List<ChunkGenerator>(chunkRow);
            
            _noise = GetComponent<NoiseGenerator>();
            
            var prefabScript = settings.chunkPrefab.GetComponent<ChunkGenerator>();
            var spacing = ((float) settings.size / chunkRow / settings.chunkSize);
            
            ChunkSettings chunkSettings = new ChunkSettings
            {
                size = (ushort) (settings.chunkSize + 2),
                spacing = spacing,
                heightScale = settings.heightScale,
                lerpFactor = settings.lerpFactor,
                hasPlayer = true
            };

            _noise.StartNoise(settings.size + 2, settings.chunkSize);
            prefabScript.SetSettings(chunkSettings);
            
            for (float x = 0; x < settings.size; x += settings.chunkSize * spacing) {
                for (float z = 0; z < settings.size; z += settings.chunkSize * spacing)
                {
                    var chunk = Instantiate(settings.chunkPrefab, new Vector3(x, 0f, z), Quaternion.identity, transform).GetComponent<ChunkGenerator>();
                    chunkSettings.x = (ushort) (x / (settings.chunkSize * spacing));
                    chunkSettings.z = (ushort) (z / (settings.chunkSize * spacing));
                    chunk.SetSettings(chunkSettings);
                    _chunks.Add(chunk);
                }
            }
        }
    }
}