using System;
using System.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using Unity.Mathematics;
using UnityEngine;

namespace Map
{
    public class NoiseGenerator : NetworkBehaviour
    {
        [SerializeField] private int size;
        [SerializeField] private float speed = 1f;
        [SerializeField] private float noiseScale = 100f;
        private half[] _noise;
        private bool _running;
        private float _time;
        [SerializeField] private MapGenerator mapGenerator;

        public override void OnStartServer()
        {
            StartNoise();
            ServerManager.OnRemoteConnectionState += OnClientConnected;
        }

        private void OnClientConnected(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                GetComponent<NetworkObject>().GiveOwnership(conn);
            }
        }
        
        public void StartNoise()
        {
            _time = 0;
            _noise = new half[size * size];
            _running = true;
            Task.Run(UpdateNoise);
        }

        private void OnApplicationQuit()
        {
            _running = false;
        }

        private void Update()
        {
            _time += Time.deltaTime;
        }

        private void UpdateNoise()
        {
            while (_running)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var perlinX = x * noiseScale + _time * speed;
                        var perlinY = y * noiseScale + _time * speed;
                        _noise[y * size + x] = (half) Mathf.PerlinNoise(perlinX, perlinY);
                    }
                }
            }
        }
        
        public void RequestNoise(ushort lod, ushort chunkSize, int x, int z) {
            RequestChunkNoiseServerRpc(Owner, lod, chunkSize, x, z); 
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestChunkNoiseServerRpc(NetworkConnection targetConnection, ushort lod, ushort chunkSize, int x, int z)
        {
            half[] depths = GetChunkNoise(lod, chunkSize, x, z);

            // Send the depth data back to the requesting client
            //NetworkConnection targetConnection = NetworkManager.ServerManager.Clients[clientId];

            //if (targetConnection != null)
            //{
                SendChunkNoiseTargetRpc(targetConnection, depths, x, z);
            //}
        }

        [TargetRpc]
        private void SendChunkNoiseTargetRpc(NetworkConnection conn, half[] depths, int x, int z)
        {
            mapGenerator.GetChunk(x, z).SetHeights(depths);
        }
        
        public half[] GetChunkNoise(ushort lod, ushort chunkSize, int chunkX, int chunkZ)
        {
            var lodFactor = lod == 0 ? 1 : lod * 2;
            var resolution = chunkSize / lodFactor;
            int zChunkOffset = chunkZ * (chunkSize - 1);
            int xChunkOffset = chunkX * (chunkSize - 1);
            
            var noise = new half[resolution * resolution];
            
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    noise[z * resolution + x] = _noise[(lodFactor * z + zChunkOffset) * size + xChunkOffset + lodFactor * x];
                }
            }

            return noise;
        }
    }
}