using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Helicopter : Enemy
    {
        [SerializeField] protected float flyHeight;
        
        private void Awake()
        {
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override float Heuristic(Node start, Node end)
        {
            return end.WorldPos.y > flyHeight ? 5000f : 0f;
        }
        
        protected override void EnemyUpdate()
        {
            switch (State)
            {
                case EnemyState.AttackCore:
                {
                    TargetPosition = new Vector3(TargetPosition.x, flyHeight, TargetPosition.z);
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x,
                        Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y,
                        transform.eulerAngles.z);
                    break;
                }
                case EnemyState.AttackHands:
                {
                    TargetRotation = Quaternion.Euler(
                        transform.eulerAngles.x,
                        Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y,
                        transform.eulerAngles.z);
                    break;
                }
                case EnemyState.Moving:
                {
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation((Path.Length > 0 ? Path[PathIndex] : TargetPosition) - transform.position).eulerAngles.y, transform.eulerAngles.z);
                    break;
                }
            }
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}