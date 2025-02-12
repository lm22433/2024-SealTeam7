using System;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Map;
using Player;
using UnityEngine;
using UnityEngine.UI;
using Weapons;

namespace Enemies
{
    public abstract class Enemy : SandRider, IDamageable
    {
        [SerializeField] protected Slider healthBar;
        [SerializeField] protected float maxHealth;
        [SerializeField] protected float damage;
        [SerializeField] protected float attackRange;
        [SerializeField] protected float attackDelay;

        private GameObject _player;
        private float _timeSinceAttack;
        private Collider[] _hitResults;
        private readonly SyncVar<float> _health = new();

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            _health.Value = maxHealth;
            healthBar.maxValue = maxHealth;
            healthBar.value = _health.Value;
            _hitResults = new Collider[12];
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
            _health.Value -= dmg;
            
            if (_health.Value <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        protected abstract void Attack(Collider hit);

        [TargetRpc]
        protected virtual void DealDamageRpc(NetworkConnection conn, PlayerManager playerMgr, float dmg)
        {
            playerMgr.TakeDamage(dmg);
        }

        protected override void ClientUpdate()
        {
            base.ClientUpdate();
            
            // turn health bar towards player
            if (_player) healthBar.transform.LookAt(_player.transform.position);
            healthBar.value = _health.Value;
        }

        protected override void ServerUpdate()
        {
            base.ServerUpdate();
            
            _timeSinceAttack += Time.deltaTime;
            
            Array.Clear(_hitResults, 0, _hitResults.Length);
            Physics.OverlapSphereNonAlloc(transform.position, attackRange, _hitResults, LayerMask.GetMask("Player"));
            // sort by distance from enemy
            Collider closestPlayer = _hitResults.OrderBy(c => c ? (transform.position - c.transform.position).sqrMagnitude : float.MaxValue).First();
            if (closestPlayer && _timeSinceAttack > attackDelay)
            {
                _timeSinceAttack = 0;
                Attack(closestPlayer);
            }
        }
    }
}