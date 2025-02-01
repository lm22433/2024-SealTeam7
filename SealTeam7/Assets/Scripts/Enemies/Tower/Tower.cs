using UnityEngine;
using Weapons;

namespace Enemies.Tower
{
    public class Tower : Enemy
    {
        [SerializeField] private float attackRange;
        [SerializeField] private float attackDelay;
        private float _timeSinceAttack;
        
        public override void Attack(Collider hit)
        {
            if (_timeSinceAttack > attackDelay)
            {
                //attackEffect.Play();
                _timeSinceAttack = 0;
                var damageable = hit.GetComponent<IDamageable>();
                damageable?.TakeDamage(damage);
            }
        }
        
        public override void Update()
        {
            base.Update();

            _timeSinceAttack += Time.deltaTime;

            Collider[] results = new Collider[12];
            Physics.OverlapSphereNonAlloc(transform.position, attackRange, results, LayerMask.GetMask("PlayerHolder"), QueryTriggerInteraction.Collide);

            foreach (Collider hit in results)
            {
                if (hit)
                {
                    Attack(hit);                   
                }
            }
        }
    }
}