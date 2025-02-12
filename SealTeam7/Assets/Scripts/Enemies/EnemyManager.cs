using System;
using FishNet;
using UnityEngine;
using FishNet.Object;
using Map;
using Python;

namespace Enemies
{
    public class EnemyManager : NetworkBehaviour
    {
        [SerializeField] private GameObject[] enemies;
        private GameObject _player;
        private KinectAPI _kinect;
        private SandboxObject[] _sandboxObjects;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _kinect = FindFirstObjectByType<KinectAPI>();
            Invoke(nameof(SpawnEnemies), 15f);
        }
        
        private void SpawnEnemies()
        {
            _sandboxObjects = _kinect.RequestSandboxObjects();
            
            foreach (SandboxObject obj in _sandboxObjects)
            {
                var prefab = obj switch
                {
                    SandboxObject.Bunker => enemies[0],
                    SandboxObject.Spawner => enemies[1],
                    _ => throw new ArgumentOutOfRangeException()
                };
                var pos = new Vector3(obj.GetX() / 1920f * 512f * 8f, 300, obj.GetY() / 1080f * 512f * 8f);
                var rot = Quaternion.identity;
                
                NetworkObject nob = InstanceFinder.NetworkManager.GetPooledInstantiated(prefab, pos, rot, true);
                ServerManager.Spawn(nob);
            }
        }
    }
}