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
            _target = transform.position;
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

        protected override void Update()
        {
            base.Update();

            UpdateState();
            
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
            
            _moveRot = Quaternion.LookRotation(_target - transform.position);
            _movePos = transform.position + transform.forward * (moveSpeed * Time.deltaTime);

            _lastAttack += Time.deltaTime;
        }
        
        private void FixedUpdate()
        {
            switch (_state)
            {
                case SoldierState.Moving:
                {
                    Rb.Move(_movePos, _moveRot);
                    break;
                }
                case SoldierState.ShootingAtCore:
                {
                    Rb.MoveRotation(_moveRot);
                    Attack(EnemyManager.godlyCore);
                    break;
                }
                case SoldierState.ShootingAtHands:
                {
                    Rb.MoveRotation(_moveRot);
                    Attack(EnemyManager.godlyHands);
                    break;
                }
            }
        }
    }
}