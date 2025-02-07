using UnityEngine;
using System;
using System.Collections.Generic;

using Unity.Multiplayer;
using FishNet.Object;
using Player;

namespace Map
{
    [Serializable]
    public struct LODInfo
    {
        public float maxViewDistance;
        public ushort lod;
    }
    
    [Serializable]
    public struct MapSettings {
        public ushort size;
        public ushort chunkRow;
        public ushort chunkSize;
        public float lerpFactor;
        public LODInfo[] lodLevels;
        public float playerMoveThreshold;
    }
    
    public class MapGenerator : MonoBehaviour {
        [SerializeField] private MapSettings settings;
        [SerializeField] private GameObject chunkPrefab;
        private GameObject _player;
        private NoiseGenerator _noise;
        private List<Chunk> _chunks;
        private float _spacing;
        private float _sqrPlayerMoveThreshold;
        private Vector3 _playerPosition = Vector3.zero;
        private Vector3 _playerPositionOld = Vector3.zero;
        [SerializeField] private bool isKinectPresent;
    
        private void Awake() 
        {            
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server) {
                return;
            }
            
            var chunkParent = Instantiate(new GameObject("Chunks"), transform);

            _sqrPlayerMoveThreshold = settings.playerMoveThreshold * settings.playerMoveThreshold;
            _chunks = new List<Chunk>(settings.chunkRow);

            _spacing = (float) settings.size / settings.chunkRow / settings.chunkSize;
   
            ChunkSettings chunkSettings = new ChunkSettings
            {
                size = settings.chunkSize,
                spacing = _spacing,
                lerpFactor = settings.lerpFactor,
                lod = settings.lodLevels[^1].lod,
                isKinectPresent = isKinectPresent
            };

            chunkPrefab.GetComponent<Chunk>().SetSettings(chunkSettings);
            
            for (float z = 0; z < settings.size - settings.chunkRow * _spacing; z += (settings.chunkSize - 1) * _spacing) {
                for (float x = 0; x < settings.size - settings.chunkRow * _spacing; x += (settings.chunkSize - 1) * _spacing)
                {
                    var chunk = Instantiate(chunkPrefab, new Vector3(z, 0f, x), Quaternion.identity, chunkParent.transform).GetComponent<Chunk>();
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
            /*foreach (var chunk in _chunks)
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
            }*/
            
            foreach(var chunk in _chunks) {
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

                chunk.SetVisible(visible);
                if (visible) chunk.SetLod(lod);
            }
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
                var players = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
                foreach (var p in players) {
                    if (p.gameObject.GetComponentInParent<NetworkObject>().IsOwner) {
                        _player = p.gameObject;
                    }
                }
            }
        }

        public Chunk GetChunk(int x, int z) {
            return _chunks[z * settings.chunkRow + x];
        }
    }
}