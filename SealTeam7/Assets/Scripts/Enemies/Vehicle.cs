using Enemies.Utils;
using Map;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies
{
    public abstract class Vehicle : Enemy
    {
        [SerializeField] private ParticleSystem[] dustTrails;
        [SerializeField] private VisualEffect smokeDmg;
        [SerializeField] private int maxLives = 2;
        [SerializeField] private float gracePeriod = 2.0f;
        private float _deathTime;
        private int _lives;
        
        public override void Init()
        {
            base.Init();
            _lives = maxLives;
            smokeDmg.Stop();
            _deathTime = 0f;
        }
        
        protected override float Heuristic(Node start, Node end)
        {
            return Mathf.Max(start.WorldPos.y - start.Parent?.WorldPos.y ?? start.WorldPos.y, 0f) * 200f;
        }
        
        public override void SetupDeath()
        {
            if (_deathTime < gracePeriod) return;
            
            _deathTime = 0f;
            _lives--;
            if (_lives > 0)
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
            
            switch (State)
            {
                case EnemyState.MoveAndAttack:
                case EnemyState.Moving:
                {
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
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, transform.eulerAngles.z).normalized;
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