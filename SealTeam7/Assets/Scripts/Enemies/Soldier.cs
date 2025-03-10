using Player;
using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
    {
        [SerializeField] private Transform gun;
        private float _lastAttack;
        [SerializeField] private AK.Wwise.Event gunFireSound;
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }

        protected override void EnemyUpdate()
        {
            switch (State)
            {
                case EnemyState.Moving:
                case EnemyState.AttackCore:
                {
                    TargetRotation = Quaternion.Slerp(Rb.rotation, Quaternion.LookRotation(new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position), aimSpeed * Time.deltaTime);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.identity, aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.LookRotation(Target.transform.position - gun.position);
                    gun.rotation = Quaternion.Slerp(gun.rotation, TargetRotation, aimSpeed * Time.deltaTime);
                    TargetRotation = Quaternion.Slerp(Rb.rotation, Quaternion.LookRotation(new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position), aimSpeed * Time.deltaTime);
                    break;
                }
            }
            
            TargetDirection = (Target.transform.position - transform.position + Vector3.up * (transform.position.y - Target.transform.position.y)).normalized;
            
            _lastAttack += Time.deltaTime;
        }

        protected override void EnemyFixedUpdate()
        {
            Rb.MoveRotation(Quaternion.Slerp(Rb.rotation, TargetRotation, aimSpeed * Time.fixedDeltaTime));
                        
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
                        gunFireSound.Post(gameObject);
                        Attack(EnemyManager.godlyCore);
                        _lastAttack = 0f;
                    }
                    break;
                }
                case EnemyState.AttackHands:
                {
                    if (_lastAttack > attackInterval)
                    {
                        gunFireSound.Post(gameObject);
                        Attack(EnemyManager.godlyHands);
                        _lastAttack = 0f;
                    }
                    break;
                }
            }
        }
    }
}