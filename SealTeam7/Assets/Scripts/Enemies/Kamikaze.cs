using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Kamikaze : Aircraft
    {
        [SerializeField] private ParticleSystem trail;
        [SerializeField] private ParticleSystem smokeTrail;
        [SerializeField] private ParticleSystem chargeParticles;

        protected override void Attack(PlayerDamageable toDamage)
        {
            gunFireSound.Post(gameObject);
            
            toDamage.TakeDamage(attackDamage);
            killScore = 0;
            SetupDeath();
        }

        protected override void EnemyUpdate()
        {
            base.EnemyUpdate();
            
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