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
        private LinkedList<Enemy> _spawningEnemies = new();
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
                // StartCoroutine(SpawnCargoPlanes());
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

        // private IEnumerator SpawnCargoPlanes()
        // {
        //     yield return new WaitForSeconds(initialStartDelay);
        //     yield return new WaitForSeconds(_difficulty.initialCargoPlaneDelay);
        //     
        //     while (GameManager.GetInstance().IsGameActive())
        //     {
        //         yield return new WaitUntil(() => _enemyCount < maxEnemyCount);
        //             
        //         Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        //
        //         var cargo = enemyData[0];
        //         
        //         SpawnEnemies(cargo, spawn.position, spawn.rotation);
        //         
        //         if (!cargo.tooltipShown)
        //         {
        //             GameManager.GetInstance().DisplayTooltip(cargo.tooltipText, enemyTooltipDuration);
        //             cargo.tooltipShown = true;
        //         }
        //         
        //         yield return new WaitForSeconds(_difficulty.cargoPlaneSpawnDelay);
        //     }
        // }

        private IEnumerator SpawnWaves()
        {
            if (_difficulty.difficultyType == DifficultyType.Tutorial)
            {
                Toast("Welcome to the tutorial! Bury the soldiers to protect your base. Watch out – your hands take damage too!", 10f);
                yield return Wait(5f);
                
                _currentWave = 1;
                yield return SpawnGrid(EnemyType.Soldier, spawnPoint: 5, rows: 4, columns: 6);
                yield return EndWave();
                
                Toast("Vehicles need to be buried multiple times. Try getting them close together and killing them all in two swift motions!", 10f);
                yield return SpawnAtInterval(EnemyType.Tank, spawnPoint: 4, count: 10, interval: 3f);
                yield return EndWave();
                
                Toast("These enemies burrow under the sand and must be dug out, not buried. Look out for their dust trails!", 10f);
                for (var i = 3; i <= 7; i++)
                {
                    Spawn(EnemyType.Burrower, spawnPoint: i);
                    yield return new WaitForSeconds(1f);
                }
                yield return EndWave();
                
                Toast("That's it for the tutorial! Remember to read the popups as new enemies arrive. Good luck – now shift some sand!", 6f);
                yield return Wait(6f);
                
                // Return to main menu
                MapManager.GetInstance().Quit();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            else
            {
                yield return new WaitForSeconds(5f);
                _currentWave = 1;
                
                // Wave 1
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 5, 7 }, 4, 6);
                yield return ReleaseSpawningEnemies();
                yield return Wait(10f);
                
                // Wave 2
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 4, 6, 7 }, 4, 6);
                yield return ReleaseSpawningEnemies();
                yield return Wait(10f);

                yield return SpawnGrids(EnemyType.LmgSoldier, new[] { 4, 6 }, 4, 5);
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 5, 7 }, 4, 6);
                yield return ReleaseSpawningEnemies();
                yield return EndWave();
                
                // Wave 3
                yield return SpawnGrids(EnemyType.RpgSoldier, new[] { 4, 6 }, 6, 3);
                yield return SpawnGrids(EnemyType.SniperSoldier, new[] { 2, 8 }, 2, 2);
                yield return ReleaseSpawningEnemies();
                yield return Wait(10f);
                
                yield return SpawnGrids(EnemyType.LmgSoldier, new[] { 3, 4, 5, 6, 7 }, 4, 5);
                yield return ReleaseSpawningEnemies();
                yield return Wait(10f);
                
                yield return SpawnAtInterval(EnemyType.Tank, new[] { 3, 5, 7 }, 5, 3f);
                yield return EndWave();
                
                // Wave 4
                yield return SpawnAtInterval(EnemyType.Burrower, new[] { 3, 7 }, 2, 3f);
                yield return Wait(10f);
                
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 4, 5, 6, 7 }, 4, 6);
                yield return SpawnGrids(EnemyType.SniperSoldier, new[] { 2, 8 }, 2, 2);
                yield return ReleaseSpawningEnemies();
                yield return EndWave();
                
                // Wave 5
                Toast("A HUGE wave of enemies is approaching...", duration: 10f);
                yield return Wait(4f);
                
                yield return SpawnAtInterval(EnemyType.Helicopter, new[] { 4, 6 }, 3, 3f);
                yield return SpawnAtInterval(EnemyType.Spawner, new[] { 3, 4, 5, 6, 7 }, 4, 3f);
                yield return SpawnGrids(EnemyType.LmgSoldier, new[] { 3, 5, 7 }, 5, 8);
                yield return SpawnGrids(EnemyType.RpgSoldier, new[] { 4, 6 }, 8, 4);
                yield return SpawnGrids(EnemyType.SniperSoldier, new[] { 2, 8 }, 3, 3);
                yield return ReleaseSpawningEnemies();
                yield return Wait(10f);

                for (var i = 0; i < 6; i++)
                {
                    yield return SpawnGrids(EnemyType.FastSoldier, new[] { 3, 4, 5, 6, 7 }, 1, 10);
                    yield return ReleaseSpawningEnemies();
                    yield return Wait(1f);
                }

                yield return SpawnGrids(EnemyType.SniperSoldier, new[] { 2, 8 }, 3, 3);
                yield return ReleaseSpawningEnemies();
                yield return SpawnAtInterval(EnemyType.Tank, new[] { 3, 4, 5, 6, 7 }, 4, 3f);
                yield return SpawnAtInterval(EnemyType.Burrower, new[] { 3, 5, 6, 7 }, 2, 3f);
                yield return EndWave();
                
                // Wave 6
                yield return SpawnGrids(EnemyType.LmgSoldier, new[] { 4, 5, 6 }, 5, 8);
                yield return SpawnAtInterval(EnemyType.Necromancer, 8, 5);
                yield return SpawnAtInterval(EnemyType.Helicopter, new[] { 4, 6 }, 3);
                yield return Wait(10f);
                
                yield return SpawnGrids(EnemyType.LmgSoldier, new[] { 4, 5, 6 }, 5, 8);
                yield return ReleaseSpawningEnemies();
                yield return SpawnAtInterval(EnemyType.Tank, new[] { 3, 4, 5, 6, 7 }, 3);
                yield return EndWave();
                
                // Wave 7
                yield return SpawnAtInterval(EnemyType.Burrower, new[] { 2 }, 4);
                yield return SpawnGrids(EnemyType.RpgSoldier, new[] { 4, 6 }, 8, 4);
                yield return ReleaseSpawningEnemies();
                yield return SpawnAtInterval(EnemyType.KamikazePlane, new[] { 3, 5, 7 }, 3);
                yield return Wait(10f);
                
                yield return SpawnGrids(EnemyType.Soldier, new[] { 3, 5, 7 }, 4, 6);
                yield return SpawnGrids(EnemyType.RpgSoldier, new[] { 4, 6 }, 8, 4);
                yield return SpawnGrids(EnemyType.SniperSoldier, new[] { 2, 8 }, 2, 2);
                yield return SpawnGrids(EnemyType.FastSoldier, new[] { 1, 9 }, 1, 10);
                yield return EndWave();
                
                // Wave 8
                yield return SpawnGrids(EnemyType.LmgSoldier, new[] { 3, 4 }, 5, 8);
                yield return SpawnGrid(EnemyType.RpgSoldier, 5, 8, 4);
                yield return SpawnGrids(EnemyType.SniperSoldier, new[] { 6, 7 }, 2, 2);
                yield return ReleaseSpawningEnemies();
                yield return SpawnAtInterval(EnemyType.AerialSpawner, new[] { 2, 8 }, 2);
                for (var i = 0; i < 6; i++)
                {
                    yield return SpawnGrid(EnemyType.FastSoldier, 1, 1, 10);
                    yield return ReleaseSpawningEnemies();
                    yield return SpawnGrid(EnemyType.FastSoldier, 9, 1, 10);
                    yield return ReleaseSpawningEnemies();
                }

                // Wave -
                Toast("Get a load of this guy!", duration: 5f);
                Spawn(EnemyType.Mech, 0);
                yield return EndWave();
   
                // Wave 10
                yield return SpawnAtInterval(EnemyType.Mech, new[] { 4, 5, 6 }, 2, 3f);
            }
        }
        
        private void Spawn(EnemyType enemy, int spawnPoint) =>
            Spawn(enemy, new[] { spawnPoint });
        
        private void Spawn(EnemyType enemy, int[] spawnPoints)
        {
            var data = enemyData.FirstOrDefault(e => e.enemyType == enemy);
            
            foreach (var spawnPoint in spawnPoints)
            {
                SpawnEnemies(data, this.spawnPoints[spawnPoint].position, this.spawnPoints[spawnPoint].rotation);
            }
        }

        private IEnumerator SpawnGrid(EnemyType enemy, int spawnPoint, float rows, float columns) => 
            SpawnGrids(enemy, new []{ spawnPoint }, rows, columns);
        
        private IEnumerator SpawnGrids(EnemyType enemy, int[] spawnPoints, float rows, float columns)
        {
            var numRows = (int) (rows*Mathf.Sqrt(_difficulty.groupSizeMultiplier));
            var numColumns = (int) (columns*Mathf.Sqrt(_difficulty.groupSizeMultiplier));
            var spacing = 8f;  // TODO: set spacing depending on enemy type?
            var data = enemyData.FirstOrDefault(e => e.enemyType == enemy);
            var timePerGrid = 0.1f;
            var timePerEnemy = timePerGrid/(numRows*numColumns);
            var enemiesToSpawnPerFrame = (int)(Time.smoothDeltaTime/timePerEnemy);

            var i = 0;
            foreach (var spawnPoint in spawnPoints)
            {
                var spawnPosition = this.spawnPoints[spawnPoint].position;
                var spawnRotation = this.spawnPoints[spawnPoint].rotation.normalized;
                var zVector = spawnRotation * Vector3.forward;
                var xVector = spawnRotation * Vector3.right;
                var gridHeight = (numRows - 1) * spacing;
                var gridWidth = (numColumns - 1) * spacing;
                var startPos = spawnPosition - gridHeight / 2 * zVector - gridWidth / 2 * -xVector;

                for (var z = 0; z < numRows; z++)
                {
                    for (var x = 0; x < numColumns; x++)
                    {
                        var pos = startPos + zVector * (z * spacing) - xVector * (x * spacing);
                        pos.y = MapManager.GetInstance().GetHeight(pos);
                        SpawnEnemies(data, pos, spawnRotation);

                        // yield return null waits until the next frame
                        if (i % enemiesToSpawnPerFrame == enemiesToSpawnPerFrame - 1) yield return null;
                        i++;
                    }
                }
            }
        }

        private IEnumerator SpawnAtInterval(EnemyType enemy, int spawnPoint, int count, float interval = 3f) =>
            SpawnAtInterval(enemy, new[] { spawnPoint }, count, interval);
        
        private IEnumerator SpawnAtInterval(EnemyType enemy, int[] spawnPoints, int count, float interval = 3f)
        {
            var data = enemyData.FirstOrDefault(e => e.enemyType == enemy);
            var scaledCount = (int) (count*_difficulty.groupSizeMultiplier);
            
            for (var i = 0; i < scaledCount; i++)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    SpawnEnemies(data, this.spawnPoints[spawnPoint].position, this.spawnPoints[spawnPoint].rotation);
                }

                yield return ReleaseSpawningEnemies(immediate: true);
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator ReleaseSpawningEnemies(bool immediate = false)
        {
            if (!immediate) yield return Wait(0.4f);
            
            foreach (var spawningEnemy in _spawningEnemies)
            {
                spawningEnemy.SetState(EnemyState.Moving);
            }
            
            _spawningEnemies.Clear();
        }

        public void SpawnEnemies(EnemyData enemy, Vector3 spawnPosition, Quaternion spawnRotation, int enemyCount = 1)
        {
            if (!enemy.tooltipShown)
            {
                Toast(enemy.tooltipText, enemyTooltipDuration);
                enemy.tooltipShown = true;
            }
            
            for (int i = 0; i < enemyCount; i++)
            {
                GameObject e = EnemyPool.GetInstance().GetFromPool(enemy, spawnPosition, spawnRotation);
                
                e.TryGetComponent(out Enemy enemyComp);
                enemyComp.Init();
                _spawningEnemies.AddLast(enemyComp);

                e.TryGetComponent(out BasePhysics basePhysics);
                basePhysics.Init();
                e.transform.SetParent(transform);
                _enemyCount++;
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

        private IEnumerator EndWave()
        {
            yield return new WaitUntil(() => _enemyCount < 5);
            yield return new WaitForSeconds(5f);
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
        
        public int GetWave() => _currentWave;
        public int GetEnemiesKilled() => _enemiesKilled;
        public Dictionary<EnemyType, int> GetEnemiesKilledDetailed() => _enemiesKilledDetailed;
    }
}