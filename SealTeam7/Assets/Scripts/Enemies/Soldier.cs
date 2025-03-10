using System;
using Map;
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

		protected override void Start()
		{
			base.Start();
			deathDuration = 0.5f;
			buriedAmount = 0.25f;
		}
        
        protected override void Attack(PlayerDamageable target)
        {
            if (!gunEffects.isPlaying) gunEffects.Play();
            target?.TakeDamage(attackDamage);
        }

        protected override void EnemyUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                case EnemyState.AttackCore:
                {
                    TargetRotation = Quaternion.Slerp(Rb.rotation, Quaternion.LookRotation(new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position), aimSpeed * Time.deltaTime);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.identity, aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.LookRotation(Target.transform.position - gun.position);
                    gun.rotation = Quaternion.Slerp(gun.rotation, TargetRotation, aimSpeed * Time.deltaTime);
                    float targetY = Math.Min(Target.transform.position.y + 24.0f, 3.0f + _mapManager.GetHeight(Target.transform.position.x, Target.transform.position.z));
                    TargetRotation = Quaternion.Slerp(Rb.rotation, Quaternion.LookRotation(new Vector3(Target.transform.position.x, targetY, Target.transform.position.z) - transform.position), aimSpeed * Time.deltaTime);
                    break;
                }
            }
            TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            Rb.MoveRotation(Quaternion.Slerp(Rb.rotation, TargetRotation, aimSpeed * Time.fixedDeltaTime));
                        
            switch (State)
            {
                case EnemyState.Moving:
                {
                    Rb.AddForce(TargetDirection * (moveSpeed * 10f));
                    //gunEffects.Stop();
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
                    if (_lastAttack > attackInterval)
                    {
                        Attack(EnemyManager.godlyHands);
                        _lastAttack = 0f;
                    }
                    break;
                }
                case EnemyState.Dying:
                {
					var x = transform.position.x;
					var z = transform.position.z;
					transform.position = new Vector3(x, _mapManager.GetHeight(x, z)-buried, z);
                    break;
                }
            }
        }

		public override void SetupDeath()
		{
			State = EnemyState.Dying;
			deathParticles.Play();
		}

        public override void Die()
        {
            deathParticles.Play();
            base.Die();
        }
    }
}