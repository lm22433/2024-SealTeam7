using UnityEngine;
using Map;

namespace Enemies.FunkyPhysics
{
    public class HelicopterPhysics : BasePhysics
    {
        [SerializeField] GameObject mainPropeller;
        [SerializeField] GameObject subPropeller;
        [SerializeField] float propellerSpeed;

        protected override void Start()
        {
            EnemyManager = FindFirstObjectByType<EnemyManager>();
            MapManager = FindFirstObjectByType<MapManager>();
            return;
        }

        protected override void Update()
        {   
            mainPropeller.transform.Rotate(new Vector3(0, propellerSpeed, 0) * Time.deltaTime);
            subPropeller.transform.Rotate(new Vector3(0, 0, propellerSpeed) * Time.deltaTime);

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