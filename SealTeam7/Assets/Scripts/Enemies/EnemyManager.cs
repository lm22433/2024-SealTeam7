using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private Transform objective;
        
        [HideInInspector] public float sqrMaxEnemyDistance;
        
        private float _lastSpawn;
        private int _enemyCount;


        private void Start()
        {
            sqrMaxEnemyDistance = maxEnemyDistance * maxEnemyDistance;
        }
        
        public Vector3 GetObjectivePosition()
        {
            return objective.position;
        }

        public void Kill(Enemy enemy)
        {
            _enemyCount--;
            Destroy(enemy.gameObject);
        }
        
        private void Update()
        {
            _lastSpawn += Time.deltaTime;
            
            if (_lastSpawn < spawnInterval) return;
            
            _lastSpawn = 0;
            foreach (var spawn in spawnPoints)
            {
                if (_enemyCount + spawnGroupSize > maxEnemyCount) continue;
                _enemyCount += spawnGroupSize;
                for (int i = 0; i < spawnGroupSize; i++)
                {
                    var spawnOffset = Random.onUnitSphere * spawnGroupSpacing;
                    spawnOffset.y = 0f;
                    Instantiate(enemyTypes[Random.Range(0, enemyTypes.Length)], spawn.position + spawnOffset, spawn.rotation, transform);
                }
            }
        }
    }
}