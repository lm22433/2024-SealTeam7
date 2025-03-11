using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
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
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.identity, aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackCore:
                {
                    var xAngle = Quaternion.LookRotation(new Vector3(
                            Target.transform.position.x,
                            MapManager.GetInstance().GetHeight(Target.transform.position),
                            Target.transform.position.z
                        ) - gun.position)
                        .eulerAngles.x;
                    TargetRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.Euler(Vector3.Angle(Target.transform.position - gun.position, gun.right), 0f, 0f);
                    gun.rotation = Quaternion.Slerp(gun.rotation, TargetRotation, aimSpeed * Time.deltaTime);
                    break;
                }
            }
            
            TargetRotation = Quaternion.LookRotation(new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position);
            // TargetDirection = (new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position).normalized;
            TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        }
    }
}