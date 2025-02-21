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
        private float[,] _kernel;
        
        private bool _running;
        private float _time;

        public NoiseGenerator(int size, float speed, float noiseScale, float heightScale, float lerpFactor, ref float[] heightMap, int kernelSize, float gaussianStrength)
        {
            _size = size;
            _speed = speed;
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _lerpFactor = lerpFactor;
            _time = 0f;
            
            _heightMap = heightMap;
            _heightMapTemp = new float[(_size + 1) * (_size + 1)];
            
            _kernel = GaussianBlur(kernelSize, gaussianStrength);
            
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

        private static float[,] GaussianBlur(int length, float weight)
        {
            float[,] kernel = new float[length, length];
            float kernelSum = 0;
            int foff = (length - 1) / 2;
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
            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < length; x++)
                {
                    kernel[y, x] =  (float) (kernel[y, x] * 1d / kernelSum);
                }
            }
            return kernel;
        }

        private void Convolve()
        {
            int foff = (_kernel.GetLength(0) - 1) / 2;
            int kcenter;
            int kpixel;
            for (int y = foff; y < _size - foff; y++)
            {
                for (int x = foff; x < _size - foff; x++)
                {

                    kcenter = y * (_size + 1) + x;
                    float acc = 0f;
                    for (int fy = -foff; fy <= foff; fy++)
                    {
                        for (int fx = -foff; fx <= foff; fx++)
                        {
                            kpixel = kcenter + fy * (_size + 1) + fx;
                            acc += _heightMapTemp[kpixel] * _kernel[fy + foff, fx + foff];
                        }
                    }

                    _heightMap[kcenter] = acc;
                }
            }
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
                        _heightMapTemp[y * (_size + 1) + x] = Mathf.Lerp(_heightMap[y * (_size + 1) + x], _heightScale * Mathf.PerlinNoise(perlinX, perlinY), _lerpFactor);
                    }
                }
                
                Convolve();
            }
        }
    }
}