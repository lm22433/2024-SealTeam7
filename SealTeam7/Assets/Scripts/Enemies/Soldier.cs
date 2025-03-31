using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Soldier : Enemy
    {
        [SerializeField] private Transform gun;
        [SerializeField] private float cameraSpwanChance = 0.01f;
        [SerializeField] private GameObject cameraHolder;
        
        public override void Init() 
        {
            base.Init();
            DeathDuration = 0.5f;
            BuriedAmount = 0.25f;

            if (cameraHolder != null) {
                float probability = UnityEngine.Random.Range(0, 1000);
                probability = probability / 100;

                if (probability <= cameraSpwanChance) {
                    BattleCamController.GetInstance().RegisterCamHolder(cameraHolder);
                }
            }

        }

        protected override float Heuristic(Node start, Node end)
        {
            return (start.WorldPos.y - start.Parent?.WorldPos.y ?? start.WorldPos.y) * 50f;
        }
        
        protected override void EnemyUpdate()
        {
            DisallowShooting = !Grounded;
            
            // gun rotation
            switch (State)
            {
                case EnemyState.Moving:
                {
                    gun.localRotation = Quaternion.Slerp(gun.localRotation, Quaternion.identity, aimSpeed * Time.deltaTime);
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