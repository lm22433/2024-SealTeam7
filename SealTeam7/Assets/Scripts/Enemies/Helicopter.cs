using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Helicopter : Enemy
    {
        [SerializeField] float flyHeight;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject projectile;

        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void Attack(PlayerDamageable toDamage)
        {
            var target = new Vector3(TargetPosition.x, transform.position.y, TargetPosition.z);
            Instantiate(projectile, muzzle.position, Quaternion.LookRotation(target - muzzle.position)).TryGetComponent(out Projectile proj);
            proj.Target = new Vector3(TargetPosition.x, transform.position.y, TargetPosition.z);
            proj.ToDamage = toDamage;
            proj.Damage = attackDamage;
            
            Destroy(proj.gameObject, 2f);
        }
        
        protected override void EnemyUpdate()
        {
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetDirection).eulerAngles.y, transform.eulerAngles.z);
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}