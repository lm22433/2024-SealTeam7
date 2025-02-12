using System;
using FishNet;
using UnityEngine;
using FishNet.Object;
using Map;
using Python;
using Random = UnityEngine.Random;

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
            if (_kinect.isObjectDetection) Invoke(nameof(SpawnEnemies), 15f);
            else SpawnEnemies();
        }
        
        private void SpawnEnemies()
        {
            if (_kinect.isObjectDetection)
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
            else
            {
                foreach (GameObject enemy in enemies)
                {
                    NetworkObject nob = InstanceFinder.NetworkManager.GetPooledInstantiated(enemy, new Vector3(2000f + Random.Range(0, 400f), 200f, 2000f + Random.Range(0, 400f)), Quaternion.identity, true);
                    ServerManager.Spawn(nob);
                }
            }
        }
    }
}