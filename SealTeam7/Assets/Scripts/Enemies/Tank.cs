using Player;
using UnityEngine;

namespace Enemies
{
    public class Tank : Enemy
    {
        [SerializeField] private Transform gun;
        private float _lastAttack;
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            if (State == EnemyState.AttackHands)
            {
                // gun rotation
                TargetRotation = Quaternion.Euler(Vector3.Angle(Target.transform.position - gun.position, gun.right), 0f, 0f);
                gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);   
            }
            else
            {
                gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
            }
            
            TargetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y, transform.rotation.eulerAngles.z);
            TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                {
                    // if upside-down / pointing upwards
                    if (Vector3.Dot(transform.up, Vector3.up) < 0.5f) break;
                    Rb.MoveRotation(Quaternion.Slerp(Rb.rotation, TargetRotation, aimSpeed * Time.fixedDeltaTime));
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