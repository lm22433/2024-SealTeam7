using System;
using Enemies.Utils;
using UnityEngine;

namespace Enemies
{
    internal enum SoldierState
    {
        MovingToObjective,
        ShootingAtObjective,
        ShootingAtHands
    }
    
    public class Soldier : Enemy
    {
        [SerializeField] private float stopShootingThreshold;
        private Vector3 _movePos;
        private Quaternion _moveRot;
        private Vector3 _target;
        private SoldierState _state;
        private float _lastAttack;

        protected override void Start()
        {
            base.Start();
            
            _moveRot = transform.rotation;
            _movePos = transform.position;
            _state = SoldierState.MovingToObjective;
        }

        protected override void Attack(IDamageable target)
        {
            if (_lastAttack < attackInterval) return;
            
            target?.TakeDamage(attackDamage);
            Debug.Log($"Shot {target}!");
        }

        protected override void Update()
        {
            base.Update();

            if ((_target - transform.position).sqrMagnitude < SqrAttackRange) _state = SoldierState.ShootingAtObjective;
            else if ((_target - transform.position).sqrMagnitude > SqrAttackRange + stopShootingThreshold) _state = SoldierState.MovingToObjective;
            
            switch (_state)
            {
                case SoldierState.MovingToObjective:
                case SoldierState.ShootingAtObjective:
                {
                    _target = EnemyManager.GetObjectivePosition();
                    break;
                }
                case SoldierState.ShootingAtHands:
                {
                    throw new NotImplementedException();
                }
            }
            
            _target.y = transform.position.y;
            
            _moveRot = Quaternion.LookRotation(_target - transform.position);
            _movePos = transform.position + transform.forward * (moveSpeed * Time.deltaTime);
        }
        
        private void FixedUpdate()
        {
            switch (_state)
            {
                case SoldierState.MovingToObjective:
                {
                    Rb.Move(_movePos, _moveRot);
                    break;
                }
                case SoldierState.ShootingAtObjective:
                case SoldierState.ShootingAtHands:
                {
                    Rb.MoveRotation(_moveRot);
                    Attack(null);
                    break;
                }
            }
        }
    }
}