using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public int spawnWave;
        [Range(0, 1)] public float spawnChance;
    }

    public class EnemyManager : MonoBehaviour
    {
        [Header("Spawn Settings")] 
        [SerializeField] private float initialStartDelay = 10f;
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
            enemy.SetupDeath();
        }

        public void StartSpawning() => StartCoroutine(SpawnWaves());
        
        public void SetDifficulty(Difficulty difficulty) => _difficulty = difficulty;
        
        public static EnemyManager GetInstance() => _instance;

        public void KillAllEnemies()
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<Enemy>(out var enemy)) Destroy(enemy.gameObject);
            }
        }

        private IEnumerator SpawnWaves()
        {
            yield return new WaitForSeconds(initialStartDelay);
            
            while (GameManager.GetInstance().GameActive)
            {
                _currentWave++;
                
                int waveEnemyGroups = _difficulty.GetWaveEnemyGroups(_currentWave);
                float spawnDelay = _difficulty.GetWaveSpawnDelay(_currentWave);
                float waveTimeLimit = _difficulty.GetWaveTimeLimit(_currentWave);

                float waveStartTime = Time.time;

                List<EnemyData> availableEnemies = enemyTypes.Where(data => data.spawnWave <= _currentWave).ToList();
                
                Debug.Log($"Wave {_currentWave} - Enemy Groups: {waveEnemyGroups}, Spawn Delay: {spawnDelay:F2}s, Time Limit: {waveTimeLimit:F2}s, Available Enemies: {string.Join(", ", availableEnemies.Select(data => data.prefab.name))}");

                for (int i = 0; i < waveEnemyGroups; i++)
                {
                    yield return new WaitUntil(() => _enemyCount < maxEnemyCount);
                    
                    Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    EnemyData chosenEnemy = availableEnemies[Random.Range(0, availableEnemies.Count)];
                    
                    // We add 1 so that we do NOT get 0.
                    int scaledGroupSize = _difficulty.GetWaveGroupSize(chosenEnemy.groupSize, _currentWave - chosenEnemy.spawnWave + 1);
                    int finalGroupSize = Mathf.Min(scaledGroupSize, maxEnemyCount - _enemyCount);
                    
                    for (int j = 0; j < finalGroupSize; j++)
                    {
                        Vector2 spawnOffset2D = Random.insideUnitCircle.normalized * chosenEnemy.groupSpacing;
                        Vector3 spawnOffset = new Vector3(spawnOffset2D.x, 4f, spawnOffset2D.y);
                        Instantiate(chosenEnemy.prefab, spawn.position + spawnOffset, spawn.rotation, transform);
                        _enemyCount++;
                    }
                    
                    yield return new WaitForSeconds(spawnDelay);
                    if (Time.time - waveStartTime >= waveTimeLimit) break;
                }
                
                while (_enemyCount > 0 && (Time.time - waveStartTime) < waveTimeLimit)
                {
                    yield return null;
                }

                if (_enemyCount == 0 && (Time.time - waveStartTime) < waveTimeLimit)
                {
                    GameManager.GetInstance().ApplyWaveClearedEarlyBonus();
                    yield return new WaitForSeconds(1f);
                }
            }
        }
    }
}