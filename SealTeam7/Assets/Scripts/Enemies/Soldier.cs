using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
    {
        private void FixedUpdate()
        {
            var objective = EnemyManager.GetObjective();
            objective.y = transform.position.y;
            
            Rb.MoveRotation(Quaternion.LookRotation(objective - transform.position));
            
            if ((objective - transform.position).sqrMagnitude > attackRange * attackRange)
            {
                Rb.MovePosition(transform.position + transform.forward * (moveSpeed * Time.deltaTime));
            }
        }
    }
}