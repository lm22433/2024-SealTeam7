using Player;
using UnityEngine;

namespace Enemies
{
    public class Kamikaze : Enemy
    {
        [SerializeField] float flyHeight;
        [SerializeField] private ParticleSystem trail;
        [SerializeField] private ParticleSystem smokeTrail;
        [SerializeField] private ParticleSystem chargeParticles;
        
        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
            killScore = 0;
            SetupDeath();
        }
        
        protected override void EnemyUpdate()
        {
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetDirection).eulerAngles.y, transform.eulerAngles.z);
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight && State != EnemyState.Dying) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight && State != EnemyState.Dying) Rb.AddForce(Vector3.up, ForceMode.Impulse);
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if (!trail.isPlaying) trail.Play();
                    if (!smokeTrail.isPlaying) smokeTrail.Play();
                    chargeParticles.Stop();
                    break;
                }
                case EnemyState.AttackCore:
                {
                    if(!chargeParticles.isPlaying) chargeParticles.Play();
                    smokeTrail.Stop();
                    Rb.linearVelocity = new Vector3 (0,0,0);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    if (!chargeParticles.isPlaying) chargeParticles.Play();
                    smokeTrail.Stop();
                    Rb.linearVelocity = new Vector3 (0,0,0);
                    break;
                }
            }
        }

        public override void SetupDeath()
		{
			base.SetupDeath();
            trail.Stop();
            smokeTrail.Stop();
		}
    }
}