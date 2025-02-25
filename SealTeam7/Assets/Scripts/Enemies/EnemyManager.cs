using Game;
using Player;
using UnityEngine;

namespace Enemies
{
    public class EnemyManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Header("")]
        [SerializeField] private GameObject[] enemyTypes;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval;
        [SerializeField] private int spawnGroupSize;
        [SerializeField] private float spawnGroupSpacing;
        [SerializeField] private int maxEnemyCount;
        [SerializeField] private float maxEnemyDistance;
        
        [Header("")]
        [Header("Game Settings")]
        [Header("")]
        [SerializeField] public PlayerCore godlyCore;
        [SerializeField] public PlayerHands godlyHands;
        
        [HideInInspector] public float sqrMaxEnemyDistance;
        
        private float _lastSpawn;
        private int _enemyCount;
        private static EnemyManager _instance;

        private void Start()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            enemy.Die();
        }
        
        public static EnemyManager GetInstance() => _instance;

        private void SpawnEnemies()
        {
            foreach (var spawn in spawnPoints)
            {
                for (int i = 0; i < spawnGroupSize; i++)
                {
                    if (_enemyCount >= maxEnemyCount) continue;
                    
                    var spawnOffset = Random.onUnitSphere * spawnGroupSpacing;
                    spawnOffset.y = 0f;
                    Instantiate(enemyTypes[Random.Range(0, enemyTypes.Length)], spawn.position + spawnOffset, spawn.rotation, transform);

                    _enemyCount++;
                }
            }
        }
        
        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            _lastSpawn += Time.deltaTime;
            
            if (_lastSpawn < spawnInterval) return;
            
            _lastSpawn = 0;
            SpawnEnemies();
        }
    }
}