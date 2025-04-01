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
            transform.position = new Vector3(transform.position.x, burrowDepth, transform.position.z);
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
            base.EnemyUpdate();
            
            // WOULD DIE EXPOSED
            if (transform.position.y >= MapManager.GetInstance().GetHeight(EnemyManager.godlyCore.transform.position) && Burrowing && !IsDying && State is EnemyState.Moving)
            {
                SetupDeath();
            }
            
            coreTargetHeightOffset = transform.position.y - MapManager.GetInstance().GetHeight(transform.position);
            drill.Rotate(Time.deltaTime * drillSpeed * Vector3.forward);
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if (!Burrowing && Grounded)
                    {
                        Burrowing = true;
                        transform.position = new Vector3(transform.position.x, burrowDepth, transform.position.z);
                    }
                    
                    break;
                }
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                case EnemyState.Idle:
                {
                    if (!Burrowing && Grounded && transform.position.y <= MapManager.GetInstance().GetHeight(transform.position))
                    {
                        Burrowing = true;
                    }
                    break;
                }
            }
            
            Rb.useGravity = !Burrowing;
            Rb.freezeRotation = Burrowing;
            Rb.detectCollisions = !Burrowing;
        }

        protected override void EnemyFixedUpdate()
        {
            switch (State)
            {
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                case EnemyState.MoveAndAttack:
                case EnemyState.Idle:
                {
                    if (Burrowing)
                    {
                        Rb.AddForce(Vector3.up * diveSpeed);
                        if (!Grounded)
                        {
                            Rb.AddForce(Vector3.down * (2 * diveSpeed), ForceMode.Impulse);
                            Burrowing = false;
                        }
                    }
                    
                    break;
                }
            }
        }
    }
}