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
            if (State is EnemyState.AttackHands)
            {
                TargetRotation = Quaternion.LookRotation(Target.transform.position - gun.position);
                gun.rotation = Quaternion.Slerp(gun.rotation, TargetRotation, aimSpeed * Time.deltaTime);
            }
            
            TargetRotation = Quaternion.LookRotation(new Vector3(TargetDirection.x, 0f, TargetDirection.z));
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if (Mathf.Abs(Vector3.Dot(transform.forward, Vector3.up)) > 0.5f) break;
                    Rb.MoveRotation(TargetRotation);
                    Rb.AddForce(TargetDirection * (moveSpeed * 10f));
                    break;
                }
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                {
                    if (_lastAttack > attackInterval)
                    {
                        Attack(State is EnemyState.AttackCore ? EnemyManager.godlyCore : EnemyManager.godlyHands);
                        _lastAttack = 0f;
                    }
                    break;
                }
            }
        }
    }
}