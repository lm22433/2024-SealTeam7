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
        [SerializeField] private float spawnGroupSize;
        [SerializeField] private float spawnGroupSpacing;

        [Header("")]
        [Header("Game Settings")]
        [Header("")]
        [SerializeField] private Transform objective;
        
        private float _lastSpawn;
        
        
        public Vector3 GetObjective()
        {
            return objective.position;
        }
        
        private void Update()
        {
            _lastSpawn += Time.deltaTime;
            
            if (_lastSpawn < spawnInterval) return;
            
            _lastSpawn = 0;
            foreach (var spawn in spawnPoints)
            {
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