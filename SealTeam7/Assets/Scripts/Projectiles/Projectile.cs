using Enemies.Utils;
using Game;
using Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace Projectiles
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed;
        public Vector3 TargetPosition { get; set; }
        public PlayerDamageable ToDamage { get; set; }
        public int Damage { get; set; }

        public ProjectileType projectileType;

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive())
            {
                ProjectilePool.GetInstance().ReturnToPool(projectileType, gameObject);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, TargetPosition, speed * Time.deltaTime);

            if (transform.position == TargetPosition)
            {
                ToDamage.TakeDamage(Damage);
                ProjectilePool.GetInstance().ReturnToPool(projectileType, gameObject);
            }
        }
    }
}