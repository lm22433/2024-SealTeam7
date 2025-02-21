using Map;
using UnityEngine;

namespace Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float attackRange;
        protected MapManager MapManager;
        protected EnemyManager EnemyManager;
        protected Rigidbody Rb;
        
        private void Start()
        {
            MapManager = FindFirstObjectByType<MapManager>();
            EnemyManager = FindFirstObjectByType<EnemyManager>();
            Rb = GetComponent<Rigidbody>();
        }
        
        public virtual void Update()
        {
            
        }
    }
}