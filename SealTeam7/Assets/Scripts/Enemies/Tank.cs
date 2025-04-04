using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies
{
    public class Tank : Vehicle
    {
        [SerializeField] [CanBeNull] private Transform gun;
        
        protected override void EnemyUpdate()
        {
            base.EnemyUpdate();
            
            DisallowShooting = Vector3.Dot(transform.forward, TargetPosition - transform.position) < 0.5f || !Grounded;
            
            // gun rotation
            if (gun) switch (State)
            {
                case EnemyState.Moving:
                {
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                {
                    var xAngle = Quaternion.LookRotation(TargetPosition - gun.position).eulerAngles.x - transform.eulerAngles.x;
                    var gunRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, gunRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
            }
        }
    }
}