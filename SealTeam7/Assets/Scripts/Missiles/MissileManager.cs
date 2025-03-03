using Game;
using Map;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Missiles
{
    public class MissileManager : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private MapManager mapManager;
        [SerializeField] private GameObject missilePrefab;
        
        [Header("Settings")]
        [SerializeField] private float missileDelay = 5.0f;

        private void Start()
        {
            if (mapManager == null) mapManager = FindFirstObjectByType<MapManager>();
            
            InvokeRepeating(nameof(SpawnMissile), 0.0f, missileDelay);
        }

        private void SpawnMissile()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            float mapSize = mapManager.GetMapSize();
            float randomX = Random.Range(0f, mapSize);
            float randomZ = Random.Range(0f, mapSize);
            float mapY = mapManager.GetHeight(randomX, randomZ);
         
            Instantiate(missilePrefab, new Vector3(randomX, mapY, randomZ), Quaternion.identity);
        }
    }
}