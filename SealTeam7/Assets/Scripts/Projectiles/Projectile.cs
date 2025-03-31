using Enemies.Utils;
using Game;
using JetBrains.Annotations;
using Map;
using Player;
using UnityEngine;

namespace Projectiles
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed;
        [SerializeField] [CanBeNull] private TrailRenderer trail;
        public Vector3 TargetPosition { get; set; }
        public PlayerDamageable ToDamage { get; set; }
        public int Damage { get; set; }
        public ProjectileType projectileType;

        private Vector3 _startPosition;

        public void Init()
        {
            if (trail) trail.Clear();
        }

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive() ||
            (transform.position - EnemyManager.GetInstance().godlyCore.transform.position).sqrMagnitude > EnemyManager.GetInstance().sqrMaxEnemyDistance)
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