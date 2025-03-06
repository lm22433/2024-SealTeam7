using Game;
using Map;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Missiles
{
    public class MissileManager : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private GameObject missilePrefab;
        [SerializeField] private GameObject targetIndicatorPrefab;
        
        [Header("Settings")]
        [SerializeField] private float missileDelay = 5.0f;
        [SerializeField] private float missileSpawnHeight = 100.0f;

        private void Start()
        {
            InvokeRepeating(nameof(SpawnMissile), 0.0f, missileDelay);
        }

        private void SpawnMissile()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            MapManager mapManager = MapManager.GetInstance(); 
            
            float mapSize = mapManager.GetMapSize();
            float randomX = Random.Range(0f, mapSize);
            float randomZ = Random.Range(0f, mapSize);
            float mapY = mapManager.GetHeight(randomX, randomZ);
            
            Vector3 targetPosition = new Vector3(randomX, mapY, randomZ);
            GameObject targetIndicator = 
                Instantiate(targetIndicatorPrefab, targetPosition, Quaternion.identity, transform);

            Vector3 missilePosition = new Vector3(randomX, mapY + missileSpawnHeight, randomZ);
            GameObject missile = Instantiate(missilePrefab, missilePosition, Quaternion.identity, transform);
            
            missile.GetComponent<Missile>().SetTarget(targetPosition, targetIndicator);
        }
    }
}