using UnityEngine;

namespace Map
{
    public class NoiseGenerator : MonoBehaviour
    {
        [SerializeField] private float speed = 1f;
        [SerializeField] private float noiseScale = 100f;
        private float[] _noise;
        private int _size;
        private int _chunkSize;
        private bool _running;
        private float _time;
        
        public void StartNoise(int size, int chunkSize)
        {
            _time = 0;
            _noise = new float[size * size];
            _size = size;
            _chunkSize = chunkSize;
            _running = true;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_running)
            {
                UpdateNoise();
            }
        }

        private void UpdateNoise()
        {
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    var perlinX = x * noiseScale + _time * speed;
                    var perlinY = y * noiseScale + _time * speed;
                    _noise[y * _size + x] = Mathf.PerlinNoise(perlinX, perlinY);
                }
            }
        }
        
        public void GetChunkNoise(ref float[] noise, int chunkX, int chunkZ)
        {
            int zChunkOffset = chunkZ * _chunkSize;
            int xChunkOffset = chunkX * _chunkSize;
            
            for (int z = 0; z < _chunkSize + 2; z++)
            {
                for (int x = 0; x < _chunkSize + 2; x++)
                {
                    noise[z * (_chunkSize + 2) + x] = _noise[(z + zChunkOffset) * _size + xChunkOffset + x];
                }
            }
        }
    }
}