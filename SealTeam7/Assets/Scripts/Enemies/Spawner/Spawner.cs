using FishNet;
using FishNet.Object;
using UnityEngine;

namespace Enemies.Spawner
{
    public class Spawner : Enemy
    {
        [SerializeField] private GameObject enemyPrefab;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            
            InvokeRepeating(nameof(SpawnEnemy), 2f, 5f);
        }

        protected override void Attack(Collider hit) {}

        private void SpawnEnemy()
        {
            NetworkObject nob = InstanceFinder.NetworkManager.GetPooledInstantiated(enemyPrefab, transform.position + transform.forward, Quaternion.identity, true);
            InstanceFinder.ServerManager.Spawn(nob);
        }
    }
}