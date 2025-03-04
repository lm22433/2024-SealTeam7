using Player;
using UnityEngine;

namespace Enemies
{
    public class Helicopter : Enemy
    {
        [SerializeField] float flyHeight;
        private float _lastAttack;

        private void Awake() {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {

            TargetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y, transform.rotation.eulerAngles.z);
            TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            _lastAttack += Time.deltaTime;
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }

        protected override void EnemyFixedUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                {

                    Rb.MoveRotation(TargetRotation);
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
                    if (_lastAttack < attackInterval)
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