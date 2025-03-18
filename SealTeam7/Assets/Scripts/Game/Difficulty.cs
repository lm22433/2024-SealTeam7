using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game
{
    public enum DifficultyType
    {
        Easy,
        Normal,
        Hard,
        Impossible
    }
    
    [CreateAssetMenu(fileName = "New Difficulty", menuName = "Game/Difficulty")]
    public class Difficulty : ScriptableObject
    {
        public string difficultyName;
        public DifficultyType difficultyType;

        [Header("Enemy Group Scaling")] 
        public int initialGroupCount;
        public float growthFactor;
        public float exponent;

        [Header("Spawn Delay Scaling")] 
        public float initialSpawnDelay;
        public float minimumSpawnDelay;
        public float decayFactor;
        
        [Header("Wave Time Limit Scaling")]
        public float initialWaveTimeLimit;
        public float minimumWaveTimeLimit;
        public float timeReductionPerWave;
        
        public int GetWaveEnemyGroupCount(int currentWave) => 
            Mathf.FloorToInt(initialGroupCount + growthFactor * Mathf.Pow(currentWave, exponent));
        
        public float GetWaveSpawnDelay(int currentWave) => 
            Mathf.Max(initialSpawnDelay * Mathf.Exp(-decayFactor * currentWave), minimumSpawnDelay);
        
        public float GetWaveTimeLimit(int currentWave) =>
            Mathf.Max(initialWaveTimeLimit - (timeReductionPerWave * currentWave), minimumWaveTimeLimit);
        
        public EnemyData GetRandomEnemy(EnemyData[] enemyData, int currentWave)
        {
            List<EnemyData> availableEnemies = enemyData.Where(e => currentWave >= e.startingWave).ToList();
            if (availableEnemies.Count == 0)
                throw new Exception("No enemies are available. You are stupid.");
            
            Dictionary<EnemyData, float> weightedChances = new Dictionary<EnemyData, float>();
            float totalWeight = 0f;

            foreach (var enemy in availableEnemies)
            {
                float spawnChance = enemy.GetGroupSpawnChance(this, currentWave);
                weightedChances[enemy] = spawnChance;
                totalWeight += spawnChance;
            }

            if (totalWeight <= 0) throw new Exception("Total weights are less than 0. Impossible!");
            
            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (var entry in weightedChances)
            {
                cumulativeWeight += entry.Value;
                if (randomValue <= cumulativeWeight)
                    return entry.Key;
            }

            throw new Exception("Could not calculate weighted chances... You done fucked up!");
        }
    }
}