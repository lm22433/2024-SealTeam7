using System;
using FishNet.Managing;
using UnityEngine;
using FishNet.Object;
using Player.Movement;

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
        private NetworkManager _networkManager;
        private GameObject _player;
        
        public override void OnStartServer()
        {
            _networkManager = FindFirstObjectByType<NetworkManager>();
            SpawnEnemies();
        }

        private void SpawnEnemies()
        {
            foreach (EnemyInfo e in enemies)
            {
                NetworkObject nob = _networkManager.GetPooledInstantiated(e.enemyPrefab, e.position, e.rotation, true);
                ServerManager.Spawn(nob);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        }
    }
}