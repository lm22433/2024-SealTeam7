using System.Collections.Generic;
using UnityEngine;

namespace Enemies.Utils
{
    public class EnemyPool : MonoBehaviour
    {
        private static EnemyPool _instance;
        
        private readonly Dictionary<EnemyType, Queue<GameObject>> _enemyPool = new();
        private readonly Dictionary<EnemyType, GameObject> _prefabs = new();

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
        }

        public void ClearPool()
        {
            foreach (Queue<GameObject> queue in _enemyPool.Values)
            {
                while (queue.Count > 0) Destroy(queue.Dequeue());
                queue.Clear();
            }
        }

        public void RegisterEnemy(EnemyData enemyData)
        {
            if (!_prefabs.ContainsKey(enemyData.enemyType))
            {
                _prefabs[enemyData.enemyType] = enemyData.prefab;
                _enemyPool[enemyData.enemyType] = new Queue<GameObject>();
            }
        }

        public GameObject GetFromPool(EnemyData enemyData, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            EnemyType type = enemyData.enemyType;
            
            if (!_enemyPool.ContainsKey(type) || _enemyPool[type].Count == 0)
            {
                if (!_prefabs.TryGetValue(type, out GameObject enemyPrefab))
                {
                    Debug.LogError($"No prefab registered for {type}!");
                    return null;
                }

                GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, spawnRotation);
                return newEnemy;
            }

            GameObject enemy = _enemyPool[type].Dequeue();
            enemy.transform.position = spawnPosition;
            enemy.transform.rotation = spawnRotation;
            enemy.SetActive(true);
            return enemy;
        }

        public void ReturnToPool(EnemyType enemyType, GameObject enemy)
        {
            enemy.SetActive(false);
            if (!_enemyPool.ContainsKey(enemyType)) _enemyPool[enemyType] = new Queue<GameObject>();
            _enemyPool[enemyType].Enqueue(enemy);
        }

        public static EnemyPool GetInstance() => _instance;
    }
}