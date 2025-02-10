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
        private NetworkManager _networkManager;
        private GameObject _player;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _networkManager = InstanceFinder.NetworkManager;
            Debug.Log($"Awaiting client connection");
            SceneManager.OnClientLoadedStartScenes += OnClientConnect;
        }

        private void OnClientConnect(NetworkConnection conn, bool asServer)
        {
            Debug.Log($"Conn {conn.ClientId} requesting ownership.");
            GiveOwnership(conn);
        }

        private void Start()
        {
            Debug.Log($"Is Server: {IsServerInitialized}, Is Client: {IsClientInitialized}");
            if (IsServerInitialized)
            {
                InvokeRepeating(nameof(SpawnEnemies), 5f, 5f);
            }
        }
        
        [ObserversRpc]
        private void DebugClientSpawn()
        {
            Debug.Log("Client received spawn update.");
        }

        private void SpawnEnemies()
        {
            foreach (EnemyInfo e in enemies)
            {
                NetworkObject nob = _networkManager.GetPooledInstantiated(e.enemyPrefab, e.position, e.rotation, true);
                ServerManager.Spawn(nob);
            }
            
            DebugClientSpawn();
        }
    }
}