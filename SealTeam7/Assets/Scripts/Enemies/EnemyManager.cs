using System;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;

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
            base.OnStartServer();
            
            _networkManager = FindFirstObjectByType<NetworkManager>();
            SpawnEnemies();
            
            ServerManager.OnRemoteConnectionState += OnClientConnected;
        }

        private void OnClientConnected(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                GetComponent<NetworkObject>().GiveOwnership(conn);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        }
        
        [Server]
        private void SpawnEnemies()
        {
            foreach (EnemyInfo e in enemies)
            {
                NetworkObject nob = _networkManager.GetPooledInstantiated(e.enemyPrefab, e.position, e.rotation, true);
                _networkManager.ServerManager.Spawn(nob);
            }
        }
    }
}