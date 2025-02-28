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
            TargetRotation = Quaternion.LookRotation(Target - gun.position);
            gun.rotation = Quaternion.Slerp(gun.rotation, TargetRotation, aimSpeed * Time.deltaTime);
            
            TargetRotation = Quaternion.LookRotation(new Vector3(Target.x, transform.position.y, Target.z) - transform.position);
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            Rb.MoveRotation(TargetRotation);
                        
            switch (State)
            {
                case EnemyState.Moving:
                {
                    gunEffects.Stop();
                    Rb.AddForce(transform.forward * (moveSpeed * 10f));
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