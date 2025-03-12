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

        private float _charge;

        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {

            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, transform.eulerAngles.z);
            TargetDirection = TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            
            if (State == EnemyState.AttackCore || State == EnemyState.AttackHands) _charge += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight && State != EnemyState.Dying) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight && State != EnemyState.Dying) Rb.AddForce(Vector3.up, ForceMode.Impulse);
            
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
                        SetupDeath();
                    }
                    break;
                }
            }
        }

        public override void SetupDeath()
		{
			base.SetupDeath();
            smokeTrail.Stop();
		}
    }
}