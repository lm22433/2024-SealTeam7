using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public ushort chunkRow;
        public ushort chunkSize;
        public float maxHeight;
        public float lerpFactor;
        public LOD[] lodLevels;
        public float playerMoveThreshold;
    }
    
    public class MapGenerator : MonoBehaviour {
        [SerializeField] private MapSettings settings;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private GameObject _player;
        private NoiseGenerator _noise;
        private List<Chunk> _chunks;
        private float _spacing;
        private float _sqrPlayerMoveThreshold;
        private Vector3 _playerPosition;
        private Vector3 _playerPositionOld;
        [SerializeField] private bool isKinectPresent;
    
        private void Awake() 
        {
            
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server) {
                return;
            }
            
            //settings.chunkSize = (ushort) (settings.size / settings.chunkRow);

            _sqrPlayerMoveThreshold = settings.playerMoveThreshold * settings.playerMoveThreshold;
            _chunks = new List<Chunk>(settings.chunkRow);

            _spacing = (float) settings.size / settings.chunkRow / settings.chunkSize;
   
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
            
            for (float z = 0; z < settings.size - settings.chunkRow * _spacing; z += (settings.chunkSize - 1) * _spacing) {
                for (float x = 0; x < settings.size - settings.chunkRow * _spacing; x += (settings.chunkSize - 1) * _spacing)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(x, 0f, z), Quaternion.identity, transform).GetComponent<Chunk>();
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
            Parallel.ForEach(_chunks, chunk =>
            {
                bool visible = false;
                ushort lod = settings.lodLevels[^1].lod;
                
                var sqrDistanceToPlayer = chunk.SqrDistanceToPlayer(_playerPosition);

                foreach (var lodInfo in settings.lodLevels)
                {
                    if (sqrDistanceToPlayer <= lodInfo.maxViewDistance * lodInfo.maxViewDistance)
                    {
                        lod = lodInfo.lod;
                        visible = true;
                        break;
                    }
                }

                chunk.SetLod(lod);
                chunk.SetVisible(visible);
            });
        }

        private void Update()
        {
            if (_player) {
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
            return _chunks[x * settings.chunkRow + z];
        }
    }
}