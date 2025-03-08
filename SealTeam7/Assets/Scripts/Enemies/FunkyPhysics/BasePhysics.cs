using System;
using Game;
using Map;
using UnityEngine;
using UnityEngine.Serialization;

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
        
        protected virtual void Start()
        {
            Rb = GetComponent<Rigidbody>();
            Self = GetComponent<Enemy>();
            Rb.freezeRotation = true;
        }

        protected virtual void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            if (transform.position.y < MapManager.GetInstance().GetInterpolatedHeight(transform.position) - sinkFactor)
            {
                //WOULD DIE BURIED
                EnemyManager.GetInstance().Kill(Self);
            }
            else if (Rb.linearVelocity.y > defianceThreshold && transform.position.y < MapManager.GetInstance().GetInterpolatedHeight(transform.position) + groundedOffset)
            {
                Physics.Raycast(transform.position, Vector3.down, out var hit, groundedOffset * 2.0f);
                Rb.AddForce((Vector3.up + hit.normal).normalized * gravityDefiance, ForceMode.Impulse);
            }
            else if (-Rb.linearVelocity.y >= fallDeathVelocityY && transform.position.y < MapManager.GetInstance().GetInterpolatedHeight(transform.position) + groundedOffset)
            {
                //WOULD DIE FALL DMG
                EnemyManager.GetInstance().Kill(Self);
            }
        }
    }
}