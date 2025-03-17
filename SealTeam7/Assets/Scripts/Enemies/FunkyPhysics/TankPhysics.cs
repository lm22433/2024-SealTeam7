using Map;
using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public class TankPhysics : BasePhysics
    {
		private bool _exploded;

        protected override void EnemyUpdate()
        {
            if (Grounded && Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0.5f) EnemyManager.GetInstance().Kill(Self);

			if (Self.IsDying() && !_exploded)
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