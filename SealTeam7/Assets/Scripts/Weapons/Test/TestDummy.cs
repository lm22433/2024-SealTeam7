using UnityEngine;

namespace Weapons.Test
{
    public class TestDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float health = 100f;

        public void TakeDamage(float dmg)
        {
            health -= dmg;
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
    }
}