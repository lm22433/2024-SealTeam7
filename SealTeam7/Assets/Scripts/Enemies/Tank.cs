using Player;
using UnityEngine;

namespace Enemies
{
    public class Tank : Enemy
    {
        [SerializeField] private Transform turret;
        [SerializeField] private Transform gun;
        private float _lastAttack;
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            TargetRotation = Quaternion.Euler(Vector3.Angle(Target - gun.position, turret.forward), 0f, 0f);
            gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
            
            TargetRotation = Quaternion.Euler(0f, Mathf.Atan2(Target.x - turret.position.x, Target.z - turret.position.z) * Mathf.Rad2Deg, 0f);
            turret.localRotation = Quaternion.Slerp(turret.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
            
            TargetRotation = Quaternion.Euler(0f, Vector3.Angle(transform.forward, new Vector3(Target.x, transform.position.y, Target.z) - transform.position), 0f);
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            TargetRotation = Quaternion.LookRotation(Target - transform.position);
                        
            switch (State)
            {
                case EnemyState.Moving:
                {
                    Rb.MoveRotation(TargetRotation);
                    Rb.AddForce(transform.forward * (moveSpeed * 10f));
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