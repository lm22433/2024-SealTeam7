using Player;
using UnityEngine;

namespace Missiles
{
    public class Missile : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private GameObject indicatorPrefab;
        [SerializeField] private ParticleSystem explosionParticleSystem;
        
        [Header("Settings")] 
        [SerializeField] private float detonationTimer;
        [Header("")]
        [SerializeField] private float explosionRadius;
        [SerializeField] private int explosionDamage;

        private float elapsedTime = 0f;

        private void Start()
        {
            Invoke(nameof(Explode), detonationTimer);
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;
        }

        private void Explode()
        {
            if (explosionParticleSystem != null) explosionParticleSystem.Play(); 
            
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider hit  in hitColliders)
            {
                if (hit.TryGetComponent(out PlayerDamageable damageable))
                {
                    damageable.TakeDamage(explosionDamage);
                }
            }
            
            Debug.Log("Big Boom!");
            
            Destroy(gameObject);
        }
    }
}