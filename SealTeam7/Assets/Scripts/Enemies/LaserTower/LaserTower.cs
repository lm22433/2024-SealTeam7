using UnityEngine;

namespace Enemies.LaserTower
{
    public class LaserTower : Enemy
    {
        [SerializeField] private float attackRange;
        [SerializeField] private float attackDelay;
        [SerializeField] private float damageModifierPerSecond;
        [SerializeField] private GameObject laser;
        private float _timeSinceAttack;
        private float _attackDuration;
        private float _damageModifier;
        private bool _attacking; 
        
        public override void Attack()
        {
            if (_timeSinceAttack > attackDelay)
            {
                Debug.Log($"Dealt {damage * _damageModifier} damage!");
                _timeSinceAttack = 0;
            }
        }
        
        public override void Update()
        {
            base.Update();
            
            _timeSinceAttack += Time.deltaTime;
            
            if (_attacking) _attackDuration += Time.deltaTime;
            else _attackDuration = 0f;
            
            _damageModifier = Mathf.Pow(damageModifierPerSecond, Mathf.Floor(_attackDuration));
            
            if ((player.transform.position - transform.position).sqrMagnitude < attackRange * attackRange)
            {
                _attacking = true;
                Attack();
            }
            else _attacking = false;
            
            laser.SetActive(_attacking);
        }
    }
}