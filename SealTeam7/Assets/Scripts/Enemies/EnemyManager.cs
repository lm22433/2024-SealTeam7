using System;
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
        [Range(0, 1)] public float spawnChance;
    }

    public class EnemyManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnGroupSpacing;
        [SerializeField] private int maxEnemyCount;
        [SerializeField] private float maxEnemyDistance;

        [Header("Game Settings")]
        [SerializeField] public PlayerCore godlyCore;
        [SerializeField] public PlayerHands godlyHands;
        
        [HideInInspector] public float sqrMaxEnemyDistance;
        
        private float _lastSpawn;
        private int _enemyCount;
        private float _spawnInterval;
        private int _spawnGroupSize;
        private EnemyData[] _enemyTypes;
        private static EnemyManager _instance;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
            SetDifficulty(GameManager.GetInstance().GetInitialDifficulty());
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            enemy.Die();
        }
        
        public static EnemyManager GetInstance() => _instance;

        public void SetDifficulty(Difficulty difficulty)
        {
            _spawnInterval = difficulty.spawnInterval;
            _spawnGroupSize = difficulty.spawnGroupSize;
            _enemyTypes = difficulty.enemies;
        }

        private void SpawnEnemies()
        {
            // choose random spawn point
            var spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            for (int i = 0; i < _spawnGroupSize; i++)
            {
                if (_enemyCount >= maxEnemyCount) continue;

                var spawnOffset = Random.onUnitSphere * spawnGroupSpacing;
                spawnOffset.y = 0f;
                
                foreach (var enemy in _enemyTypes)
                {
                    if (Random.value > enemy.spawnChance) continue;
                    
                    Instantiate(enemy.prefab, spawn.position + spawnOffset, spawn.rotation, transform);
                    break;
                }

                _enemyCount++;
            }
        }
        
        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            _lastSpawn += Time.deltaTime;
            
            if (_lastSpawn < _spawnInterval) return;
            
            _lastSpawn = 0;
            SpawnEnemies();
        }
    }
}