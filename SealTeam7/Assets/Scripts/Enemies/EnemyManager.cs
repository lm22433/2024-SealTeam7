using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enemies.Utils;
using Game;
using Map;
using Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemies
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
        [SerializeField] public PlayerHands godlyHands;
        
        [Header("Pathing Settings")]
        [SerializeField] private float mapUpdateInterval;
        [SerializeField] private int pathingDepth;
        [SerializeField] public float pathFindInterval;

        [Header("Enemies")]
        [SerializeField] private EnemyData[] enemyData;
            
        [HideInInspector] public float sqrMaxEnemyDistance;
        
        private float _lastSpawn;
        private float _lastMapUpdate;
        private int _enemyCount;
        private float _spawnInterval;
        private PathFinder _pathFinder;
        private Queue<PathRequest> _pathRequestQueue;
        private bool _running;
        private EnemyData[] _enemyTypes;
        private static EnemyManager _instance;
        private Difficulty _difficulty;
        private int _currentWave;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
            _pathFinder = new PathFinder(MapManager.GetInstance().GetMapSize(), MapManager.GetInstance().GetMapSpacing(), MapManager.GetInstance().GetPathingLodFactor());
            _pathRequestQueue = new Queue<PathRequest>();
            _running = true;

            Task.Run(PathThread);
        }

        private void OnApplicationQuit() => _running = false;

        private void Start()
        {
            foreach (EnemyData data in enemyData)
            {
                Debug.Log(data.enemyType);
                EnemyPool.GetInstance().RegisterEnemy(data);
            }
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            GameManager.GetInstance().RegisterKill(enemy.killScore);
            enemy.SetupDeath();
            EnemyPool.GetInstance().ReturnToPool(enemy.enemyType, enemy.gameObject);
        }

        public void StartSpawning() => StartCoroutine(SpawnWaves());
        
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
            var request = _pathRequestQueue.Dequeue();
            _pathFinder.FindPathAsync(request.Start, request.End, pathingDepth, request.Heuristic, request.Callback);
        }

        private IEnumerator SpawnWaves()
        {
            yield return new WaitForSeconds(initialStartDelay);
            
            while (GameManager.GetInstance().GameActive)
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
                    if (chosenEnemy == null) continue;
                    int finalGroupSize = Mathf.Min(chosenEnemy.GetGroupSpawnSize(_difficulty, _currentWave), maxEnemyCount - _enemyCount);
                    
                    for (int j = 0; j < finalGroupSize; j++)
                    {
                        Vector2 spawnOffset2D = Random.insideUnitCircle.normalized * chosenEnemy.groupSpacing;
                        Vector3 spawnOffset = new Vector3(spawnOffset2D.x, 4f, spawnOffset2D.y);
                        GameObject enemy = EnemyPool.GetInstance().GetFromPool(chosenEnemy, spawn.position + spawnOffset, spawn.rotation);
                        if (enemy != null)
                        {
                            enemy.GetComponent<Enemy>().Init();
                            enemy.transform.SetParent(transform);
                            _enemyCount++;
                        }
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
        
        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
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