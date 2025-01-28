using System;
using UnityEngine;
using UnityEngine.UI;
using Weapons;

namespace Enemies
{
    public class Enemy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 150f;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Transform player;
        [SerializeField] private float shootRange;
        [SerializeField] private float shootDelay;
        
        private float _health;
        private float _timeSinceLastShoot;

        public void Start()
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
        
        private void Die()
        {
            Destroy(gameObject);
        }
        
        private void Shoot()
        {
            Debug.Log("Shot!");
            _timeSinceLastShoot = 0;
        }

        private void Update()
        {
            _timeSinceLastShoot += Time.deltaTime;
            if ((player.transform.position - transform.position).sqrMagnitude < shootRange * shootRange)
            {
                if (_timeSinceLastShoot > shootDelay)
                {
                    Shoot();
                }
            }
            
            healthBar.transform.LookAt(player.position);
        }
    }
}