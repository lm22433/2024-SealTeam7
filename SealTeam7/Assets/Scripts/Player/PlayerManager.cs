using UnityEngine;
using UnityEngine.UI;
using Weapons;

namespace Player
{
    public class PlayerManager : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth;
        [SerializeField] private Slider healthBar;
        private float _health;

        private void Start()
        {
            _health = maxHealth;
            healthBar.maxValue = maxHealth;
            healthBar.value = _health;
        }

        public void TakeDamage(float dmg)
        {
            _health -= dmg;
        }

        public void Update()
        {
            healthBar.value = _health;
        }
    }
}