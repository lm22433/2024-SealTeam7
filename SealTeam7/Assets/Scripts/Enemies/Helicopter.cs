using Player;
using UnityEngine;

namespace Enemies
{
    public class Helicopter : Enemy
    {
        [SerializeField] float flyHeight;

        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void Attack(PlayerDamageable target)
        {
            target?.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            TargetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, Quaternion.LookRotation(Target.transform.position - transform.position).eulerAngles.y, transform.rotation.eulerAngles.z);
            TargetDirection = (new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z) - transform.position).normalized;
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}