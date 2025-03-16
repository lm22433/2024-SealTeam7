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
        [SerializeField] private AK.Wwise.Event helicopterSound;
        private bool _isGracefulShutdown = false;

        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
            helicopterSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundEffectCallback);
        }

        private void OnDestroy() {
        
            _isGracefulShutdown = true;

            helicopterSound.Stop(gameObject);
        }

        void SoundEffectCallback(object in_cookie, AkCallbackType in_type, object in_info){
            if (!_isGracefulShutdown) {
                helicopterSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, SoundEffectCallback);
            }
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
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, transform.eulerAngles.z);
            TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}