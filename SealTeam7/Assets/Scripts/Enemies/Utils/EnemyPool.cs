using System;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies.Utils
{
    [Serializable]
    public struct EnemyPoolInfo
    {
        public GameObject prefab;
        public int poolSize;
    }
    
    public class EnemyPool : MonoBehaviour
    {
        [SerializeField] private EnemyPoolInfo enemyToPool;
        private List<GameObject> _pooledEnemies;
        
        private static EnemyPool _instance;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            _pooledEnemies = new List<GameObject>();
            GameObject tmp;
            for (int i = 0; i < enemyToPool.poolSize; i++)
            {
                tmp = Instantiate(enemyToPool.prefab);
                tmp.SetActive(false);
                _pooledEnemies.Add(tmp);
            }
        }

        public GameObject GetPooledEnemy()
        {
            foreach (var gameObj in _pooledEnemies)
            {
                if (!gameObj.activeInHierarchy) return gameObj;
            }

            return null;
        }
        
        public static EnemyPool GetInstance() => _instance;
    }
}