using Game;
using Player;
using UnityEngine;

namespace Enemies
{
    public enum EnemyState
    {
        Moving,
        AttackCore,
        AttackHands
    }
    
    public abstract class Enemy : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float acceleration;
        [SerializeField] protected float aimSpeed;
        [SerializeField] protected float attackRange;
        [SerializeField] protected float attackInterval;
        [SerializeField] protected float stopShootingThreshold;
        [SerializeField] protected int attackDamage;
        [SerializeField] protected int killScore;
        protected float SqrAttackRange;
        protected EnemyManager EnemyManager;
        protected Rigidbody Rb; 
        protected EnemyState State;
        protected bool DisallowMovement;
        protected bool DisallowShooting;
        protected PlayerDamageable Target;
        protected Quaternion TargetRotation;
        protected Vector3 TargetDirection;
        protected float LastAttack;

        protected virtual void Start()
        {
            EnemyManager = EnemyManager.GetInstance();
            Rb = GetComponent<Rigidbody>();

            SqrAttackRange = attackRange * attackRange;
            State = EnemyState.Moving;
            // Target = transform.position + transform.forward;
            Target = EnemyManager.godlyCore;
            TargetRotation = transform.rotation;
            TargetDirection = transform.forward;
        }

        public void Die()
        {
            GameManager.GetInstance().RegisterKill(killScore);
            Destroy(gameObject);
        }

        protected abstract void Attack(PlayerDamageable target);
        protected virtual void EnemyUpdate() {}
        protected virtual void EnemyFixedUpdate() {}
        
        private void UpdateState()
        {
            var coreTarget = new Vector3(EnemyManager.godlyCore.transform.position.x, transform.position.y, EnemyManager.godlyCore.transform.position.z);
            if ((coreTarget - transform.position).sqrMagnitude < SqrAttackRange && !DisallowShooting) State = EnemyState.AttackCore;
            else if ((EnemyManager.godlyHands.transform.position - transform.position).sqrMagnitude < SqrAttackRange && !DisallowShooting) State = EnemyState.AttackHands;
            else if ((coreTarget - transform.position).sqrMagnitude > SqrAttackRange + stopShootingThreshold) State = EnemyState.Moving;
            else State = EnemyState.Moving;
        }

        private void UpdateTarget()
        {
            switch (State)
            {
                case EnemyState.Moving:
                case EnemyState.AttackCore:
                {
                    Target = EnemyManager.godlyCore;
                    break;
                }
                case EnemyState.AttackHands:
                {
                    Target = EnemyManager.godlyHands;
                    break;
                }
            }
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

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;

            if ((transform.position - EnemyManager.godlyCore.transform.position).sqrMagnitude >
                EnemyManager.sqrMaxEnemyDistance)
            {
                EnemyManager.Kill(this);
            }
            
            UpdateState();
            UpdateTarget();
            LimitSpeed();
            
            LastAttack += Time.deltaTime;
            
            EnemyUpdate();
        }

        private void FixedUpdate()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            if (!DisallowMovement) Rb.MoveRotation(Quaternion.Slerp(Rb.rotation, TargetRotation, aimSpeed * Time.fixedDeltaTime));
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if (!DisallowMovement) Rb.AddForce(TargetDirection * (acceleration * 10f));
                    break;
                }
                case EnemyState.AttackCore:
                {
                    if (LastAttack > attackInterval && !DisallowShooting)
                    {
                        Attack(EnemyManager.godlyCore);
                        LastAttack = 0f;
                    }
                    break;
                }
                case EnemyState.AttackHands:
                {
                    if (LastAttack > attackInterval && !DisallowShooting)
                    {
                        Attack(EnemyManager.godlyHands);
                        LastAttack = 0f;
                    }
                    break;
                }
            }
            
            EnemyFixedUpdate();
        }
    }
}