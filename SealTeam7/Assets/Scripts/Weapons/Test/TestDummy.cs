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
        private float health;

        public void Start()
        {
            health = maxHealth;
            healthBar.maxValue = maxHealth;
            healthBar.value = health;
        }
        
        public void TakeDamage(float dmg)
        {
            health -= dmg;
            healthBar.value = health;
            
            Debug.Log($"{gameObject.name} took {dmg} damage. Remaining health: {health}");

            if (health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"{gameObject.name} has died!");
            Destroy(gameObject);
        }

        private void Update()
        {
            healthBar.transform.LookAt(player.position);
        }
    }
}