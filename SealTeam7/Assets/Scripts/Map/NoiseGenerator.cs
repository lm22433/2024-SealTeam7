using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace Map
{
    public class NoiseGenerator
    {
        private readonly int _size;
        private readonly float _speed;
        private readonly float _noiseScale;
        private readonly float _heightScale;
        private readonly float _lerpFactor;
        
        private float[] _heightMap;
        private bool _running;
        private float _time;

        public NoiseGenerator(int size, float speed, float noiseScale, float heightScale, float lerpFactor, ref float[] heightMap)
        {
            _size = size;
            _speed = speed;
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _lerpFactor = lerpFactor;
            _time = 0f;
            _heightMap = heightMap;
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
            Profiler.BeginThreadProfiling("NoiseGenerator", "UpdateNoise");
            while (_running)
            {
                for (int y = 0; y < _size + 1; y++)
                {
                    for (int x = 0; x < _size + 1; x++)
                    {
                        var perlinX = x * _noiseScale + _time * _speed;
                        var perlinY = y * _noiseScale + _time * _speed;
                        _heightMap[y * (_size + 1) + x] = Mathf.Lerp(_heightMap[y * (_size + 1) + x], _heightScale * Mathf.PerlinNoise(perlinX, perlinY), _lerpFactor);
                    }
                }
            }
        }
    }
}