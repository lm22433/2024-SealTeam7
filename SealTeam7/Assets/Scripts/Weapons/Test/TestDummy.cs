using System;
using UnityEngine;
using UnityEngine.UI;

namespace Weapons.Test
{
    public class TestDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 150f;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Transform player;
        private float _health;

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

        private void Update()
        {
            healthBar.transform.LookAt(player.position);
        }
    }
}