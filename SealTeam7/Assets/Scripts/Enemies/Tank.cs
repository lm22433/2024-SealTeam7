using Player;
using UnityEngine;

namespace Enemies
{
    public class Tank : Enemy
    {
        [SerializeField] private Transform turret;
        [SerializeField] private Transform gun;
        [SerializeField] private ParticleSystem deathParticles;
        private float _lastAttack;
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            // gun rotation
            // TargetRotation = Quaternion.Euler(Vector3.Angle(Target - gun.position, Vector3.right), 0f, 0f);
            // gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
            
            // turret rotation
            TargetRotation = Quaternion.Euler(0f, Quaternion.LookRotation(Target.transform.position - turret.position).eulerAngles.y - transform.rotation.eulerAngles.y, 0f);
            turret.localRotation = Quaternion.Slerp(turret.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
            
            TargetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y, transform.rotation.eulerAngles.z);
            TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if (Mathf.Abs(Vector3.Dot(transform.forward, Vector3.up)) < 0.1f) break;
                    Rb.MoveRotation(TargetRotation);
                    Rb.AddForce(TargetDirection * (moveSpeed * 10f));
                    break;
                }
                case EnemyState.AttackCore:
                {
                    if (_lastAttack > attackInterval)
                    {
                        Attack(EnemyManager.godlyCore);
                        _lastAttack = 0f;
                    }
                    break;
                }
                case EnemyState.AttackHands:
                {
                    if (_lastAttack < attackInterval)
                    {
                        Attack(EnemyManager.godlyHands);
                        _lastAttack = 0f;
                    }
                    break;
                }
            }
        }

        public override void Die()
        {
            deathParticles.Play();
            base.Die();
        }
    }
}