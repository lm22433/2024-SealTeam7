using Enemies.Utils;
using Map;
using Player;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies
{
    public class Spawner : Enemy
    {
        [SerializeField] private EnemyData spawnee;
        [SerializeField] private ParticleSystem[] dustTrails;
        [SerializeField] private VisualEffect smokeDmg;
        [SerializeField] private int lives = 2;
        [SerializeField] private float gracePeriod = 3.0f;
        private float _deathTime;

        public override void Init()
        {
            base.Init();
            LastAttack = attackInterval - 2.0f;
            lives = 2;
            smokeDmg.Stop();
            _deathTime = 0f;
        }
        
        protected override float Heuristic(Node start, Node end)
        {
            return (start.WorldPos.y - start.Parent?.WorldPos.y ?? start.WorldPos.y) * 100f;
        }
        
        public override void SetupDeath()
        {
            if (_deathTime < gracePeriod) return;
            
            _deathTime = 0f;
            lives--;
            if (lives > 0)
            {
                smokeDmg.Play();
                transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position) + groundedOffset, transform.position.z);
                Rb.linearVelocity = Vector3.zero;
            }
            else base.SetupDeath();
        }

        protected override void Attack(PlayerDamageable player)
        {
            EnemyManager.SpawnerSpawn(new Vector3(transform.position.x, transform.position.y + 2.0f, transform.position.z - 2.0f), spawnee, attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            DisallowMovement = Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0.8f;
            DisallowShooting = Vector3.Dot(transform.forward, TargetPosition - transform.position) < 0.8f || !Grounded;
            
            // gun rotation
            switch (State)
            {
                case EnemyState.Moving:
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(TargetPosition - transform.position), aimSpeed * Time.deltaTime);
                    if (DisallowMovement || Rb.position.y > MapManager.GetInstance().GetHeight(transform.position) + groundedOffset)
                    {
                        foreach (var dustTrail in dustTrails)
                            if (dustTrail.isPlaying) dustTrail.Stop();
                    }
                    else
                    {
                        foreach (var dustTrail in dustTrails)
                            if (!dustTrail.isPlaying) dustTrail.Play();
                    }
                    break;
                }
                case EnemyState.AttackCore:
                {
                    // var xAngle = Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.x - transform.eulerAngles.x;
                    // TargetRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    // transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    // TargetRotation = Quaternion.Euler(Vector3.Angle(TargetPosition - transform.position, transform.right), 0f, 0f);
                    // transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.Dying:
                {
                    foreach (var dustTrail in dustTrails)
                        if(dustTrail.isPlaying) dustTrail.Stop();
                    break;
                }
            }
            
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, transform.eulerAngles.z);
            TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        }
    }
}