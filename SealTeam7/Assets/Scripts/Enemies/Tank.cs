using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Tank : Enemy
    {
        [SerializeField] private Transform gun;
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            // gun rotation
            switch (State)
            {
                case EnemyState.Moving:
                {
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackCore:
                {
                    TargetRotation = Quaternion.Euler(Vector3.Angle(Target.transform.position + Vector3.up * (MapManager.GetInstance().GetHeight(Target.transform.position) - Target.transform.position.y) - gun.position, gun.right), 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.Euler(Vector3.Angle(Target.transform.position - gun.position, gun.right), 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
            }
            
            TargetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y, transform.rotation.eulerAngles.z);
            TargetDirection = (new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position).normalized;
        }

        protected override void EnemyFixedUpdate()
        {
            DisallowMovement = Vector3.Dot(transform.up, Vector3.up) < 0.5f;
        }
    }
}