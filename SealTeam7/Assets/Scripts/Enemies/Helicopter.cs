using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Helicopter : Enemy
    {
        [SerializeField] float flyHeight;

        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override void EnemyUpdate()
        {
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, transform.eulerAngles.z);
            TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}