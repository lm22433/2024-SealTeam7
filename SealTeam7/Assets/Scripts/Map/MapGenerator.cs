using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Serialization;

using Unity.Multiplayer;
using Unity.VisualScripting;
using FishNet.Object;

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
        public float maxHeight;
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
        [SerializeField] private bool isLocalhost;
    
        private void Awake() 
        {
            
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server) {

                return;
            }

            _sqrPlayerMoveThreshold = settings.playerMoveThreshold * settings.playerMoveThreshold;
            _chunkRow = (ushort) math.sqrt(settings.chunks);
            _chunks = new List<Chunk>(_chunkRow);

            _spacing = (float) settings.size / _chunkRow / settings.chunkSize;
            
            _noise = GetComponent<NoiseGenerator>();

            if (isLocalhost) {
                _noise.StartNoise(settings.size, settings.chunkSize);
            }
   
            ChunkSettings chunkSettings = new ChunkSettings
            {
                size = settings.chunkSize,
                spacing = _spacing,
                maxHeight = settings.maxHeight,
                lerpFactor = settings.lerpFactor,
                lod = settings.lodLevels[^1].lod,
                isLocalhost = isLocalhost
            };

            chunkPrefab.GetComponent<Chunk>().SetSettings(chunkSettings);
            
            for (float x = 0; x < settings.size; x += settings.chunkSize * _spacing) {
                for (float z = 0; z < settings.size; z += settings.chunkSize * _spacing)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(x - 0.5f * settings.size - 0.5f * settings.chunkSize * _spacing, 0f, z - 0.5f * settings.size - 0.5f * settings.chunkSize * _spacing), Quaternion.identity, transform).GetComponent<Chunk>();
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
                var sqrDistanceToPlayer = chunk.SqrDistanceToPlayer(_playerPosition);
                if (sqrDistanceToPlayer > settings.lodLevels[^1].maxViewDistance * settings.lodLevels[^1].maxViewDistance)
                {
                    chunk.SetVisible(false); continue;
                }
                
                foreach (var lod in settings.lodLevels)
                {
                    if (sqrDistanceToPlayer <= lod.maxViewDistance * lod.maxViewDistance)
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
            if(player) {
                _playerPosition = new Vector2(player.transform.position.x, player.transform.position.z);
                if ((_playerPositionOld - _playerPosition).SqrMagnitude() > _sqrPlayerMoveThreshold)
                {
                    _playerPositionOld = _playerPosition;
                    UpdateChunkLods();
                }
            } else {
                var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
                foreach (PlayerMovement p in players) {
                    if (p.gameObject.GetComponent<NetworkObject>().IsOwner) {
                        player = p.gameObject;
                    }
                }
            }
        }

        public Chunk GetChunk(int x, int z) {
            Debug.Log($"{_chunks.Count} - {x * (int) math.sqrt(settings.chunks) + z}");
            return _chunks[x * (int) math.sqrt(settings.chunks) + z];
        }
    }
}