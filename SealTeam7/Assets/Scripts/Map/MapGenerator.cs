using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

using Unity.Multiplayer;
using System.Drawing;
using FishNet.Object;

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
    
    public class MapGenerator : NetworkBehaviour {
        [SerializeField] private MapSettings settings;
        [SerializeField] private GameObject player;
        private List<ChunkGenerator> _chunks;
    
        override public void OnStartClient()
        {
            //base.OnStartServer();
            
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask != MultiplayerRoleFlags.Server) {

                return;
            }

            var chunkRow = (int) math.sqrt(settings.chunks);
            _chunks = new List<ChunkGenerator>(chunkRow);
            
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