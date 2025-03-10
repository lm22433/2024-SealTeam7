using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public class HelicopterPhysics : BasePhysics
    {
        [SerializeField] private Transform mainPropeller;
        [SerializeField] private Transform subPropeller;
        [SerializeField] private float propellerSpeed;

        protected override void Update()
        {
            base.Update();
            mainPropeller.Rotate(Vector3.forward * (propellerSpeed * Time.deltaTime)); // Kind of fucked. Jank Blender. Don't touch.
            subPropeller.Rotate(Vector3.forward * (propellerSpeed * Time.deltaTime));
        }
        
        private void OnTriggerEnter(Collider collider)
        {
            if (!collider.gameObject.CompareTag($"Ground")) return;
            EnemyManager.GetInstance().Kill(Self);
        }
    }
}