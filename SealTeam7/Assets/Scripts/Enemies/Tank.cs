using Enemies.Utils;
using Map;
using Player;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace Enemies
{
    public class Tank : Enemy
    {
        [SerializeField] private Transform gun;
        [SerializeField] private ParticleSystem[] dustTrails;
        [SerializeField] private VisualEffect smokeDmg;
        [SerializeField] private int lives = 2;
        [SerializeField] private float gracePeriod = 3.0f;
        private float _deathTime;

        public override void Init()
        {
            base.Init();
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
        
        protected override void EnemyUpdate()
        {
            _deathTime += Time.deltaTime;
            
            DisallowMovement = Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0.5f;
            DisallowShooting = Vector3.Dot(transform.forward, TargetPosition - transform.position) < 0.8f || !Grounded;
            
            // gun rotation
            switch (State)
            {
                case EnemyState.Moving:
                {
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation((Path.Length > 0 ? Path[PathIndex] : TargetPosition) - transform.position).eulerAngles.y, transform.eulerAngles.z);
                    
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
                case EnemyState.AttackHands:
                {
                    var xAngle = Quaternion.LookRotation(TargetPosition - gun.position).eulerAngles.x - transform.eulerAngles.x;
                    TargetRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, transform.eulerAngles.z);
                    break;
                }
                case EnemyState.Dying:
                {
                    foreach (var dustTrail in dustTrails)
                        if(dustTrail.isPlaying) dustTrail.Stop();
                    break;
                }
            }
        }
    }
}