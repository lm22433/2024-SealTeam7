using System;
using System.ComponentModel;
using Unity.VisualScripting;
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
        [Header("Base Values")] public int baseEnemyCount;
        public float baseSpawnDelay;
        public float baseHardEnemyChance;
        public float baseWaveTimeLimit;
        public float baseWaveBonusTime;

        [Header("Growth Multipliers")] public float enemyCountMultiplier;
        public float spawnDelayMultiplier;
        public float hardEnemyChanceMultiplier;
        public float waveTimeLimitMultiplier;
        public float waveBonusTimeMultiplier;

        public int GetWaveEnemyCount(int wave) => Mathf.CeilToInt(baseEnemyCount * Mathf.Pow(enemyCountMultiplier, wave - 1));
        public float GetWaveSpawnDelay(int wave) => baseSpawnDelay * Mathf.Pow(spawnDelayMultiplier, wave - 1);
        public float GetWaveHardEnemyChance(int wave) => Mathf.Clamp(baseHardEnemyChance * Mathf.Pow(hardEnemyChanceMultiplier, wave - 1), 0f, 1f);
        public float GetWaveTimeLimit(int wave) => baseWaveTimeLimit * Mathf.Pow(waveTimeLimitMultiplier, wave - 1);
        public float GetWaveBonusTime(int wave) => baseWaveBonusTime * Mathf.Pow(waveBonusTimeMultiplier, wave - 1);
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
                        baseEnemyCount = 5,
                        enemyCountMultiplier = 1.2f,
                        baseSpawnDelay = 2.0f,
                        spawnDelayMultiplier = 0.95f,
                        baseHardEnemyChance = 0.05f,
                        hardEnemyChanceMultiplier = 1.1f,
                        baseWaveTimeLimit = 40f,
                        waveTimeLimitMultiplier = 1.05f,
                        baseWaveBonusTime = 10f,
                        waveBonusTimeMultiplier = 0.95f
                    };
                case DifficultyLevel.Normal:
                    return new Difficulty
                    {
                        baseEnemyCount = 6,
                        enemyCountMultiplier = 1.5f,
                        baseSpawnDelay = 1.5f,
                        spawnDelayMultiplier = 0.9f,
                        baseHardEnemyChance = 0.1f,
                        hardEnemyChanceMultiplier = 1.12f,
                        baseWaveTimeLimit = 35f,
                        waveTimeLimitMultiplier = 1.03f,
                        baseWaveBonusTime = 7f,
                        waveBonusTimeMultiplier = 0.92f
                    };
                case DifficultyLevel.Hard:
                    return new Difficulty
                    {
                        baseEnemyCount = 8,
                        enemyCountMultiplier = 1.8f,
                        baseSpawnDelay = 1.0f,
                        spawnDelayMultiplier = 0.85f,
                        baseHardEnemyChance = 0.2f,
                        hardEnemyChanceMultiplier = 1.15f,
                        baseWaveTimeLimit = 30f,
                        waveTimeLimitMultiplier = 1.02f,
                        baseWaveBonusTime = 5f,
                        waveBonusTimeMultiplier = 0.9f
                    };
                case DifficultyLevel.Impossible:
                    return new Difficulty
                    {
                        baseEnemyCount = 12,
                        enemyCountMultiplier = 2.5f,
                        baseSpawnDelay = 0.8f,
                        spawnDelayMultiplier = 0.75f,
                        baseHardEnemyChance = 0.4f,
                        hardEnemyChanceMultiplier = 1.3f,
                        baseWaveTimeLimit = 25f,
                        waveTimeLimitMultiplier = 1.01f,
                        baseWaveBonusTime = 3f,
                        waveBonusTimeMultiplier = 0.85f
                    };
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}