using FishNet.Object;
using Player;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using Weapons;

namespace Enemies
{
    public abstract class Enemy : NetworkBehaviour, IDamageable
    {
        [SerializeField] public Slider healthBar;
        [SerializeField] protected float maxHealth;
        [SerializeField] protected float damage;
        [SerializeField] protected VisualEffect attackEffect;
        private float _health;

        public virtual void Start()
        {
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
            //TODO: fix for multiple players
            //healthBar.transform.LookAt(player.transform.position);
        }
    }
}