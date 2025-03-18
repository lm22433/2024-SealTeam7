using Player;
using UnityEngine;

namespace Enemies
{
    public class Kamikaze : Enemy
    {
        [SerializeField] private ParticleSystem trail;
        [SerializeField] private ParticleSystem smokeTrail;
        [SerializeField] private ParticleSystem chargeParticles;
        
        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }

        protected override void EnemyUpdate()
        {
            switch (State)
            {
                case EnemyState.AttackCore:
                {
                    TargetPosition = new Vector3(TargetPosition.x, flyHeight, TargetPosition.z);
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x,
                        Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y,
                        transform.eulerAngles.z);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.Euler(
                        transform.eulerAngles.x,
                        Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y,
                        transform.eulerAngles.z);
                    break;
                }
                case EnemyState.Moving:
                {
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation((Path.Length > 0 ? Path[PathIndex] : TargetPosition) - transform.position).eulerAngles.y, transform.eulerAngles.z);
                    break;
                }
            }
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