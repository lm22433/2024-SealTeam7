using System;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace Map
{
    public class NoiseGenerator
    {
        private readonly int _chunkRow;
        private readonly int _chunkSize;
        private readonly float _speed;
        private readonly float _noiseScale;
        private readonly float _heightScale;
        
        private NativeArray<float> _noise;
        private NativeArray<float> _heightMap;
        private JobHandle _noiseUpdate;
        private bool _completed;
        private bool _running;
        private float _time;

        public NoiseGenerator(int chunkRow, int chunkSize, int size, float speed, float noiseScale, float heightScale, ref NativeArray<float> heightMap)
        {
            _chunkRow = chunkRow;
            _chunkSize = chunkSize;
            _speed = speed;
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _time = 0f;
            _heightMap = heightMap;
            
            _noise = new NativeArray<float>((size + _chunkRow) * (size + _chunkRow), Allocator.Persistent);
            
            _running = true;
        }

        ~NoiseGenerator()
        {
            _noise.Dispose();
            _running = false;
        }
        
        public void AdvanceTime(float deltaTime)
        {
            _time += deltaTime;
            UpdateNoise();
            WriteNoise();
        }

        private void WriteNoise()
        {
            _heightMap = _noise;
        }

        private void UpdateNoise()
        {
            if (_completed)
            {
                _completed = false;
                _noiseUpdate = new NoiseUpdate
                {
                    ChunkRow = _chunkRow,
                    ChunkSize = _chunkSize,
                    Noise = _noise,
                    HeightScale = _heightScale,
                    NoiseScale = _noiseScale,
                    Speed = _speed,
                    Time = _time
                }.Schedule(_chunkRow * _chunkRow, 1, _noiseUpdate);
            }
            
            if (_noiseUpdate.IsCompleted)
            {
                _noiseUpdate.Complete();
                _completed = true;
            }
            

            /*Profiler.BeginThreadProfiling("NoiseGenerator", "UpdateNoise");
            try
            {
                while (_running)
                {
                    for (int j = 0; j < _chunkRow; j++)
                    {
                        for (int i = 0; i < _chunkRow; i++)
                        {
                            for (int y = 0; y < _chunkSize + 1; y++)
                            {
                                for (int x = 0; x < _chunkSize + 1; x++)
                                {
                                    Debug.Log($"Noise Generator: {i}, {j}, {x}, {y}");
                                    var xChunkOffset = i * _chunkSize;
                                    var yChunkOffset = j * _chunkSize;
                                    var perlinX = (xChunkOffset + x) * _noiseScale + _time * _speed;
                                    var perlinY = (yChunkOffset + y) * _noiseScale + _time * _speed;
                                    _heightMap[
                                        (j * _chunkRow + i) * (_chunkSize + 1) * (_chunkSize + 1) +
                                        y * (_chunkSize + 1) + x] = _heightScale * Mathf.PerlinNoise(perlinX, perlinY);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }*/
        }
    }

    [BurstCompile]
    public struct NoiseUpdate : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float> Noise;
        [ReadOnly] public int ChunkRow;
        [ReadOnly] public int ChunkSize;
        [ReadOnly] public float Time;
        [ReadOnly] public float Speed;
        [ReadOnly] public float NoiseScale;
        [ReadOnly] public float HeightScale;
        
        public void Execute(int index)
        {
            int i = index % ChunkRow;
            int j = index / ChunkRow;
            
            for (int y = 0; y < ChunkSize + 1; y++)
            {
                for (int x = 0; x < ChunkSize + 1; x++)
                {
                    var xChunkOffset = i * ChunkSize;
                    var yChunkOffset = j * ChunkSize;
                    var perlinX = (xChunkOffset + x) * NoiseScale + Time * Speed;
                    var perlinY = (yChunkOffset + y) * NoiseScale + Time * Speed;
                    Noise[
                        (j * ChunkRow + i) * (ChunkSize + 1) * (ChunkSize + 1) +
                        y * (ChunkSize + 1) + x] = HeightScale * Mathf.PerlinNoise(perlinX, perlinY);
                }
            }
        }
    }
}