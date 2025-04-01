using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enemies.FunkyPhysics;
using Game;
using Map;
using Player;
using Projectiles;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemies.Utils
{   
    public struct PathRequest
    {
        public Vector3 Start;
        public Vector3 End;
        public Func<Node, Node, float> Heuristic;
        public Action<Vector3[]> Callback;

        public PathRequest(Vector3 start, Vector3 end, Func<Node, Node, float> heuristic, Action<Vector3[]> callback)
        {
            Start = start;
            End = end;
            Heuristic = heuristic;
            Callback = callback;
        }
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
        [SerializeField] public PlayerHands[] godlyHands;
        [SerializeField] private float enemyTooltipDuration = 5f;
        
        [Header("Pathing Settings")]
        [SerializeField] private float mapUpdateInterval;
        [SerializeField] private int pathingDepth;
        [SerializeField] public float pathFindInterval;

        [Header("Enemies - Cargo Plane First")]
        [SerializeField] private EnemyData[] enemyData;
        
        [HideInInspector] public float sqrMaxEnemyDistance;
        [HideInInspector] public EnemyData lastDeadEnemy;
        
        private float _lastSpawn;
        private float _lastMapUpdate;
        private int _enemyCount;
        private float _spawnInterval;
        private PathFinder _pathFinder;
        private ConcurrentQueue<PathRequest> _pathRequestQueue;
        private bool _running;
        private static EnemyManager _instance;
        private Difficulty _difficulty;
        private int _currentWave;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
            _pathFinder = new PathFinder(MapManager.GetInstance().GetMapSize(), MapManager.GetInstance().GetMapSpacing(), MapManager.GetInstance().GetPathingLodFactor());
            _pathRequestQueue = new ConcurrentQueue<PathRequest>();
            _running = true;

            Task.Run(PathThread);
        }

        private void OnApplicationQuit() => _running = false;

        private void Start()
        {
            foreach (EnemyData data in enemyData)
            {
                EnemyPool.GetInstance().RegisterEnemy(data);
                Enemy enemy = data.prefab.GetComponent<Enemy>();
                ProjectilePool.GetInstance().RegisterProjectile(enemy.projectileType, enemy.projectile);
                if (data.enemyType is EnemyType.Soldier) lastDeadEnemy = data;
                data.tooltipShown = false;
            }
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            GetDataFromDeadEnemy(enemy);
            GameManager.GetInstance().RegisterKill(enemy.killScore);
            EnemyPool.GetInstance().ReturnToPool(enemy.enemyType, enemy.gameObject);
        }

        public void StartSpawning()
        {
            StartCoroutine(SpawnWaves());
            StartCoroutine(SpawnCargoPlanes());
        }

        public void SetDifficulty(Difficulty difficulty) => _difficulty = difficulty;

        private void PathThread()
        {
            while (_running)
            {
                TryProcessPath();
            }
        }

        public void RequestPath(Vector3 start, Vector3 end, Func<Node, Node, float> heuristic, Action<Vector3[]> callback)
        {
            _pathRequestQueue.Enqueue(new PathRequest(start, end, heuristic, callback));
        }

        private void TryProcessPath()
        {
            if (_pathRequestQueue.Count < 1) return;
            if (!_pathRequestQueue.TryDequeue(out var request)) return;
            _pathFinder.FindPathAsync(request.Start, request.End, pathingDepth, request.Heuristic, request.Callback);
        }

        private IEnumerator SpawnCargoPlanes()
        {
            yield return new WaitForSeconds(initialStartDelay);
            yield return new WaitForSeconds(_difficulty.initialCargoPlaneDelay);
            
            while (GameManager.GetInstance().GameActive)
            {
                yield return new WaitUntil(() => _enemyCount < maxEnemyCount);
                    
                Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

                var cargo = enemyData[0];
                
                SpawnEnemies(cargo, spawn.position, spawn.rotation);
                
                if (!cargo.tooltipShown)
                {
                    GameManager.GetInstance().DisplayTooltip(cargo.tooltipText, enemyTooltipDuration);
                    cargo.tooltipShown = true;
                }
                
                yield return new WaitForSeconds(_difficulty.cargoPlaneSpawnDelay);
            }
        }

        private IEnumerator SpawnWaves()
        {
            if (_difficulty.difficultyType == DifficultyType.Tutorial)
            {
                GameManager.GetInstance().DisplayTooltip("Welcome to the tutorial! Bury the soldiers to protect " +
                                                         "your base. Watch out – your hands take damage too!", 10f);
                SpawnEnemies(enemyData.FirstOrDefault(e => e.enemyType == EnemyType.Soldier), 
                    spawnPoints[5].position, spawnPoints[5].rotation, 6);
            }

            yield return null;
        }

        public void SpawnEnemies(EnemyData enemy, Vector3 spawnPosition, Quaternion spawnRotation, int enemyCount = 1)
        {
            for (int i = 0; i < enemyCount; i++)
            {
                GameObject e = EnemyPool.GetInstance().GetFromPool(enemy, spawnPosition, spawnRotation);
                if (!e) continue;
                e.GetComponent<Enemy>().Init();
                e.GetComponent<BasePhysics>().Init();
                e.transform.SetParent(transform);
                _enemyCount++;
            }
        }
        
        private void GetDataFromDeadEnemy(Enemy enemy)
        {
            if (enemy.enemyType is EnemyType.Necromancer) return;
            lastDeadEnemy = enemyData.FirstOrDefault(e => e.enemyType == enemy.enemyType);
        }
        
        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
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