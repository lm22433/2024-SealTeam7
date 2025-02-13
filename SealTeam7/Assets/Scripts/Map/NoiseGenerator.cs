using System;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Map
{
    public class NoiseGenerator
    {
        private readonly int _size;
        private readonly float _speed;
        private readonly float _noiseScale;
        private readonly float _heightScale;
        
        private byte[] _heightMap;
        private bool _running;
        private float _time;

        public NoiseGenerator(int size, float speed, float noiseScale, float heightScale, ref byte[] heightMap)
        {
            _size = size;
            _speed = speed;
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _time = 0f;
            _heightMap = heightMap;
            _running = true;
            
            Task.Run(UpdateNoise);
        }

        ~NoiseGenerator()
        {
            _running = false;
        }
        
        public void UpdateTime()
        {
            _time += Time.deltaTime;
        }

        private void UpdateNoise()
        {
            while (_running)
            {
                for (int y = 0; y < _size + 1; y++)
                {
                    for (int x = 0; x < _size + 1; x++)
                    {
                        var perlinX = x * _noiseScale + _time * _speed;
                        var perlinY = y * _noiseScale + _time * _speed;
                        _heightMap[y * _size + x] = (byte) (_heightScale * Mathf.PerlinNoise(perlinX, perlinY));
                    }
                }
            }
        }
    }
}