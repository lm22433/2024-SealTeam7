using System.Collections;
using Map;
using Player;
using UnityEngine;

namespace Missiles
{
    public class Missile : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private ParticleSystem explosionParticleSystem;
        [SerializeField] private GameObject missileObject;
        
        [Header("Settings")] 
        [SerializeField] private float missileFallSpeed;
        [Header("")]
        [SerializeField] private float explosionRadius;
        [SerializeField] private int explosionDamage;
        
        private Vector3 targetPosition;
        private GameObject targetIndicator;
        private bool isFalling;
        
        public void SetTarget(Vector3 targetPosition, GameObject targetIndicator)
        {
            this.targetPosition = targetPosition;
            this.targetIndicator = targetIndicator;
            isFalling = true;
        }

        private void Update()
        {
            if (!isFalling) return;
            targetPosition.y = MapManager.GetInstance().GetHeight(targetPosition.x, targetPosition.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, missileFallSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f) Explode();
        }

        private void Explode()
        {
            isFalling = false;
            missileObject.SetActive(false);
            
            Destroy(targetIndicator);
            
            foreach (Collider hit in Physics.OverlapSphere(transform.position, explosionRadius))
            {
                if (hit.TryGetComponent(out PlayerHands damageable)) damageable.TakeDamage(explosionDamage);
            }
         
            explosionParticleSystem.Play();
            StartCoroutine(WaitForExplosion());
        }

        private IEnumerator WaitForExplosion()
        {
            yield return new WaitForSeconds(explosionParticleSystem.main.duration);
            Destroy(gameObject);
        }
    }
}