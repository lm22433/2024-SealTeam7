using Game;
using Player;
using UnityEngine;

namespace Enemies
{
    internal enum SoldierState
    {
        Moving,
        ShootingAtCore,
        ShootingAtHands
    }
    
    public class Soldier : Enemy
    {
        [SerializeField] private float stopShootingThreshold;
        private Vector3 _moveForce;
        private Quaternion _targetRotation;
        private Vector3 _target;
        private SoldierState _state;
        private float _lastAttack;

        protected override void Start()
        {
            base.Start();
            
            _targetRotation = transform.rotation;
            _moveForce = Vector3.zero;
            _target = transform.position + transform.forward;
            _state = SoldierState.Moving;
        }

        protected override void Attack(PlayerDamageable target)
        {
            if (_lastAttack < attackInterval) return;
            
            target?.TakeDamage(attackDamage);
            _lastAttack = 0f;
        }

        private void UpdateState()
        {
            if ((EnemyManager.godlyCore.transform.position - transform.position).sqrMagnitude < SqrAttackRange) _state = SoldierState.ShootingAtCore;
            else if ((EnemyManager.godlyHands.transform.position - transform.position).sqrMagnitude < SqrAttackRange) _state = SoldierState.ShootingAtHands;
            else if ((EnemyManager.godlyCore.transform.position - transform.position).sqrMagnitude > SqrAttackRange + stopShootingThreshold) _state = SoldierState.Moving;
        }

        private void UpdateTarget()
        {
            switch (_state)
            {
                case SoldierState.Moving:
                case SoldierState.ShootingAtCore:
                {
                    _target = EnemyManager.godlyCore.transform.position;
                    break;
                }
                case SoldierState.ShootingAtHands:
                {
                    _target = EnemyManager.godlyHands.transform.position;
                    break;
                }
            }
            
            _target.y = transform.position.y;
        }

        private void LimitSpeed()
        {
            Vector3 vel = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
            // limit velocity if needed
            if (vel.magnitude > moveSpeed)
            {
                Vector3 newVel = vel.normalized * moveSpeed;
                Rb.linearVelocity = new Vector3(newVel.x, Rb.linearVelocity.y, newVel.z);
            }
        }

        protected override void EnemyUpdate()
        {
            UpdateState();
            UpdateTarget();
            LimitSpeed();
            
            _lastAttack += Time.deltaTime;
        }
        
        protected override void EnemyFixedUpdate()
        {
            _targetRotation = Quaternion.LookRotation(_target - transform.position);
            
            Rb.MoveRotation(_targetRotation);
            
            _moveForce = transform.forward * (moveSpeed * 10f);
            
            switch (_state)
            {
                case SoldierState.Moving:
                {
                    Rb.AddForce(_moveForce);
                    break;
                }
                case SoldierState.ShootingAtCore:
                {
                    Attack(EnemyManager.godlyCore);
                    break;
                }
                case SoldierState.ShootingAtHands:
                {
                    Attack(EnemyManager.godlyHands);
                    break;
                }
            }
        }
    }
}