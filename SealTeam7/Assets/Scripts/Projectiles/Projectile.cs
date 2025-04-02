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
        private Vector3 _gravity;

        public void Init()
        {
            _startPosition = transform.position;
            _gravity = new Vector3(0f, -9.81f, 0f);
            
            if (projectileType == ProjectileType.MortarProjectile)
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                var t = 10f;
                var p = TargetPosition - _startPosition;
                //rb.useGravity = false;
                rb.linearVelocity = new Vector3(p.x / t, p.y / t + 0.5f * _gravity.y * t, p.z / t);
            }
            
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
            
            //if (TryGetComponent(out Rigidbody rb)) rb.AddForce(_gravity, ForceMode.Acceleration);

            if (projectileType != ProjectileType.MortarProjectile) transform.position = Vector3.MoveTowards(transform.position, TargetPosition, speed * Time.deltaTime);

            if (transform.position == TargetPosition)
            {
                ToDamage.TakeDamage(Damage);
                ProjectilePool.GetInstance().ReturnToPool(projectileType, gameObject);
            }
        }
    }
}