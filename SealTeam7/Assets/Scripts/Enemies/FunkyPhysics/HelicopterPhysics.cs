using UnityEngine;
using Map;

namespace Enemies.FunkyPhysics
{
    public class HelicopterPhysics : BasePhysics
    {
        [SerializeField] private Transform mainPropeller;
        [SerializeField] private Transform subPropeller;
        [SerializeField] private float propellerSpeed;

        protected override void Start()
        {
            EnemyManager = FindFirstObjectByType<EnemyManager>();
            MapManager = FindFirstObjectByType<MapManager>();
        }

        protected override void Update()
        {   
            mainPropeller.Rotate(Vector3.forward * (propellerSpeed * Time.deltaTime)); // Kind of fucked. Jank Blender. Don't touch.
            subPropeller.Rotate(Vector3.forward * (propellerSpeed * Time.deltaTime));

            if (transform.position.y < MapManager.GetHeight(transform.position.x, transform.position.z))
            {
                EnemyManager.Kill(self);
            }
        }
        
        private void OnTriggerEnter(Collider collider) {
            if (collider.gameObject.tag == "Ground") {
                GetComponent<Helicopter>().Die();
            }
            
        }
    }
}