using UnityEngine;
using Map;

namespace Enemies.FunkyPhysics
{
    public class HelicopterPhysics : BasePhysics
    {
        [SerializeField] GameObject mainPropeller;
        [SerializeField] GameObject subPropeller;
        [SerializeField] float propellerSpeed;

        protected override void Update()
        {
            base.Update();
            
            mainPropeller.transform.Rotate(new Vector3(0, propellerSpeed, 0) * Time.deltaTime);
            subPropeller.transform.Rotate(new Vector3(0, 0, propellerSpeed) * Time.deltaTime);
        }
        
        private void OnTriggerEnter(Collider collider)
        {
            if (!collider.gameObject.CompareTag($"Ground")) return;
            EnemyManager.GetInstance().Kill(Self);
        }
    }
}