using Game;
using Map;
using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public abstract class BasePhysics : MonoBehaviour
    {
        [SerializeField] protected float gravityDefiance;
        [SerializeField] protected float defianceThreshold;
        [SerializeField] protected float sinkFactor;
        [SerializeField] protected float groundedOffset;
        [SerializeField] protected float fallDeathVelocityY;
        protected Enemy Self;
        protected Rigidbody Rb;
        protected bool Grounded;
        
        protected virtual void Start()
        {
            Rb = GetComponent<Rigidbody>();
            Self = GetComponent<Enemy>();
        }

        private void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;

            Grounded = transform.position.y < MapManager.GetInstance().GetHeight(transform.position) + groundedOffset;
            
            //WOULD DIE BURIED
            if (transform.position.y < MapManager.GetInstance().GetHeight(transform.position) - sinkFactor)
            {
                Self.buried = Self.buriedAmount;
                EnemyManager.GetInstance().Kill(Self);
            }
            //WOULD DIE FALL DMG
            if (-Rb.linearVelocity.y >= fallDeathVelocityY && Grounded) EnemyManager.GetInstance().Kill(Self);

            EnemyUpdate();
        }

        private void FixedUpdate()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            if (Rb.linearVelocity.y > defianceThreshold && Grounded)
            {
                Physics.Raycast(transform.position, Vector3.down, out var hit, groundedOffset * 2.0f);
                Rb.AddForce((Vector3.up + hit.normal).normalized * gravityDefiance, ForceMode.Impulse);
            }
            
            EnemyFixedUpdate();
        }

        protected virtual void EnemyUpdate() {}
        protected virtual void EnemyFixedUpdate() {}
    }
}