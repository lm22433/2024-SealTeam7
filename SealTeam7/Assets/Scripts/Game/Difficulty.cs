using System;
using System.ComponentModel;
using UnityEngine;

namespace Game
{
    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard,
        Impossible
    }

    [Serializable]
    public struct Difficulty
    {
        [Header("Base Values")] 
        public int baseEnemyGroupCount;
        public float baseSpawnDelay;
        public float baseWaveTimeLimit;

        [Header("Growth Multipliers")] 
        public float enemyGroupCountMultiplier;
        public float enemyGroupSizeMultiplier;
        public float spawnDelayMultiplier;
        public float waveTimeLimitMultiplier;

        public int GetWaveEnemyGroups(int wave) => Mathf.FloorToInt(baseEnemyGroupCount * Mathf.Pow(enemyGroupCountMultiplier, wave - 1));
        public float GetWaveSpawnDelay(int wave) => baseSpawnDelay * Mathf.Pow(spawnDelayMultiplier, wave - 1);
        public float GetWaveTimeLimit(int wave) => baseWaveTimeLimit * Mathf.Pow(waveTimeLimitMultiplier, wave - 1);
        
        public int GetWaveGroupSize(int baseSize, int wave) => Mathf.FloorToInt(baseSize * Mathf.Pow(enemyGroupSizeMultiplier, wave - 1));
    }

    public static class DifficultySettings
    {
        public static Difficulty GetDifficulty(DifficultyLevel difficultyLevel)
        {
            switch (difficultyLevel)
            {
                case DifficultyLevel.Easy:
                    return new Difficulty
                    {
                        baseEnemyGroupCount = 5,
                        enemyGroupCountMultiplier = 1.2f,
                        baseSpawnDelay = 2.0f,
                        spawnDelayMultiplier = 0.95f,
                        baseWaveTimeLimit = 30f,
                        waveTimeLimitMultiplier = 1.05f,
                        enemyGroupSizeMultiplier = 1.1f
                    };
                case DifficultyLevel.Normal:
                    return new Difficulty
                    {
                        baseEnemyGroupCount = 6,
                        enemyGroupCountMultiplier = 1.5f,
                        baseSpawnDelay = 1.5f,
                        spawnDelayMultiplier = 0.9f,
                        baseWaveTimeLimit = 20f,
                        waveTimeLimitMultiplier = 1.03f,
                        enemyGroupSizeMultiplier = 1.2f
                    };
                case DifficultyLevel.Hard:
                    return new Difficulty
                    {
                        baseEnemyGroupCount = 8,
                        enemyGroupCountMultiplier = 1.8f,
                        baseSpawnDelay = 1.0f,
                        spawnDelayMultiplier = 0.85f,
                        baseWaveTimeLimit = 15f,
                        waveTimeLimitMultiplier = 1.02f,
                        enemyGroupSizeMultiplier = 1.3f
                    };
                case DifficultyLevel.Impossible:
                    return new Difficulty
                    {
                        baseEnemyGroupCount = 12,
                        enemyGroupCountMultiplier = 2.5f,
                        baseSpawnDelay = 0.8f,
                        spawnDelayMultiplier = 0.75f,
                        baseWaveTimeLimit = 10f,
                        waveTimeLimitMultiplier = 1.01f,
                        enemyGroupSizeMultiplier = 1.5f
                    };
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}