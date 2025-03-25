using UnityEngine;
using Map;
using Game;

namespace Enemies.FunkyPhysics
{
    public class HelicopterPhysics : BasePhysics
    {
        [SerializeField] private Transform mainPropeller;
        [SerializeField] private Transform subPropeller;
        [SerializeField] private float propellerSpeed;
        private bool _exploded;

        protected override void EnemyUpdate()
        {
            if (Grounded && !Self.IsDying) EnemyManager.GetInstance().Kill(Self);

            mainPropeller.Rotate(Vector3.forward * (propellerSpeed * Time.deltaTime)); // Kind of fucked. Jank Blender. Don't touch.
            subPropeller.Rotate(Vector3.forward * (propellerSpeed * Time.deltaTime));

            if (Self.IsDying && !_exploded)
            {
                RaycastHit[] objs = Physics.SphereCastAll(transform.position, 50.0f, transform.forward, 1.0f);
                foreach (var item in objs)
                {
                    if (item.rigidbody) item.rigidbody.AddForce((item.point - transform.position + 5.0f * Vector3.up).normalized * 25.0f, ForceMode.Impulse);
                }
                _exploded = true;
            }
        }
    }
}