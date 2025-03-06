using System.Collections.Generic;
using UnityEngine;

namespace Enemies.Utils
{
    public class EnemyPool : MonoBehaviour
    {
        public static EnemyPool SharedInstance;
        public List<GameObject> pooledObjects;
        public GameObject objectToPool;
        public int amountToPool;

        private void Awake()
        {
            SharedInstance = this;
        }

        private void Start()
        {
            pooledObjects = new List<GameObject>();
            GameObject tmp;
            for(int i = 0; i < amountToPool; i++)
            {
                tmp = Instantiate(objectToPool);
                tmp.SetActive(false);
                pooledObjects.Add(tmp);
            }
        }
    }
}