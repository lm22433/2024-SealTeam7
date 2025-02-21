using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using System;

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
        private float[] _heightMapTemp;
        private bool _running;
        private float _time;

        private int _kernelSize;
        private float _guassianStrength;
        private float[,] _kernel;

        public NoiseGenerator(int size, float spacing, float speed, float noiseScale, float heightScale, ref float[] heightMap, float lerpFactor, int kernelSize, float guassianStrength)
        {
            _size = size;
            _speed = speed;
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _lerpFactor = lerpFactor;
            _time = 0f;
            _heightMap = heightMap;
            _heightMapTemp = new float[(_size + 1) * (_size + 1)];
            _running = true;
            _kernelSize = kernelSize;
            _guassianStrength = guassianStrength;

            _kernel = GaussianBlur(_kernelSize, _guassianStrength);
            
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

        public static float[,] GaussianBlur(int lenght, float weight)
        {
            float[,] kernel = new float[lenght, lenght];
            float kernelSum = 0;
            int foff = (lenght - 1) / 2;
            float distance;
            double constant = 1d / (2 * Math.PI * weight * weight);
            for (int y = -foff; y <= foff; y++)
            {
                for (int x = -foff; x <= foff; x++)
                {
                    distance = ((y * y) + (x * x)) / (2 * weight * weight);
                    kernel[y + foff, x + foff] = (float) (constant * Math.Exp(-distance));
                    kernelSum += kernel[y + foff, x + foff];
                }
            }
            for (int y = 0; y < lenght; y++)
            {
                for (int x = 0; x < lenght; x++)
                {
                    kernel[y, x] =  (float) (kernel[y, x] * 1d / kernelSum);
                }
            }
            return kernel;
        }

        public void Convolve()
        {
            int foff = (_kernel.GetLength(0) - 1) / 2;
            int kcenter;
            int kpixel;
            for (int y = foff; y < _size - foff; y++)
            {
                for (int x = foff; x < _size - foff; x++)
                {
                    kcenter = y * _size + x;
                    float acc = 0f;
                    for (int fy = -foff; fy <= foff; fy++)
                    {
                        for (int fx = -foff; fx <= foff; fx++)
                        {
                            kpixel = kcenter + fy * _size + fx;
                            acc += _heightMapTemp[kpixel] * _kernel[fy + foff, fx + foff];
                        }
                    }

                    _heightMap[kcenter] = acc;
                }
            }
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
                        //_heightMap[y * (_size + 1) + x] = Mathf.Lerp(_heightMap[y * (_size + 1) + x], _heightScale * Mathf.PerlinNoise(perlinX, perlinY), _lerpFactor);
                        _heightMapTemp[(int) (y * (_size + 1) + x)] = _heightScale * Mathf.PerlinNoise(perlinX, perlinY);
                    }
                }
                //Convolve();
            }
        }
    }
}