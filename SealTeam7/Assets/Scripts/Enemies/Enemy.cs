using System;
using Game;
using Player;
using UnityEngine;

namespace Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float attackRange;
        [SerializeField] protected float attackInterval;
        [SerializeField] protected int attackDamage;
        [SerializeField] protected int killScore;
        protected float SqrAttackRange;
        protected EnemyManager EnemyManager;
        protected Rigidbody Rb;

        protected virtual void Start()
        {
            EnemyManager = EnemyManager.GetInstance();
            Rb = GetComponent<Rigidbody>();

            SqrAttackRange = attackRange * attackRange;
        }

        protected abstract void Attack(PlayerDamageable target);

        public void Die()
        {
            GameManager.GetInstance().RegisterKill(killScore);
            Destroy(gameObject);
        }

        protected abstract void EnemyUpdate();
        protected abstract void EnemyFixedUpdate();

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;

            if ((transform.position - EnemyManager.godlyCore.transform.position).sqrMagnitude >
                EnemyManager.sqrMaxEnemyDistance)
            {
                EnemyManager.Kill(this);
            }
            
            EnemyUpdate();
        }

        private void FixedUpdate()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;

            EnemyFixedUpdate();
        }
    }
}