using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Helicopter : Enemy
    {
        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void EnemyUpdate()
        {
            if (State is EnemyState.AttackCore) TargetPosition = new Vector3(TargetPosition.x, flyHeight, TargetPosition.z);
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(Rb.linearVelocity).eulerAngles.y, transform.eulerAngles.z);
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}