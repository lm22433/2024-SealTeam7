using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
    {
        [SerializeField] private Transform gun;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject projectile;

		protected override void Start()
		{
			base.Start();
			DeathDuration = 0.5f;
			buriedAmount = 0.25f;
		}
        
        protected override void Attack(PlayerDamageable toDamage)
        {
            Instantiate(projectile, muzzle.position, Quaternion.LookRotation(TargetPosition - muzzle.position)).TryGetComponent(out Projectile proj);
            proj.Target = TargetPosition;
            proj.ToDamage = toDamage;
            proj.Damage = attackDamage;
            
            Destroy(proj.gameObject, 2f);
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
                    var xAngle = Quaternion.LookRotation(TargetPosition - gun.position).eulerAngles.x;
                    TargetRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.Euler(Vector3.Angle(TargetPosition - gun.position, gun.right), 0f, 0f);
                    gun.rotation = Quaternion.Slerp(gun.rotation, TargetRotation, aimSpeed * Time.deltaTime);
                    break;
                }
            }
            
            TargetRotation = Quaternion.LookRotation(new Vector3(TargetPosition.x, transform.position.y, TargetPosition.z) - transform.position);
            TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        }
    }
}