using Map;
using UnityEngine;

namespace Enemies.FunkyPhysics
{
    public class TankPhysics : BasePhysics
    {
        protected override void EnemyUpdate()
        {
            if (Grounded && Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0.5f) EnemyManager.GetInstance().Kill(Self);
        }
    }
}