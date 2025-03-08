using System;
using System.Linq;
using Enemies.Utils;
using Game;
using Map;
using Player;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Enemies
{
    [Serializable]
    public struct EnemyData
    {
        public GameObject prefab;
        public int groupSize;
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
        
        [Header("Pathing Settings")]
        [SerializeField] private float mapUpdateInterval;
        [SerializeField] private int pathingDepth;

        [HideInInspector] public float sqrMaxEnemyDistance;
        
        private float _lastSpawn;
        private float _lastMapUpdate;
        private int _enemyCount;
        private float _spawnInterval;
        private PathFinder _pathFinder;
        private int _mapSize;
        private float _mapSpacing;
        private EnemyData[] _enemyTypes;
        private static EnemyManager _instance;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
            _mapSize = MapManager.GetInstance().GetMapSize();
            _mapSpacing = MapManager.GetInstance().GetMapSpacing();
            _pathFinder = new PathFinder(_mapSize, _mapSpacing);
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            enemy.Die();
        }

        public void SetDifficulty(Difficulty difficulty)
        {
            _spawnInterval = difficulty.spawnInterval;
            _enemyTypes = difficulty.enemies;
        }

        public Vector3[] FindPath(Vector3 start, Vector3 end)
        {
            return _pathFinder.FindPath(start, end, pathingDepth);
        }

        private void SpawnEnemies()
        {
            // choose random spawn point
            var spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            var spawnOffset = Random.onUnitSphere * spawnGroupSpacing;
            spawnOffset.y = 0f;
            
            foreach (var enemy in _enemyTypes)
            {
                if (Random.value > enemy.spawnChance) continue;

                for (int i = 0; i < enemy.groupSize; i++)
                {
                    if (_enemyCount >= maxEnemyCount) continue;
                    Instantiate(enemy.prefab, spawn.position + spawnOffset, spawn.rotation, transform);
                    _enemyCount++;
                }

                break;
            }
        }
        
        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            _lastSpawn += Time.deltaTime;
            if (_lastSpawn > _spawnInterval)
            {
                _lastSpawn = 0;
                SpawnEnemies();
            }
            
            _lastMapUpdate += Time.deltaTime;
            if (_lastMapUpdate > mapUpdateInterval)
            {
                _lastMapUpdate = 0;
                _pathFinder.UpdateMap(ref MapManager.GetInstance().GetHeightMap());
            }
        }
        
        public static EnemyManager GetInstance() => _instance;
    }
}