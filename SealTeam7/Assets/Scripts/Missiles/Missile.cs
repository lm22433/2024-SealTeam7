using Player;
using UnityEngine;

namespace Missiles
{
    public class Missile : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private ParticleSystem explosionParticleSystem;
        
        [Header("Settings")] 
        [SerializeField] private float missileFallSpeed;
        [Header("")]
        [SerializeField] private float explosionRadius;
        [SerializeField] private int explosionDamage;
        
        private Vector3 targetPosition;
        private GameObject targetIndicator;
        private bool isFalling = false;
        
        public void SetTarget(Vector3 targetPosition, GameObject targetIndicator)
        {
            this.targetPosition = targetPosition;
            this.targetIndicator = targetIndicator;
            isFalling = true;
        }

        private void Update()
        {
            if (!isFalling) return;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, missileFallSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f) Explode();
        }

        private void Explode()
        {
            Destroy(targetIndicator);
            explosionParticleSystem.Play(); 
            
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider hit  in hitColliders)
            {
                if (hit.TryGetComponent(out PlayerHands damageable))
                {
                    damageable.TakeDamage(explosionDamage);
                }
            }
            
            Debug.Log("Big Boom!");
            
            Destroy(gameObject);
        }
    }
}