using System;
using Game;
using UnityEngine;

namespace Enemies.Utils
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData", order = 1)]
    public class EnemyData : ScriptableObject
    {
        [Header("Enemy Settings")]
        public GameObject prefab;
        public EnemyType enemyType;
        public float groupSpacing;

        [Header("Wave Settings")]
        public int startingWave;

        [Header("Group Spawn Size Scaling")]
        public int baseGroupSpawnSize;
        public float groupSpawnSizeGrowthRate;

        [Header("Spawn Chance Scaling")]
        public float maxSpawnChance;
        public float spawnChanceGrowthRate;

        public int GetGroupSpawnSize(Difficulty difficulty, int currentWave) =>
            Mathf.RoundToInt(baseGroupSpawnSize + groupSpawnSizeGrowthRate * Mathf.Log(currentWave - startingWave + 1, 2) * GetDifficultyMultiplier(difficulty.difficultyType));

        public float GetGroupSpawnChance(Difficulty difficulty, int currentWave) =>
            maxSpawnChance * 1.0f / (1.0f + Mathf.Exp(-spawnChanceGrowthRate * (currentWave - startingWave - 5))) * GetDifficultyMultiplier(difficulty.difficultyType);

        public static float GetDifficultyMultiplier(DifficultyType difficultyType) =>
            difficultyType switch
            {
                DifficultyType.Easy => 0.8f,
                DifficultyType.Normal => 1.0f,
                DifficultyType.Hard => 1.2f,
                DifficultyType.Impossible => 1.5f,
                _ => throw new Exception("Unknown difficulty type")
            };
    }
}