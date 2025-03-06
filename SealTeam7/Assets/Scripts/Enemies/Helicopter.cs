using Player;
using UnityEngine;

namespace Enemies
{
    public class Helicopter : Enemy
    {
        [SerializeField] float flyHeight;
        [SerializeField] private ParticleSystem gunEffects;
        [SerializeField] private ParticleSystem deathParticles;
        private float _lastAttack;

        private void Awake() {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void Attack(PlayerDamageable target)
        {
            if (!gunEffects.isPlaying) gunEffects.Play();
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            TargetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y, transform.rotation.eulerAngles.z);
            TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
            Rb.MoveRotation(TargetRotation);
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    Rb.AddForce(TargetDirection * (moveSpeed * 10f));
                    break;
                }
                case EnemyState.AttackCore:
                {
                    Rb.linearVelocity = new Vector3 (0,0,0);
                    if (_lastAttack > attackInterval)
                    {
                        Attack(EnemyManager.godlyCore);
                        _lastAttack = 0f;
                    }
                    break;
                }
                case EnemyState.AttackHands:
                {
                    Rb.linearVelocity = new Vector3 (0,0,0);
                    if (_lastAttack < attackInterval)
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
			deathParticles.Play();
			State = EnemyState.Dying;
		}

        public override void Die()
        {
			base.Die();
        }
    }
}