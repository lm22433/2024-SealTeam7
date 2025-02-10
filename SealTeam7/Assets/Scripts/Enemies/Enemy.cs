using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameKit.Dependencies.Utilities;
using Kinect;
using Map;
using Player;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using Weapons;

namespace Enemies
{
    public abstract class Enemy : NetworkBehaviour, IDamageable
    {
        [SerializeField] protected Slider healthBar;
        [SerializeField] protected float maxHealth;
        [SerializeField] protected float damage;
        [SerializeField] protected VisualEffect attackEffect;
        private KinectAPI _kinect;
        private NoiseGenerator _noiseGenerator;
        private GameObject _player;

        private readonly SyncVar<float> _health = new SyncVar<float>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            _kinect = FindFirstObjectByType<KinectAPI>();
            _noiseGenerator = FindFirstObjectByType<NoiseGenerator>();
            _health.Value = maxHealth;
            healthBar.maxValue = maxHealth;
            healthBar.value = _health.Value;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            Debug.Log($"Looking for player");
            
            var players = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
            foreach (var p in players) {
                if (p.gameObject.GetComponentInParent<NetworkObject>().IsOwner) {
                    _player = p.gameObject;
                }
            }
            
            Debug.Log($"Found {players.Length} players, owner is {_player.gameObject.GetComponentInParent<NetworkObject>().OwnerId}");
        }
        
        public void TakeDamage(float dmg)
        {
            _health.Value -= dmg;
            
            if (_health.Value <= 0)
            {
                Die();
            }
        }
        
        public virtual void Die()
        {
            Destroy(gameObject);
        }
        
        public abstract void Attack(Collider hit);

        [TargetRpc]
        public virtual void DealDamageRPC(NetworkConnection conn, PlayerManager playerMgr, float dmg) {}

        public virtual void Update()
        {
            // run on client only
            if (!IsServerInitialized)
            {
                // turn health bar towards player
                if (_player) healthBar.transform.LookAt(_player.transform.position);
                healthBar.value = _health.Value;
                return;
            }
            
            // only run on server
            
            var x = (int) transform.position.x;
            var z = (int) transform.position.z;
        
            // sit on terrain
            transform.SetPosition(false,
                _kinect.isKinectPresent
                    ? new Vector3(transform.position.x, _kinect.GetHeight(x, z) + 0.5f * transform.lossyScale.y, transform.position.z)
                    : new Vector3(transform.position.x, _noiseGenerator.GetHeight(x, z), transform.position.z));
        }
    }
}