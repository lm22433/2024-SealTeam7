using Player;
using UnityEngine;

namespace Enemies
{
    public class Kamikaze : Enemy
    {
        [SerializeField] float flyHeight;
        [SerializeField] private ParticleSystem deathParticles;
        [SerializeField] private ParticleSystem trail;
        [SerializeField] private ParticleSystem smokeTrail;
        [SerializeField] private ParticleSystem chargeParticles;

        private float _charge;

        private void Awake() {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {

            TargetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y, transform.rotation.eulerAngles.z);
            TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            if (State == EnemyState.AttackCore || State == EnemyState.AttackHands) _charge += Time.deltaTime;
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }

        protected override void EnemyFixedUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if(!trail.isPlaying) trail.Play();
                    if(!smokeTrail.isPlaying) smokeTrail.Play();
                    chargeParticles.Stop();
                    Rb.MoveRotation(TargetRotation);
                    Rb.AddForce(TargetDirection * (moveSpeed * 10f));

                    break;
                }
                case EnemyState.AttackCore:
                {
                    //trail.Stop();
                    if(!chargeParticles.isPlaying) chargeParticles.Play();
                    smokeTrail.Stop();
                    Rb.linearVelocity = new Vector3 (0,0,0);
                    if (_charge > attackInterval)
                    {
                        Attack(EnemyManager.godlyCore);
                        killScore = 0;
                        this.SetupDeath();
                    }
                    break;
                }
                case EnemyState.AttackHands:
                {
                    //trail.Stop();
                    if(!chargeParticles.isPlaying) chargeParticles.Play();
                    smokeTrail.Stop();
                    Rb.linearVelocity = new Vector3 (0,0,0);
                    if (_charge > attackInterval)
                    {
                        Attack(EnemyManager.godlyHands);
                        killScore = 0;
                        this.SetupDeath();
                    }
                    break;
                }
            }
        }

        public override void SetupDeath()
		{
			deathParticles.Play();
            smokeTrail.Stop();
			State = EnemyState.Dying;
		}
    }
}