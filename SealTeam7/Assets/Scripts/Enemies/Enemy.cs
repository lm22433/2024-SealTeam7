using System;
using System.Linq;
using Enemies.Utils;
using Game;
using Map;
using Player;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies
{
    public enum EnemyState
    {
        Moving,
        AttackCore,
        AttackHands,
        Idle,
        Dying
    }
    
    public abstract class Enemy : MonoBehaviour
    {
        [SerializeField] protected internal EnemyType enemyType;
        
        [Header("Movement")]
        [SerializeField] protected Vector3 forceOffset;
        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float acceleration;
        [SerializeField] protected float groundedOffset;
        [SerializeField] protected float flyHeight;
        
        [Header("Attacking")]
        [SerializeField] protected float aimSpeed;
        [SerializeField] protected float attackRange;
        [SerializeField] protected float attackInterval;
        [SerializeField] protected float stopShootingThreshold;
        [SerializeField] protected float coreTargetHeightOffset;
        [SerializeField] protected int attackDamage;
        [SerializeField] protected internal int killScore;
        
        [Header("Visual Effects")]
        [SerializeField] private VisualEffect deathParticles;
        [SerializeField] private Transform model;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject projectile;

        [Header("Sound Effects")]
        [SerializeField] protected AK.Wwise.Event gunFireSound;
        [SerializeField] protected AK.Wwise.Event deathSoundEffect;
        
        protected float SqrAttackRange;
        protected EnemyManager EnemyManager;
        protected Rigidbody Rb;
        protected EnemyState State;
        protected bool DisallowMovement;
        protected bool DisallowShooting;
        protected float LastAttack;
        protected Vector3 TargetPosition;
        protected Quaternion TargetRotation;
        protected Vector3 TargetDirection;
        protected Vector3[] Path;
        protected int PathIndex;
        protected float PathFindInterval;
        protected float LastPathFind;
		protected float DeathDuration;
        protected internal bool Grounded;
        protected internal float Buried;
        protected internal float BuriedAmount = 0.5f;
        private int _handIndex;

        protected virtual void Start()
        {
            EnemyManager = EnemyManager.GetInstance();
            Rb = GetComponent<Rigidbody>();
            SqrAttackRange = attackRange * attackRange;
        }

        public virtual void Init()
        {
            model.gameObject.SetActive(true);
            deathParticles.Stop();

            State = EnemyState.Moving;
            Path = Array.Empty<Vector3>();
            TargetPosition = EnemyManager.GetInstance().godlyCore.transform.position;
            TargetRotation = Quaternion.identity;
            TargetDirection = Vector3.zero;
            PathFindInterval = EnemyManager.GetInstance().pathFindInterval;
            LastAttack = attackInterval;
            LastPathFind = PathFindInterval;
            DeathDuration = 3.0f;
        }

		public virtual void SetupDeath()
        {
            if (State == EnemyState.Dying) return;
            
            if (transform.position.y < MapManager.GetInstance().GetHeight(transform.position))
            {
                transform.position = new Vector3(
                    transform.position.x,
                    MapManager.GetInstance().GetHeight(transform.position),
                    transform.position.z);
            }
            
            model.gameObject.SetActive(false);
            deathSoundEffect.Post(gameObject);
            deathParticles.Play();
			State = EnemyState.Dying;
		}

        protected virtual void Attack(PlayerDamageable toDamage)
        {
            gunFireSound.Post(gameObject);
            
            Instantiate(projectile, muzzle.position, Quaternion.LookRotation(TargetPosition - muzzle.position), EnemyManager.transform).TryGetComponent(out Projectile proj);
            proj.TargetPosition = TargetPosition;
            proj.ToDamage = toDamage;
            proj.Damage = attackDamage;
            
            Destroy(proj.gameObject, 3f);
        }

        protected abstract float Heuristic(Node start, Node end);
        protected virtual void EnemyUpdate() {}
        protected virtual void EnemyFixedUpdate() {}
        
        private void UpdateState()
        {
			if (State is EnemyState.Dying) return;
            var coreTarget = new Vector3(
                EnemyManager.godlyCore.transform.position.x,
                flyHeight == 0 ? MapManager.GetInstance().GetHeight(EnemyManager.godlyCore.transform.position) + coreTargetHeightOffset : flyHeight,
                EnemyManager.godlyCore.transform.position.z
            );
            
            if ((coreTarget - transform.position).sqrMagnitude < SqrAttackRange && !DisallowShooting) State = EnemyState.AttackCore;
            else if ((coreTarget - transform.position).sqrMagnitude > SqrAttackRange + stopShootingThreshold && State is EnemyState.AttackCore) State = EnemyState.Moving;
            else if (State is not EnemyState.Idle) State = EnemyState.Moving;
            
            foreach (var hand in EnemyManager.godlyHands.Select((value, index) => new {value, index}))
            {
                if ((hand.value.transform.position - transform.position).sqrMagnitude < SqrAttackRange && !DisallowShooting)
                {
                    State = EnemyState.AttackHands;
                    _handIndex = hand.index;
                }
            }
        }

        private void UpdateTarget()
        {
            switch (State)
            {
                case EnemyState.Moving:
                case EnemyState.AttackCore:
                {
                    TargetPosition = new Vector3(
                        EnemyManager.godlyCore.transform.position.x,
                        flyHeight == 0 ? MapManager.GetInstance().GetHeight(EnemyManager.godlyCore.transform.position) + coreTargetHeightOffset : flyHeight,
                        EnemyManager.godlyCore.transform.position.z
                    );
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetPosition = EnemyManager.godlyHands[_handIndex].transform.position;
                    break;
                }
            }
        }

        private void LimitSpeed()
        {
            var vel = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
            // limit velocity if needed
            if (!(vel.magnitude > moveSpeed)) return;
            var newVel = vel.normalized * moveSpeed;
            Rb.linearVelocity = new Vector3(newVel.x, Rb.linearVelocity.y, newVel.z);
        }

        private void FollowPath()
        {
            Path ??= Array.Empty<Vector3>();
            if (Path.Length > 0 && PathIndex < Path.Length - 1)
            {
                if (LastPathFind >= PathFindInterval)
                {
                    LastPathFind = 0;
                    RequestPath();
                }
                var pathPosition = new Vector3(Mathf.RoundToInt(transform.position.x), Path[PathIndex].y, Mathf.RoundToInt(transform.position.z));
                if ((pathPosition - Path[PathIndex]).sqrMagnitude < 100f) PathIndex++;
                TargetDirection = Vector3.ProjectOnPlane(Path[PathIndex] - transform.position, Vector3.up).normalized;
            }
            else if (State is EnemyState.Moving)
            {
                TargetDirection = Vector3.zero;
                RequestPath();
                State = EnemyState.Idle;
            }
            
            TargetRotation = Quaternion.Euler(0f, Quaternion.LookRotation((Path.Length > 0 ? Path[PathIndex] : TargetPosition) - transform.position).eulerAngles.y, 0f);
        }

        private void RequestPath()
        {
            EnemyManager.RequestPath(transform.position, TargetPosition, Heuristic, SetPath);
        }

        private void SetPath(Vector3[] path)
        {
            Path = path;
            PathIndex = 0;
            if (State is EnemyState.Idle) State = EnemyState.Moving;
        }

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;

			if (State == EnemyState.Dying)
            {
                if (transform.position.y < MapManager.GetInstance().GetHeight(transform.position))
                {
                    transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position) - Buried, transform.position.z);
                }
				DeathDuration -= Time.deltaTime;
				if (DeathDuration <= 0.0f) EnemyManager.Kill(this);
			}

            if ((transform.position - EnemyManager.godlyCore.transform.position).sqrMagnitude > EnemyManager.sqrMaxEnemyDistance) EnemyManager.Kill(this);
            Grounded = transform.position.y < MapManager.GetInstance().GetHeight(transform.position) + groundedOffset;
            
            UpdateState();
            UpdateTarget();
            LimitSpeed();
            FollowPath();
            
            LastAttack += Time.deltaTime;
            LastPathFind += Time.deltaTime;
            
            EnemyUpdate();
        }

        private void FixedUpdate()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            if (!DisallowMovement) Rb.MoveRotation(Quaternion.Slerp(Rb.rotation, TargetRotation.normalized, aimSpeed * Time.fixedDeltaTime));
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if (!DisallowMovement) Rb.AddForceAtPosition(TargetDirection * (acceleration * 10f), Rb.worldCenterOfMass + forceOffset);
                    break;
                }
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                {
                    if (LastAttack >= attackInterval && !DisallowShooting)
                    {
                        LastAttack = 0f;
                        Attack(State is EnemyState.AttackCore ? EnemyManager.godlyCore : EnemyManager.godlyHands[_handIndex]);
                    }
                    break;
                }
            }
            
            EnemyFixedUpdate();
        }
        
        public void OnDrawGizmosSelected()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            Gizmos.color = Color.green;
            if (Path.Length > 0) Gizmos.DrawCube(Path[PathIndex], Vector3.one);
            for (int i = PathIndex + 1; i < Path.Length; i++)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(Path[i - 1], Path[i]);
                Gizmos.color = Color.red;
                Gizmos.DrawCube(Path[i], Vector3.one);
            }
        }

        public bool IsDying => State == EnemyState.Dying;
    }
}