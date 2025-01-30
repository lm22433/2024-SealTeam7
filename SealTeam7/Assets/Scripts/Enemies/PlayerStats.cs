using UnityEngine;
using Weapons;

namespace Enemies
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth;
        private float _health;

        private void Start()
        {
            _health = maxHealth;
        }
        
        public void TakeDamage(float dmg)
        {
            _health -= dmg;
        }

        private void FixedUpdate()
        {
            Debug.Log(_health);
        }
    }
}