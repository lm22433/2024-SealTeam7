using Game;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public enum EnemyState
    {
        Moving,
        AttackCore,
        AttackHands,
        Dying
    }

    public enum Target
    {
        Core,
        Hands
    }
    
    public abstract class Enemy : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float aimSpeed;
        [SerializeField] protected float attackRange;
        [SerializeField] protected float attackInterval;
        [SerializeField] protected float stopShootingThreshold;
        [SerializeField] protected int attackDamage;
        [SerializeField] protected int killScore;
        protected MapManager _mapManager;
        protected float SqrAttackRange;
        protected EnemyManager EnemyManager;
        protected Rigidbody Rb;
        protected EnemyState State;
        protected PlayerDamageable Target;
        protected Quaternion TargetRotation;
        protected Vector3 TargetDirection;
		protected float deathDuration = 2.0f;
		public float buried = 0.0f;
		public float buriedAmount = 0.5f;

        protected virtual void Start()
        {
            EnemyManager = EnemyManager.GetInstance();
			_mapManager = FindFirstObjectByType<MapManager>();
            Rb = GetComponent<Rigidbody>();

            SqrAttackRange = attackRange * attackRange;
            State = EnemyState.Moving;
            // Target = transform.position + transform.forward;
            Target = EnemyManager.godlyCore;
            TargetRotation = transform.rotation;
            TargetDirection = transform.forward;
        }

        public virtual void Die()
        {
            GameManager.GetInstance().RegisterKill(killScore);
            Destroy(gameObject);
        }

		public virtual void SetupDeath()
		{
			State = EnemyState.Dying;
		}

        protected abstract void Attack(PlayerDamageable target);
        protected virtual void EnemyUpdate() {}
        protected virtual void EnemyFixedUpdate() {}
        
        private void UpdateState()
        {
			if(State == EnemyState.Dying) return;
            var coreTarget = new Vector3(EnemyManager.godlyCore.transform.position.x, transform.position.y, EnemyManager.godlyCore.transform.position.z);
            if ((coreTarget - transform.position).sqrMagnitude < SqrAttackRange) State = EnemyState.AttackCore;
            else if ((EnemyManager.godlyHands.transform.position - transform.position).sqrMagnitude < SqrAttackRange) State = EnemyState.AttackHands;
            else if ((coreTarget - transform.position).sqrMagnitude > SqrAttackRange + stopShootingThreshold) State = EnemyState.Moving;
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

			if (State == EnemyState.Dying) {
				
				deathDuration -= Time.deltaTime;
				if (deathDuration <= 0.0f) this.Die();
			}

            if ((transform.position - EnemyManager.godlyCore.transform.position).sqrMagnitude >
                EnemyManager.sqrMaxEnemyDistance)
            {
                EnemyManager.Kill(this);
            }
            
            UpdateState();
            UpdateTarget();
            LimitSpeed();
            
            EnemyUpdate();

        }

        private void FixedUpdate()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            EnemyFixedUpdate();
        }

		public bool IsDying()
		{
			return State == EnemyState.Dying;
		}
    }
}