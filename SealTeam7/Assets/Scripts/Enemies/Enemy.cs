using Enemies.Utils;
using UnityEngine;

namespace Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float attackRange;
        [SerializeField] protected float attackInterval;
        [SerializeField] protected float attackDamage;
        protected float SqrAttackRange;
        protected EnemyManager EnemyManager;
        protected Rigidbody Rb;
        
        protected virtual void Start()
        {
            EnemyManager = FindFirstObjectByType<EnemyManager>();
            Rb = GetComponent<Rigidbody>();
            
            SqrAttackRange = attackRange * attackRange;
        }

        protected abstract void Attack(IDamageable target);

        protected virtual void Update()
        {
            if ((transform.position - EnemyManager.GetObjectivePosition()).sqrMagnitude > EnemyManager.sqrMaxEnemyDistance)
            {
                EnemyManager.Kill(this);
            }
        }
    }
}