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
        
        [HideInInspector] public float sqrMaxEnemyDistance;
        
        private float _lastSpawn;
        private int _enemyCount;
        private float _spawnInterval;
        private EnemyData[] _enemyTypes;
        private static EnemyManager _instance;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            enemy.SetupDeath();
        }
        
        public static EnemyManager GetInstance() => _instance;

        public void SetDifficulty(Difficulty difficulty)
        {
            _spawnInterval = difficulty.spawnInterval;
            _enemyTypes = difficulty.enemies;
        }

        public void KillAllEnemies()
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<Enemy>(out var enemy)) Destroy(enemy.gameObject);
            }
        }

        private void SpawnEnemies()
        {
            // choose random spawn point
            var spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            
            foreach (var enemy in _enemyTypes)
            {
                if (Random.value > enemy.spawnChance) continue;

                for (int i = 0; i < enemy.groupSize; i++)
                {
                    var spawnOffset2D = Random.insideUnitCircle.normalized * enemy.groupSpacing;
                    var spawnOffset = new Vector3(spawnOffset2D.x, 4f, spawnOffset2D.y);
                    
                    if (_enemyCount >= maxEnemyCount) continue;
                    Instantiate(enemy.prefab, spawn.position + spawnOffset, spawn.rotation, transform);
                    _enemyCount++;
                }

                break;
            }
        }

        public void SpawnerSpawn(Vector3 spawnPoint, GameObject spawnee, int spawnCount)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                _enemyCount++;
                Instantiate(spawnee, spawnPoint, Quaternion.identity, transform);
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