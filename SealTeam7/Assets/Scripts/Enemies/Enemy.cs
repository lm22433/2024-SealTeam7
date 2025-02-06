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
        private GameObject _player;

        public virtual void Start()
        {
            _kinect = FindFirstObjectByType<KinectAPI>();
            _health = maxHealth;
            healthBar.maxValue = maxHealth;
            healthBar.value = _health;
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

        public virtual void Update()
        {
            if (!IsServerInitialized) return;

            var x = (int) transform.position.x;
            var z = (int) transform.position.z;
            
            // sit on terrain
            transform.SetPosition(false, new Vector3(transform.position.x, _kinect.GetHeight(x, z), transform.position.z));
            
            // look at player
            if (_player)
            {
                healthBar.transform.LookAt(_player.transform.position);
            }
            else
            {
                var players = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
                foreach (var p in players) {
                    if (p.gameObject.GetComponentInParent<NetworkObject>().IsOwner) {
                        _player = p.gameObject;
                    }
                }
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        }
    }
}