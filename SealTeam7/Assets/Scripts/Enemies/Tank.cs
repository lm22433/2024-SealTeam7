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
            TargetRotation = Quaternion.Euler(Vector3.Angle(Target - gun.position, Vector3.right), 0f, 0f);
            gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
            
            //TargetRotation = Quaternion.Euler(0f, Mathf.Atan2(Target.z - turret.position.z, Target.x - turret.position.x) * Mathf.Rad2Deg, 0f);
            TargetRotation = Quaternion.Euler(0f, Quaternion.LookRotation(Target - turret.position).eulerAngles.y - transform.rotation.eulerAngles.y, 0f);
            turret.localRotation = Quaternion.Slerp(turret.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
            
            TargetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(Target - transform.position).eulerAngles.y, transform.rotation.eulerAngles.z);
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
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