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
using UnityEngine.SceneManagement;
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
            // StartCoroutine(SpawnCargoPlanes());
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
                _currentWave = 0;
                IncrementWaveNumber();
                yield return Wait(3f);
                
                Toast("Welcome to the tutorial! Bury the soldiers to protect your base. Watch out – your hands take damage too!", 10f);
                yield return Wait(2f);
                yield return SpawnGrid(EnemyType.Soldier, spawnPoint: 5, rows: 4, columns: 6);
                yield return Wait(20f);
                
                Toast("Vehicles need to be buried multiple times. Try getting them close together and killing them all in two swift motions!", 10f);
                yield return SpawnAtInterval(EnemyType.Tank, spawnPoint: 4, count: 10, interval: 3f);
                yield return Wait(20f);
                
                Toast("These enemies burrow under the sand and must be dug out, not buried. Look out for their dust trails!", 10f);
                for (var i = 3; i <= 7; i++)
                {
                    Spawn(EnemyType.Burrower, spawnPoint: i);
                    yield return new WaitForSeconds(1f);
                }
                yield return Wait(11f);
                
                Toast("That's it for the tutorial! Remember to read the popups as new enemies arrive. Good luck – now shift some sand!", 8f);
                yield return Wait(8f);
                
                // Return to main menu
                MapManager.GetInstance().Quit();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            else
            {
                yield return new WaitForSeconds(5f);
                
                // Wave 1
                yield return SpawnGrids(EnemyType.Soldier, spawnPoints: new[] { 3, 5, 7 }, 4, 6);
                yield return Wait(10f);

                yield return SpawnGrids(EnemyType.SniperSoldier, new[] { 2, 8 }, 3, 3);
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 5, 7 }, 4, 6);
                yield return Wait(30f);
                IncrementWaveNumber();
                
                // Wave 2
                Spawn(EnemyType.FastSoldier, spawnPoint: 5);
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 4, 6, 7 }, 4, 6);
                yield return Wait(10f);
                
                Spawn(EnemyType.FastSoldier, spawnPoint: 3);
                Spawn(EnemyType.FastSoldier, spawnPoint: 4);
                Spawn(EnemyType.FastSoldier, spawnPoint: 6);
                Spawn(EnemyType.FastSoldier, spawnPoint: 7);
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 4, 6, 7 }, 4, 6);
                yield return Wait(10f);
                
                Spawn(EnemyType.FastSoldier, spawnPoint: 3);
                Spawn(EnemyType.FastSoldier, spawnPoint: 4);
                Spawn(EnemyType.FastSoldier, spawnPoint: 5);
                Spawn(EnemyType.FastSoldier, spawnPoint: 6);
                Spawn(EnemyType.FastSoldier, spawnPoint: 7);
                yield return SpawnGrids(EnemyType.RpgSoldier, new[] { 4, 6 }, 4, 5);
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 5, 7 }, 4, 6);
                yield return Wait(30f);
                IncrementWaveNumber();
                
                // Wave 3
                yield return SpawnGrids(EnemyType.SniperSoldier, new[] { 2, 8 }, 3, 3);
            }
        }
        
        private void Spawn(EnemyType enemy, int spawnPoint)
        {
            var data = enemyData.FirstOrDefault(e => e.enemyType == enemy);
            SpawnEnemies(data, spawnPoints[spawnPoint].position, spawnPoints[spawnPoint].rotation);
        }

        private IEnumerator SpawnGrid(EnemyType enemy, int spawnPoint, float rows, float columns) => 
            SpawnGrids(enemy, new []{ spawnPoint }, rows, columns);
        
        private IEnumerator SpawnGrids(EnemyType enemy, int[] spawnPoints, float rows, float columns)
        {
            var numRows = (int) rows;  //TODO: scale by difficulty multiplier
            var numColumns = (int) columns;
            var spacing = 8f;  // TODO: set spacing depending on enemy type?
            var timeInterval = 0.01f;  // TODO: set depending on num rows and columns?
            var enemyComps = new Enemy[spawnPoints.Length*numRows*numColumns];

            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var spawnPoint = spawnPoints[i];
                var spawnPosition = this.spawnPoints[spawnPoint].position;
                var spawnRotation = this.spawnPoints[spawnPoint].rotation.normalized;
                var zVector = spawnRotation * Vector3.forward;
                var xVector = spawnRotation * Vector3.right;
                var gridHeight = (numRows - 1) * spacing;
                var gridWidth = (numColumns - 1) * spacing;
                var startPos = spawnPosition - gridHeight / 2 * zVector - gridWidth / 2 * xVector;

                for (var z = 0; z < numRows; z++)
                {
                    for (var x = 0; x < numColumns; x++)
                    {
                        var pos = startPos + zVector * (z * spacing) + xVector * (x * spacing);
                        var enemyComp = SpawnAndGetEnemy(enemyData.FirstOrDefault(e => e.enemyType == enemy), pos,
                            spawnRotation);
                        enemyComp.DisallowMovement = true;
                        enemyComp.Invulnerable = true;
                        enemyComps[i*numRows*numColumns + z*numColumns + x] = enemyComp;
                        yield return new WaitForSeconds(timeInterval);
                    }
                }
            }

            yield return Wait(0.5f);

            for (var i = 0; i < spawnPoints.Length*numRows*numColumns; i++)
            {
                enemyComps[i].DisallowMovement = false;
                enemyComps[i].Invulnerable = false;
            }
        }

        private IEnumerator SpawnAtInterval(EnemyType enemy, int spawnPoint, int count, float interval = 1f)
        {
            for (var i = 0; i < count; i++)
            {
                var data = enemyData.FirstOrDefault(e => e.enemyType == enemy);
                SpawnEnemies(data, spawnPoints[spawnPoint].position, spawnPoints[spawnPoint].rotation);
                yield return new WaitForSeconds(interval);
            }
        }
        
        private Enemy SpawnAndGetEnemy(EnemyData enemy, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            GameObject e = EnemyPool.GetInstance().GetFromPool(enemy, spawnPosition, spawnRotation);
            var enemyComp = e.GetComponent<Enemy>();
            enemyComp.Init();
            e.GetComponent<BasePhysics>().Init();
            e.transform.SetParent(transform);
            _enemyCount++;
            return enemyComp;
        }

        public void SpawnEnemies(EnemyData enemy, Vector3 spawnPosition, Quaternion spawnRotation, int enemyCount = 1)
        {
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnAndGetEnemy(enemy, spawnPosition, spawnRotation);
            }
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);  //TODO: scale by difficulty multiplier
        }
        
        private void Toast(string message, float duration)
        {
            GameManager.GetInstance().DisplayTooltip(message, duration);
        }

        private void IncrementWaveNumber()
        {
            //TODO: update UI
            _currentWave++;
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