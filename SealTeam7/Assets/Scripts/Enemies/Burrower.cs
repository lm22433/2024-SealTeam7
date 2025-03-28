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
        [SerializeField] private float diveSpeed = 1f;
        protected internal bool Burrowing;
        private float _newHeight;

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
            if (State is EnemyState.Dying) return;
            
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
                        _newHeight = burrowDepth;
                    }
                    
                    break;
                }
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                case EnemyState.Idle:
                {
                    if (Burrowing)
                    {
                        Burrowing = false;
                        _newHeight = MapManager.GetInstance().GetHeight(transform.position) + groundedOffset;
                        Rb.linearVelocity = Vector3.zero;
                    }

                    break;
                }
            }
            
            transform.position = new Vector3(
                transform.position.x,
                Mathf.Lerp(transform.position.y, _newHeight, diveSpeed * Time.deltaTime),
                transform.position.z
            );
            
            Grounded = !Burrowing && Grounded;
            Rb.freezeRotation = Burrowing;
            Rb.detectCollisions = !Burrowing;
        }
    }
}