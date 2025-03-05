using UnityEngine;
using Map;
using Game;

namespace Enemies.FunkyPhysics
{
    public class HelicopterPhysics : BasePhysics
    {
        [SerializeField] GameObject mainPropeller;
        [SerializeField] GameObject subPropeller;
        [SerializeField] float propellerSpeed;
        private bool _exploded = false;


        protected override void Start()
        {
            EnemyManager = FindFirstObjectByType<EnemyManager>();
            MapManager = FindFirstObjectByType<MapManager>();
            return;
        }

        protected override void Update()
        {  
            if (!GameManager.GetInstance().IsGameActive()) return;

            mainPropeller.transform.Rotate(new Vector3(0, propellerSpeed, 0) * Time.deltaTime);
            subPropeller.transform.Rotate(new Vector3(0, 0, propellerSpeed) * Time.deltaTime);

            if (transform.position.y < MapManager.GetHeight(transform.position.x, transform.position.z))
            {
                EnemyManager.Kill(self);
            }

            if(self.IsDying() && !_exploded) {
				RaycastHit[] objs = Physics.SphereCastAll(transform.position, 50.0f, transform.forward, 1.0f);
                foreach (var item in objs)
                {
                    if(item.rigidbody != null)item.rigidbody.AddForce((item.point-transform.position + (5.0f * Vector3.up)).normalized * 25.0f, ForceMode.Impulse);
                }
                _exploded = true;
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