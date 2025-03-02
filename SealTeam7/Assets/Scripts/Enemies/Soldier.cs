using Player;
using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
    {
        [SerializeField] private Transform gun;
        private float _lastAttack;
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }

        protected override void EnemyUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                case EnemyState.AttackCore: break;
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.LookRotation(Target.transform.position - gun.position);
                    gun.rotation = Quaternion.Slerp(gun.rotation, TargetRotation, aimSpeed * Time.deltaTime);
                    break;
                }
            }
            
            //TargetRotation = Quaternion.LookRotation(new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position);
            //TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            if ((transform.position - Path[PathIndex]).sqrMagnitude < 0.2f) PathIndex++;
            TargetDirection = Path[PathIndex] - transform.position;
            TargetRotation = Quaternion.LookRotation(new Vector3(TargetDirection.x, 0f, TargetDirection.z));
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            Rb.MoveRotation(TargetRotation);
                        
            switch (State)
            {
                case EnemyState.Moving:
                {
                    Rb.AddForce(TargetDirection * (moveSpeed * 10f));
                    break;
                }
                case EnemyState.AttackCore:
                {
                    if (_lastAttack > attackInterval)
                    {
                        Attack(EnemyManager.godlyCore);
                        _lastAttack = 0f;
                    }
                    break;
                }
                case EnemyState.AttackHands:
                {
                    if (_lastAttack > attackInterval)
                    {
                        Attack(EnemyManager.godlyHands);
                        _lastAttack = 0f;
                    }
                    break;
                }
            }
        }
    }
}