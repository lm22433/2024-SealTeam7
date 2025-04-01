using System;
using UnityEngine;
using Map;
using Game;

namespace Enemies.FunkyPhysics
{
    public class HelicopterPhysics : BasePhysics
    {
        private bool _exploded;

        protected override void EnemyUpdate()
        {
            if (Self.Grounded && !Self.IsDying) Self.SetupDeath();
            
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

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Ground")) Self.SetupDeath();
        }
    }
}