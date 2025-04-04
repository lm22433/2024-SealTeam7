using Game;
using Map;
using Enemies.Utils;
using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public abstract class BasePhysics : MonoBehaviour
    {
        [SerializeField] protected float gravityDefiance;
        [SerializeField] protected float defianceThreshold;
        [SerializeField] protected float sinkFactor;
        [SerializeField] protected float fallDeathVelocityY;
        [SerializeField] protected float jumpForce = 10f;
        [SerializeField] protected float laplaceLocation = 0.0f;
        [SerializeField] protected float laplaceScale = 2.0f;
        [SerializeField] protected float yeetThreshold = 0.8f;
        protected Enemy Self;
        protected Rigidbody Rb;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Self = GetComponent<Enemy>();
        }
        
        public virtual void Init() {}

        protected virtual void Update()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            //WOULD DIE BURIED
            if (transform.position.y <= MapManager.GetInstance().GetHeight(transform.position) - sinkFactor && !Self.IsDying)
            {
                float flyXx = LaplaceDistribution.Sample(laplaceLocation, laplaceScale);
                if (-yeetThreshold < flyXx && flyXx < yeetThreshold)
                {
                    transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position) + 1.0f, transform.position.z);
                    float flyXz = LaplaceDistribution.Sample(laplaceLocation, laplaceScale);
                    float flyYx = LaplaceDistribution.ProbabilityDensity(flyXx, laplaceLocation, laplaceScale);
                    float flyYz = LaplaceDistribution.ProbabilityDensity(flyYx, laplaceLocation, laplaceScale);
                    Vector3 flyVectorX = new Vector3(flyXx, flyYx, 0f);
                    Vector3 flyVectorZ = new Vector3(0f, flyYz, flyXz);
                    Vector3 velocity = flyVectorX + flyVectorZ;
                    velocity.y = velocity.y / 2.0f;
                    velocity = velocity.normalized;
                    Rb.linearVelocity = Vector3.zero;
                    Rb.AddForce(sinkFactor * jumpForce * velocity, ForceMode.Impulse);
                }
                else
                {
                    Self.Buried = Self.BuriedAmount;
                    Self.SetupDeath();
                }
            }
            //WOULD DIE FALL DMG
            if (-Rb.linearVelocity.y >= fallDeathVelocityY && Self.Grounded && !Self.IsDying) Self.SetupDeath();

            EnemyUpdate();
        }

        private void FixedUpdate()
        {
            if (!GameManager.GetInstance().IsGameActive()) return;
            
            // if (Rb.linearVelocity.y > defianceThreshold && Self.Grounded)
            // {
            //     Physics.Raycast(transform.position, Vector3.down, out var hit, Self.transform.localScale.y * 2.0f);
            //     Rb.AddForce((Vector3.up + hit.normal).normalized * gravityDefiance, ForceMode.Impulse);
            // }
            
            EnemyFixedUpdate();
        }

        protected virtual void EnemyUpdate() {}
        protected virtual void EnemyFixedUpdate() {}
    }
}