using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using System;
using Emgu.CV;
using Emgu.CV.Structure;
using Game;

namespace Map
{
    public class NoiseGenerator
    {
        private readonly int _size;
        private readonly float _speed;
        private readonly float _noiseScale;
        private readonly float _heightScale;
        
        private float[,] _heightMap;
        private Image<Gray, float> _tmpImage1;
        
        private bool _running;
        private float _time;

        public NoiseGenerator(int size, float speed, float noiseScale, float heightScale, ref float[,] heightMap)
        {
            _size = size;
            _speed = speed;
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _time = 0f;
            
            _heightMap = heightMap;
            _tmpImage1 = new Image<Gray, float>(size + 1, size + 1);
            
            _running = true;
            
            Task.Run(UpdateNoise);
        }

        public void Stop()
        {
            _running = false;
        }
        
        public void AdvanceTime(float deltaTime)
        {
            _time += deltaTime;
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
                        _heightMap[y, x] = 50f + 0.5f * _heightScale * Mathf.PerlinNoise(perlinX, perlinY);
                        _tmpImage1.Data[y, x, 0] = 50f + 0.5f * _heightScale * Mathf.PerlinNoise(perlinX, perlinY);
                        _heightMap = _tmpImage1[0].Mat
                    }
                }
            }
        }
    }
}