using System.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using Kinect;
using Unity.Mathematics;
using UnityEngine;

namespace Map
{
    public class NoiseGenerator : NetworkBehaviour
    {
        [SerializeField] private int size;
        [SerializeField] private float speed;
        [SerializeField] private float noiseScale;
        [SerializeField] private float maxHeight;
        private half[] _noise;
        private bool _running;
        private float _time;
        [SerializeField] private MapGenerator mapGenerator;
        private KinectAPI _kinect;

        private void Start()
        {
            _kinect = FindFirstObjectByType<KinectAPI>();

            if (!IsServerInitialized || _kinect.isKinectPresent) return;

            StartNoise();
        }

        private void StartNoise()
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
            if (!_kinect.isKinectPresent)
            {
                _time += Time.deltaTime;
            }
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
                        _noise[y * size + x] = (half) (Mathf.PerlinNoise(perlinX, perlinY) * maxHeight);
                    }
                }
            }
        }
        
        public void RequestNoise(ushort lod, ushort chunkSize, int x, int z) {
            RequestChunkNoiseServerRpc(ClientManager.Connection, lod, chunkSize, x, z); 
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestChunkNoiseServerRpc(NetworkConnection conn, ushort lod, ushort chunkSize, int x, int z)
        {
            half[] depths = GetChunkNoise(lod, chunkSize, x, z);
            
            SendChunkNoiseTargetRpc(conn, depths, x, z, lod);
        }

        [TargetRpc]
        private void SendChunkNoiseTargetRpc(NetworkConnection conn, half[] depths, int x, int z, ushort lod)
        {
            mapGenerator.GetChunk(x, z).SetHeights(depths, lod);
        }
        
        private half[] GetChunkNoise(ushort lod, ushort chunkSize, int chunkX, int chunkZ)
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

        public half GetHeight(int xPos, int zPos)
        {
            return _noise[zPos * size + xPos];
        }
    }
}