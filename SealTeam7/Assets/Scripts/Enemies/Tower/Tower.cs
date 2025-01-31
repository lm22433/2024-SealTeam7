using UnityEngine;
using Weapons;

namespace Enemies.Tower
{
    public class Tower : Enemy
    {
        [SerializeField] private float attackRange;
        [SerializeField] private float attackDelay;
        private float _timeSinceAttack;
        
        public override void Attack()
        {
            if (_timeSinceAttack > attackDelay)
            {
                _timeSinceAttack = 0;
                Player.TakeDamage(damage);
            }
        }
        
        public override void Update()
        {
            base.Update();

            _timeSinceAttack += Time.deltaTime;

            // Only needed if enemy is owned by server
            /*
            Collider[] results = new Collider[12];
            Physics.OverlapSphereNonAlloc(transform.position, attackRange, results, LayerMask.GetMask("PlayerHolder"), QueryTriggerInteraction.Collide);

            foreach (Collider hit in results)
            {
                Attack(hit);
            }
            */

            if ((PlayerObject.transform.position - transform.position).sqrMagnitude < attackRange * attackRange)
            {
                Attack();   
            }
        }
    }
}