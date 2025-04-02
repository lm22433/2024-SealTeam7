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
        private int _enemiesKilled;
        private readonly Dictionary<EnemyType, int> _enemiesKilledDetailed = new();

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
            _enemiesKilled++;
            if (!_enemiesKilledDetailed.TryAdd(enemy.enemyType, 1))
                _enemiesKilledDetailed[enemy.enemyType]++;
            GetDataFromDeadEnemy(enemy);
            GameManager.GetInstance().RegisterKill(enemy.killScore);
            EnemyPool.GetInstance().ReturnToPool(enemy.enemyType, enemy.gameObject);
        }

        public void StartSpawning()
        {
            if (!GameManager.GetInstance().IsSandboxMode())
            {
                StartCoroutine(SpawnWaves());
                StartCoroutine(SpawnCargoPlanes());
            }
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
            
            while (GameManager.GetInstance().IsGameActive())
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
            yield return new WaitForSeconds(initialStartDelay);
            
            while (GameManager.GetInstance().IsGameActive())
            {
                _currentWave++;
                
                int waveEnemyGroups = _difficulty.GetWaveEnemyGroupCount(_currentWave);
                float spawnDelay = _difficulty.GetWaveSpawnDelay(_currentWave);
                float waveTimeLimit = _difficulty.GetWaveTimeLimit(_currentWave);

                float waveStartTime = Time.time;
                
                Debug.Log($"Wave {_currentWave} - Enemy Groups: {waveEnemyGroups}, Spawn Delay: {spawnDelay:F2}s, Time Limit: {waveTimeLimit:F2}s");

                for (int i = 0; i < waveEnemyGroups; i++)
                {
                    yield return new WaitUntil(() => _enemyCount < maxEnemyCount);
                    
                    Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    
                    EnemyData chosenEnemy = _difficulty.GetRandomEnemy(enemyData, _currentWave);
                    if (!chosenEnemy) continue;
                    
                    if (!chosenEnemy.tooltipShown)
                    {
                        GameManager.GetInstance().DisplayTooltip(chosenEnemy.tooltipText, enemyTooltipDuration);
                        chosenEnemy.tooltipShown = true;
                    }
                    
                    int finalGroupSize = Mathf.Min(chosenEnemy.GetGroupSpawnSize(_difficulty, _currentWave), maxEnemyCount - _enemyCount);
                    
                    for (int j = 0; j < finalGroupSize; j++)
                    {
                        Vector2 spawnOffset2D = Random.insideUnitCircle.normalized * chosenEnemy.groupSpacing;
                        Vector3 spawnOffset = new Vector3(spawnOffset2D.x, 4f, spawnOffset2D.y);
                        SpawnEnemies(chosenEnemy, spawn.position + spawnOffset, spawn.rotation);
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
        
        public int GetWave() => _currentWave;
        public int GetEnemiesKilled() => _enemiesKilled;
        public Dictionary<EnemyType, int> GetEnemiesKilledDetailed() => _enemiesKilledDetailed;
    }
}