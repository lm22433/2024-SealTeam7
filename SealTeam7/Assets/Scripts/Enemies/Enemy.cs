using Player;
using UnityEngine;
using UnityEngine.UI;
using Weapons;

namespace Enemies
{
    public abstract class Enemy : MonoBehaviour, IDamageable
    {
        [SerializeField] public Slider healthBar;
        [SerializeField] protected float maxHealth;
        [SerializeField] protected float damage;
        protected GameObject PlayerObject;
        protected IDamageable Player;
        private float _health;

        public virtual void Start()
        {
            _health = maxHealth;
            healthBar.maxValue = maxHealth;
            healthBar.value = _health;
            PlayerObject = GameObject.FindWithTag("Player");
            Player = PlayerObject.GetComponent<PlayerManager>();
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
        
        public abstract void Attack();

        public virtual void Update()
        {
            healthBar.transform.LookAt(PlayerObject.transform.position);
        }
    }
}