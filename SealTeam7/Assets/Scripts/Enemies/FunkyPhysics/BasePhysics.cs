using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public abstract class BasePhysics : MonoBehaviour
    {
        [SerializeField] protected float gravityDefiance;
        [SerializeField] protected float defianceThreshold;
        [SerializeField] protected float sinkFactor;
        [SerializeField] protected float fallDeathRequirement;
        [SerializeField] protected Enemy _self;
        protected float reasonableGroundedness = 0.6f;
        protected MapManager _mapManager;
        protected EnemyManager _enemyManager;
        protected Rigidbody _rb;
        
        protected virtual void Start()
        {
            _enemyManager = FindFirstObjectByType<EnemyManager>();
            _mapManager = FindFirstObjectByType<MapManager>();
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
        }

        protected abstract void DeathAffect(EnemyManager manager);

        protected virtual void Update()
        {
            if (transform.position.y < _mapManager.GetHeight(transform.position.x, transform.position.z) - sinkFactor)
            {
                Debug.Log("WOULD DIE BURIED");
                _enemyManager.Kill(_self);
            }

            else if (_rb.linearVelocity.y > defianceThreshold && transform.position.y < _mapManager.GetHeight(transform.position.x, transform.position.z) + reasonableGroundedness)
            {
                RaycastHit hit;
                Physics.Raycast(transform.position, Vector3.down, out hit, reasonableGroundedness * 2.0f);
                _rb.AddForce((Vector3.up + hit.normal).normalized * gravityDefiance, ForceMode.Impulse);
            }
            else if (-_rb.linearVelocity.y >= fallDeathRequirement && transform.position.y < _mapManager.GetHeight(transform.position.x, transform.position.z) + reasonableGroundedness)
            {
                Debug.Log("WOULD DIE FALL DMG");
                _enemyManager.Kill(_self);
            }
        }
        
    }
}