using System;
using Enemies.Utils;
using Game;
using Map;
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
        
        [Header("Pathing Settings")]
        [SerializeField] private float mapUpdateInterval;
        [SerializeField] private int pathingDepth;
        [SerializeField] public float pathFindInterval;

        [HideInInspector] public float sqrMaxEnemyDistance;
        
        private float _lastSpawn;
        private float _lastMapUpdate;
        private int _enemyCount;
        private float _spawnInterval;
        private PathFinder _pathFinder;
        private EnemyData[] _enemyTypes;
        private static EnemyManager _instance;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
            _pathFinder = new PathFinder(MapManager.GetInstance().GetMapSize(), MapManager.GetInstance().GetMapSpacing(), MapManager.GetInstance().GetPathingLodFactor());
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            enemy.SetupDeath();
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
                _pathFinder.UpdateGradient(ref MapManager.GetInstance().GetGradientMap());
            }
        }
        
        public static EnemyManager GetInstance() => _instance;
    }
}