using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Multiplayer;
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
        private GameObject _player;
        private NoiseGenerator _noise;
        private List<Chunk> _chunks;
        private float _spacing;
        private ushort _chunkRow;
        private float _sqrPlayerMoveThreshold;
        private Vector3 _playerPosition;
        private Vector3 _playerPositionOld;
        [SerializeField] private bool isKinectPresent;
    
        private void Awake() 
        {
            
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server) {
                return;
            }

            _sqrPlayerMoveThreshold = settings.playerMoveThreshold * settings.playerMoveThreshold;
            _chunkRow = (ushort) math.sqrt(settings.chunks);
            _chunks = new List<Chunk>(_chunkRow);

            _spacing = (float) settings.size / _chunkRow / settings.chunkSize;
   
            ChunkSettings chunkSettings = new ChunkSettings
            {
                size = settings.chunkSize,
                spacing = _spacing,
                maxHeight = settings.maxHeight,
                lerpFactor = settings.lerpFactor,
                lod = settings.lodLevels[^1].lod,
                isKinectPresent = isKinectPresent
            };

            chunkPrefab.GetComponent<Chunk>().SetSettings(chunkSettings);
            
            for (float z = 0; z < settings.size - ; z += (settings.chunkSize - 1) * _spacing) {
                for (float x = 0; x < settings.size; x += (settings.chunkSize - 1) * _spacing)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(x - 0.5f * settings.size - 0.5f * settings.chunkSize * _spacing, 0f, z - 0.5f * settings.size - 0.5f * settings.chunkSize * _spacing), Quaternion.identity, transform).GetComponent<Chunk>();
                    chunkSettings.x = (ushort) (x / ((settings.chunkSize - 1) * _spacing));
                    chunkSettings.z = (ushort) (z / ((settings.chunkSize - 1) * _spacing));
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
            if(_player) {
                _playerPosition = _player.transform.position;
                if (Vector3.SqrMagnitude(_playerPositionOld - _playerPosition) > _sqrPlayerMoveThreshold)
                {
                    _playerPositionOld = _playerPosition;
                    UpdateChunkLods();
                }
            } else {
                var players = FindObjectsByType<AdvancedMovement>(FindObjectsSortMode.None);
                foreach (var p in players) {
                    if (p.gameObject.GetComponentInParent<NetworkObject>().IsOwner) {
                        _player = p.gameObject;
                    }
                }
            }
        }

        public Chunk GetChunk(int x, int z) {
            // idk why this has to be the other way around
            return _chunks[x * _chunkRow + z];
        }
    }
}