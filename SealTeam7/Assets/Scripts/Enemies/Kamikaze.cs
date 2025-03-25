using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Kamikaze : Enemy
    {
        [SerializeField] private ParticleSystem trail;
        [SerializeField] private ParticleSystem smokeTrail;
        [SerializeField] private ParticleSystem chargeParticles;

        public override void Init()
        {
            base.Init();
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override float Heuristic(Node start, Node end)
        {
            return start.WorldPos.y > flyHeight - 20f ? 10000f : 0f;
        }
        
        protected override void Attack(PlayerDamageable toDamage)
        {
            gunFireSound.Post(gameObject);
            
            toDamage.TakeDamage(attackDamage);
            killScore = 0;
            SetupDeath();
        }

        protected override void EnemyUpdate()
        {
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

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight && State != EnemyState.Dying) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight && State != EnemyState.Dying) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }

        public override void SetupDeath()
		{
			base.SetupDeath();
            trail.Stop();
            smokeTrail.Stop();
		}
    }
}