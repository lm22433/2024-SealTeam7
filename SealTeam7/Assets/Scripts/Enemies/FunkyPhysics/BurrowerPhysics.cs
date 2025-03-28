using Enemies.FunkyPhysics;
using UnityEngine;
using Map;
using Game;

namespace Enemies.FunkyPhysics
{
    public class BurrowerPhysics : BasePhysics
    {
        private bool _exploded;

        protected override void Update()
        {
            
            if (!GameManager.GetInstance().IsGameActive()) return;

            // WOULD DIE FALL DMG
            if (-Rb.linearVelocity.y >= fallDeathVelocityY && transform.position.y >= MapManager.GetInstance().GetHeight(transform.position) && Self.Grounded && !Self.IsDying)
            {
                Self.SetupDeath();
            }
            // WOULD DIE EXPOSED
            if (transform.position.y >= MapManager.GetInstance().GetHeight(transform.position) && ((Burrower) Self).Burrowing && !Self.IsDying)
            {
                Self.SetupDeath();
            }
            
            EnemyUpdate();
        }

        protected override void EnemyUpdate()
        {
            if (Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0f && Self.Grounded && !Self.IsDying)
            {
                Self.SetupDeath();
            }

            if (Self.IsDying && !_exploded)
            {
                RaycastHit[] objs = Physics.SphereCastAll(transform.position, 50.0f, transform.forward, 1.0f);
                foreach (var item in objs)
                {
                    if (item.rigidbody) item.rigidbody.AddForce((item.point-transform.position + (5.0f * Vector3.up)).normalized * 25.0f, ForceMode.Impulse);
                }
                _exploded = true;
            }
        }
    }
}