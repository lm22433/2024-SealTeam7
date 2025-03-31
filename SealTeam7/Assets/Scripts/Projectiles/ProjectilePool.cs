using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    public class ProjectilePool : MonoBehaviour
    {
        private static ProjectilePool _instance;

        private readonly Dictionary<ProjectileType, Queue<GameObject>> _projectilePool = new();
        private readonly Dictionary<ProjectileType, GameObject> _prefabs = new();

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(this);
        }

        public void ClearPool()
        {
            foreach (Queue<GameObject> queue in _projectilePool.Values)
            {
                while (queue.Count > 0) Destroy(queue.Dequeue());
                queue.Clear();
            }
        }

        public void RegisterProjectile(ProjectileType projectileType, GameObject projectile)
        {
            if (!_prefabs.ContainsKey(projectileType))
            {
                _prefabs[projectileType] = projectile;
                _projectilePool[projectileType] = new Queue<GameObject>();
            }
        }

        public GameObject GetFromPool(ProjectileType projectileType, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (!_projectilePool.ContainsKey(projectileType) || _projectilePool[projectileType].Count == 0)
            {
                if (!_prefabs.TryGetValue(projectileType, out GameObject projectilePrefab))
                {
                    Debug.LogError($"No prefab registered for {projectileType}!");
                    return null;
                }

                GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
                return newProjectile;
            }

            GameObject projectile = _projectilePool[projectileType].Dequeue();
            projectile.transform.position = spawnPosition;
            projectile.transform.rotation = spawnRotation;
            projectile.SetActive(true);
            return projectile;
        }

        public void ReturnToPool(ProjectileType projectileType, GameObject projectile)
        {
            projectile.SetActive(false);
            if (!_projectilePool.ContainsKey(projectileType)) _projectilePool[projectileType] = new Queue<GameObject>();
            _projectilePool[projectileType].Enqueue(projectile);
        }

        public void ReturnToPool(ProjectileType projectileType, GameObject projectile, float time)
        {
            StartCoroutine(ReturnToPoolAfterDelay(projectileType, projectile, time));
        }

        private IEnumerator ReturnToPoolAfterDelay(ProjectileType projectileType, GameObject projectile, float time)
        {
            yield return new WaitForSeconds(time);
            ReturnToPool(projectileType, projectile);
        }

        public static ProjectilePool GetInstance() => _instance;
    }
}