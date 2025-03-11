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
        [SerializeField] protected Enemy self;
        protected MapManager MapManager;
        protected EnemyManager EnemyManager;
        protected Rigidbody Rb;
        
        protected virtual void Start()
        {
            EnemyManager = FindFirstObjectByType<EnemyManager>();
            MapManager = FindFirstObjectByType<MapManager>();
            Rb = GetComponent<Rigidbody>();
            Rb.freezeRotation = true;
        }

        protected virtual void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            if (transform.position.y < MapManager.GetHeight(transform.position.x, transform.position.z) - sinkFactor)
            {
                //WOULD DIE BURIED
                EnemyManager.Kill(self);
            }
            else if (Rb.linearVelocity.y > defianceThreshold && transform.position.y < MapManager.GetHeight(transform.position.x, transform.position.z) + groundedOffset)
            {
                Physics.Raycast(transform.position, Vector3.down, out var hit, groundedOffset * 2.0f);
                Rb.AddForce((Vector3.up + hit.normal).normalized * gravityDefiance, ForceMode.Impulse);
            }
            else if (-Rb.linearVelocity.y >= fallDeathVelocityY && transform.position.y < MapManager.GetHeight(transform.position.x, transform.position.z) + groundedOffset)
            {
                //WOULD DIE FALL DMG
                EnemyManager.Kill(self);
            }
        }
    }
}