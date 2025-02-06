using System.Linq;
using UnityEngine;
using Weapons;

namespace Enemies.Tower
{
    public class Tower : Enemy
    {
        [SerializeField] private float attackRange;
        [SerializeField] private float attackDelay;
        [SerializeField] private float lookSpeed;
        private float _timeSinceAttack;
        private Vector3 _target;

        public override void Start()
        {
            base.Start();
            _target = transform.position;
        }
        
        public override void Attack(Collider hit)
        {
            if (_timeSinceAttack > attackDelay)
            {
                _target = hit.transform.position;
                //attackEffect.Play();
                _timeSinceAttack = 0;
                var damageable = hit.GetComponent<IDamageable>();
                damageable?.TakeDamage(damage);
            }
        }
        
        public override void Update()
        {
            base.Update();
            
            var lerpTarget = Vector3.Lerp(transform.position, _target, lookSpeed * Time.deltaTime);
            transform.LookAt(lerpTarget);
            
            _timeSinceAttack += Time.deltaTime;

            Collider[] results = new Collider[12];
            Physics.OverlapSphereNonAlloc(transform.position, attackRange, results, LayerMask.GetMask("PlayerHolder"), QueryTriggerInteraction.Collide);
            // sort by distance from tower
            Collider closestPlayer = results.OrderBy(c => c ? (transform.position - c.transform.position).sqrMagnitude : float.MaxValue).First();
            if (closestPlayer) Attack(closestPlayer);
        }
    }
}