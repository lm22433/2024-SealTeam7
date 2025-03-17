using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Tank : Enemy
    {
        [SerializeField] private Transform gun;
        [SerializeField] private ParticleSystem[] dustTrails;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject projectile;
        [SerializeField] protected float groundedOffset;

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
            DisallowMovement = Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0.8f;
            DisallowShooting = Vector3.Dot(transform.forward, TargetPosition - transform.position) < 0.8f;
            
            // gun rotation
            switch (State)
            {
                case EnemyState.Moving:
                {
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    if (DisallowMovement || Rb.position.y > MapManager.GetInstance().GetHeight(transform.position) + groundedOffset)
                    {
                        foreach (var dustTrail in dustTrails)
                            if (dustTrail.isPlaying) dustTrail.Stop();
                    }
                    else
                    {
                        foreach (var dustTrail in dustTrails)
                            if (!dustTrail.isPlaying) dustTrail.Play();
                    }
                    break;
                }
                case EnemyState.AttackCore:
                {
                    var xAngle = Quaternion.LookRotation(TargetPosition - gun.position).eulerAngles.x - transform.eulerAngles.x;
                    TargetRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.Euler(Vector3.Angle(TargetPosition - gun.position, gun.right), 0f, 0f);
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    break;
                }
                case EnemyState.Dying:
                {
                    foreach (var dustTrail in dustTrails) dustTrail.Stop();
                    break;
                }
            }
            
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(Rb.linearVelocity).eulerAngles.y, transform.eulerAngles.z);
        }
    }
}