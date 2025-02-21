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
        
        private void Start()
        {
            MapManager = FindFirstObjectByType<MapManager>();
            EnemyManager = FindFirstObjectByType<EnemyManager>();
        }
        
        public virtual void Update()
        {
            var x = transform.position.x;
            var z = transform.position.z;
        
            // sit on terrain
            transform.SetPositionAndRotation(new Vector3(x, MapManager.GetHeight(x, z) + transform.lossyScale.y, z), transform.rotation);
        }
    }
}