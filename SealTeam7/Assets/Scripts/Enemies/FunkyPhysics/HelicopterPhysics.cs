using UnityEngine;
using Map;

namespace Enemies.FunkyPhysics
{
    public class HelicopterPhysics : BasePhysics
    {
        protected override void Start()
        {
            EnemyManager = FindFirstObjectByType<EnemyManager>();
            MapManager = FindFirstObjectByType<MapManager>();
            return;
        }

        protected override void Update()
        {
            if (transform.position.y < MapManager.GetHeight(transform.position.x, transform.position.z))
            {
                EnemyManager.Kill(self);
            }

            return;
        }
        
        private void OnTriggerEnter(Collider collider) {
            if (collider.gameObject.tag == "Ground") {
                Debug.Log("collision");
                GetComponent<Helicopter>().Die();
            }
            
        }
    }
}