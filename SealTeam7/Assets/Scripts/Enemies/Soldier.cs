using Player;
using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
    {
        [SerializeField] private Transform gun;
        [SerializeField] private ParticleSystem gunEffects;
        [SerializeField] private ParticleSystem deathParticles;
        private float _lastAttack;
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }

        protected override void EnemyUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                case EnemyState.AttackCore: break;
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.LookRotation(Target.transform.position - gun.position);
                    gun.rotation = Quaternion.Slerp(gun.rotation, TargetRotation, aimSpeed * Time.deltaTime);
                    break;
                }
            }
            
            TargetRotation = Quaternion.LookRotation(new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position);
            TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            Rb.MoveRotation(TargetRotation);
                        
            switch (State)
            {
                case EnemyState.Moving:
                {
                    Rb.AddForce(TargetDirection * (moveSpeed * 10f));
                    gunEffects.Stop();
                    break;
                }
                case EnemyState.AttackCore:
                {
                    gunEffects.Play();
                    if (_lastAttack > attackInterval)
                    {
                        gunEffects.Play();
                        Attack(EnemyManager.godlyCore);
                        _lastAttack = 0f;
                    }
                    break;
                }
                case EnemyState.AttackHands:
                {
                    gunEffects.Play();
                    if (_lastAttack > attackInterval)
                    {
                        gunEffects.Play();
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