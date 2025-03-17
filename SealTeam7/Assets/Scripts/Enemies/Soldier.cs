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
                    TargetRotation = Quaternion.Euler(0f, Quaternion.LookRotation(Rb.linearVelocity).eulerAngles.y, 0f);
                    break;
                }
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                {
                    var xAngle = Quaternion.LookRotation(TargetPosition - gun.position).eulerAngles.x;
                    TargetRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
                    TargetRotation = Quaternion.Euler(0f, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, 0f);
                    break;
                }
            }
        }
    }
}