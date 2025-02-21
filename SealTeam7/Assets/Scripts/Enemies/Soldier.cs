using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
    {
        public override void Update()
        {
            base.Update();

            var objective = EnemyManager.GetObjective();
            objective.y = transform.position.y;
            transform.LookAt(objective);
            if ((objective - transform.position).sqrMagnitude > attackRange * attackRange)
            {
                transform.Translate(Vector3.forward * (moveSpeed * Time.deltaTime));
            }
        }
    }
}