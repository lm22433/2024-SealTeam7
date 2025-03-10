using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemies
{
    [Serializable]
    public struct EnemyData
    {
        public GameObject prefab;
        public int groupSize;
        public float groupSpacing;
        [Range(0, 1)] public float spawnChance;
    }

    public class EnemyManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int maxEnemyCount;
        [SerializeField] private float maxEnemyDistance;

        [Header("Game Settings")]
        [SerializeField] public PlayerCore godlyCore;
        [SerializeField] public PlayerHands godlyHands;
        
        [Header("Enemies")]
        [SerializeField] private EnemyData[] enemyTypes;
            
        [HideInInspector] public float sqrMaxEnemyDistance;
        
        private static EnemyManager _instance;
        
        private int _enemyCount;
        private Difficulty _difficulty;
        private int _currentWave;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            GameManager.GetInstance().RegisterKill(enemy.killScore);
            enemy.Die();
        }

        public void StartSpawning() => StartCoroutine(SpawnWaves());
        
        public void SetDifficulty(Difficulty difficulty) => _difficulty = difficulty;
        
        public static EnemyManager GetInstance() => _instance;

        public void KillAllEnemies()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private IEnumerator SpawnWaves()
        {
            while (GameManager.GetInstance().GameActive)
            {
                _currentWave++;
                
                int waveEnemyGroups = _difficulty.GetWaveEnemyGroups(_currentWave);
                float spawnDelay = _difficulty.GetWaveSpawnDelay(_currentWave);
                float waveTimeLimit = _difficulty.GetWaveTimeLimit(_currentWave);

                Debug.Log($"Wave {_currentWave} - Enemy Groups: {waveEnemyGroups}, Spawn Delay: {spawnDelay:F2}s, Time Limit: {waveTimeLimit:F2}s");

                float waveStartTime = Time.time;
                
                List<EnemyData> availableEnemies = new List<EnemyData>();
                //TODO: Serialize how many waves gap per new enemy
                int unlockedEnemies = Mathf.Min(enemyTypes.Length, 1 + _currentWave / 2);
                for (int i = 0; i < unlockedEnemies; i++)
                {
                    availableEnemies.Add(enemyTypes[i]);
                }

                for (int i = 0; i < waveEnemyGroups; i++)
                {
                    yield return new WaitUntil(() => _enemyCount < maxEnemyCount);
                    
                    Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    EnemyData chosenEnemy = availableEnemies[Random.Range(0, availableEnemies.Count)];
                    
                    int scaledGroupSize = _difficulty.GetWaveGroupSize(chosenEnemy.groupSize, _currentWave);
                    int finalGroupSize = Mathf.Min(scaledGroupSize, maxEnemyCount - _enemyCount);
                    
                    for (int j = 0; j < finalGroupSize; j++)
                    {
                        Vector2 offset2D = Random.insideUnitCircle.normalized * chosenEnemy.groupSpacing;
                        Vector3 spawnOffset = new Vector3(offset2D.x, 0f, offset2D.y);
                        Instantiate(chosenEnemy.prefab, spawn.position + spawnOffset, spawn.rotation, transform);
                        _enemyCount++;
                    }
                    
                    Debug.Log($"Spawned enemy group with {finalGroupSize} enemies of type {chosenEnemy.prefab.name}.");

                    yield return new WaitForSeconds(spawnDelay);
                    if (Time.time - waveStartTime >= waveTimeLimit) break;
                }
                
                Debug.Log("Spawned whole wave. Waiting for wave to end...");
                
                while (_enemyCount > 0 && (Time.time - waveStartTime) < waveTimeLimit)
                {
                    yield return null;
                }

                if (_enemyCount == 0 && (Time.time - waveStartTime) < waveTimeLimit)
                {
                    GameManager.GetInstance().ApplyWaveClearedEarlyBonus();
                    yield return new WaitForSeconds(1f);
                }
                Debug.Log("Spawning next wave.");
            }
        }
    }
}