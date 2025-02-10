using System;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine.SceneManagement;

namespace Enemies
{
    [Serializable]
    public struct EnemyInfo
    {
        public GameObject enemyPrefab;
        public Vector3 position;
        public Quaternion rotation;
    }
    
    public class EnemyManager : NetworkBehaviour
    {
        [SerializeField] private EnemyInfo[] enemies;
        private GameObject _player;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnEnemies();
        }
        
        private void SpawnEnemies()
        {
            foreach (EnemyInfo e in enemies)
            {
                NetworkObject nob = InstanceFinder.NetworkManager.GetPooledInstantiated(e.enemyPrefab, e.position, e.rotation, true);
                ServerManager.Spawn(nob);
            }
        }
    }
}