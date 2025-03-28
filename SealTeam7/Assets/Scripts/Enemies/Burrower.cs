using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Burrower : Vehicle
    {
        [SerializeField] private Transform drill;
        [SerializeField] private float drillSpeed;
        [SerializeField] private float burrowDepth;
        protected internal bool Burrowing;

        public override void Init()
        {
            base.Init();
            Burrowing = false;
        }

        protected override float Heuristic(Node start, Node end)
        {
            return 0f;
        }

        protected override void Attack(PlayerDamageable toDamage)
        {
            toDamage.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            coreTargetHeightOffset = Burrowing ? -burrowDepth : 0f;
            Debug.Log(State);
            DisallowShooting = Vector3.Dot(transform.forward, TargetPosition - transform.position) < 0.8f || !Grounded;
            drill.Rotate(Time.deltaTime * drillSpeed * Vector3.forward);
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if (!Burrowing && Grounded)
                    {
                        Burrowing = true;
                    }

                    if (Burrowing)
                    {
                        transform.position = new Vector3(
                            transform.position.x,
                            burrowDepth,
                            transform.position.z
                        );
                    }
                    
                    break;
                }
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                case EnemyState.Idle:
                {
                    if (Burrowing)
                    {
                        Rb.linearVelocity = Vector3.zero;
                        transform.position = new Vector3(
                            transform.position.x,
                            MapManager.GetInstance().GetHeight(transform.position) + groundedOffset,
                            transform.position.z
                        );
                        
                        Burrowing = false;
                    }

                    break;
                }
            }
            
            Grounded = !Burrowing && Grounded;
            Rb.freezeRotation = Burrowing;
            Rb.detectCollisions = !Burrowing;
        }
    }
}