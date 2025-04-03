using System;
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
        [SerializeField] private float hitRadius;
        [SerializeField] [CanBeNull] private TrailRenderer trail;
        public Vector3 TargetPosition { get; set; }
        public PlayerDamageable ToDamage { get; set; }
        public int Damage { get; set; }
        public ProjectileType projectileType;

        [CanBeNull] private Rigidbody _rb;
        private Vector3 _startPosition;
        private float _sqrHitRadius;

        public void Init()
        {
            _sqrHitRadius = hitRadius * hitRadius;
            _startPosition = transform.position;
            
            if (projectileType == ProjectileType.MortarProjectile)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
                var g = Physics.gravity;
                var dst = TargetPosition - _startPosition;
                
                const float theta = 60f * Mathf.Deg2Rad;
                
                var v = Mathf.Sqrt(dst.magnitude * -g.y / Mathf.Sin(2 * theta));
                var vForward = v * Mathf.Cos(theta);
                var vUp = v * Mathf.Sin(theta);

                _rb!.detectCollisions = false;
                _rb!.linearVelocity = Vector3.ProjectOnPlane(dst.normalized, Vector3.up) * vForward + vUp * Vector3.up;
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
            
            transform.rotation = Quaternion.LookRotation(_rb?.linearVelocity ?? TargetPosition - _startPosition);
            if (projectileType != ProjectileType.MortarProjectile) transform.position = Vector3.MoveTowards(transform.position, TargetPosition, speed * Time.deltaTime);

            if ((transform.position - TargetPosition).sqrMagnitude < _sqrHitRadius)
            {
                ToDamage.TakeDamage(Damage);
                ProjectilePool.GetInstance().ReturnToPool(projectileType, gameObject);
            }
        }
    }
}