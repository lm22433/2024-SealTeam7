using FishNet.Connection;
using FishNet.Object;
using GameKit.Dependencies.Utilities;
using Kinect;
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
        private float _health;
        [SerializeField] private GameObject _player;

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            _kinect = FindFirstObjectByType<KinectAPI>();
            _health = maxHealth;
            healthBar.maxValue = maxHealth;
            healthBar.value = _health;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            var players = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
            foreach (var p in players) {
                if (p.gameObject.GetComponentInParent<NetworkObject>().IsOwner) {
                    _player = p.gameObject;
                }
            }
        }

        public void TakeDamage(float dmg)
        {
            _health -= dmg;
            healthBar.value = _health;

            if (_health <= 0)
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
        public virtual void DealDamageRPC(NetworkConnection conn, PlayerManager playerManager, float dmg) {}

        public virtual void Update()
        {
            // turn health bar towards player
            if (_player) healthBar.transform.LookAt(_player.transform.position);
            
            // only run on server
            if (IsServerInitialized)
            {
                var x = (int) transform.position.x;
                var z = (int) transform.position.z;
            
                // sit on terrain
                transform.SetPosition(false, new Vector3(transform.position.x, _kinect.GetHeight(x, z), transform.position.z));   
            }
        }
    }
}