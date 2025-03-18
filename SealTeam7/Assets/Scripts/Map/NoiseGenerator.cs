using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Game;
using Unity.Mathematics;

namespace Map
{
    public class NoiseGenerator
    {
        private readonly int _size;
        private readonly float _speed;
        private readonly float _noiseScale;
        private readonly float _heightScale;
        
        private float[,] _heightMap;
        private float2[,] _gradientMap;
        private Image<Gray, float> _heightImage;
        private Image<Gray, float> _gradientX;
        private Image<Gray, float> _gradientZ;
        private Image<Gray, float> _squareGradient;
        private Image<Gray, float> _gradientMagnitude;
        
        private bool _running;
        private float _time;

        public NoiseGenerator(int size, float speed, float noiseScale, float heightScale, ref float[,] heightMap, ref float2[,] gradientMap)
        {
            _size = size;
            _speed = speed;
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _time = 0f;
            
            _heightMap = heightMap;
            _gradientMap = gradientMap;
            _heightImage = new Image<Gray, float>(size + 1, size + 1);
            _gradientX = new Image<Gray, float>(size + 1, size + 1);
            _gradientZ = new Image<Gray, float>(size + 1, size + 1);
            _squareGradient = new Image<Gray, float>(size + 1, size + 1);
            _gradientMagnitude = new Image<Gray, float>(size + 1, size + 1);
            
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
                        //_heightImage.Data[y, x, 0] = 50f + 0.5f * _heightScale * Mathf.PerlinNoise(perlinX, perlinY);
                        _heightMap[y, x] = 50f + 0.5f * _heightScale * Mathf.PerlinNoise(perlinX, perlinY);
                    }
                }
                
                /*CvInvoke.Sobel(_heightImage, _gradientX, DepthType.Default, 1, 0);
                CvInvoke.Sobel(_heightImage, _gradientZ, DepthType.Default, 0, 1);
                
                for (int y = 0; y < _size + 1; y++)
                {
                    for (int x = 0; x < _size + 1; x++)
                    {
                        _heightMap[y, x] = _heightImage.Data[y, x, 0];
                        _gradientMap[y, x] = new float2(_gradientZ.Data[y, x, 0], _gradientX.Data[y, x, 0]);
                    }
                }*/
            }
        }
    }
}