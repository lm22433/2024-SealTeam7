using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
    {
        [SerializeField] private Transform gun;

        protected override void Start()
        {
            base.Start();
            DeathDuration = 0.5f;
            buriedAmount = 0.25f;
        }

        protected override float Heuristic(Node start, Node end)
        {
            return (start.WorldPos.y - start.Parent?.WorldPos.y ?? start.WorldPos.y) * 50f;
        }

        protected override void EnemyUpdate()
        {
            // gun rotation
            switch (State)
            {
                case EnemyState.Moving:
                    {
                        gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.identity, aimSpeed * Time.deltaTime);
                        TargetRotation = Quaternion.Euler(0f, Quaternion.LookRotation((Path.Length > 0 ? Path[PathIndex] : TargetPosition) - transform.position).eulerAngles.y, 0f);
                        break;
                    }
                case EnemyState.AttackCore:
                case EnemyState.AttackHands:
                    {
                        var xAngle = Quaternion.LookRotation(TargetPosition - gun.position).eulerAngles.x;
                        TargetRotation = Quaternion.Euler(xAngle, 0f, 0f);
                        gun.localRotation = Quaternion.Slerp(gun.localRotation, TargetRotation, aimSpeed * Time.deltaTime);
                        TargetRotation = Quaternion.Euler(0f, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, 0f);
                        break;
                    }
            }
        }
    }
}