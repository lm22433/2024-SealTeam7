using Enemies.Utils;
using Map;
using UnityEngine;

namespace Enemies
{
    public class Mech : Enemy
    {
        [SerializeField] private GameObject Mmodel;
        [SerializeField] private GameObject Msphere;
        [SerializeField] private Rigidbody primaryRB;
        private float attackDelay;        
        public override void Init() 
        {
            base.Init();
            DeathDuration = 3.0f;
            BuriedAmount = 3.0f;
            attackDelay = 0.0f;
            State = EnemyState.AttackCore;
        }

        protected override float Heuristic(Node start, Node end)
        {
            return Mathf.Max(start.WorldPos.y - start.Parent?.WorldPos.y ?? start.WorldPos.y, 0f) * 100f;
        }

        protected override void UpdateState()
        {
            attackDelay += Time.deltaTime;
            if (State is EnemyState.Moving) attackDelay = 0.0f;
            else if (attackDelay >= 10.0f)
            {
                attackDelay = 0.0f;
                State = EnemyState.Moving;
            }
        }

        protected override void EnemyUpdate()
        {
            DisallowShooting = !Grounded;
            if (transform.position.y < MapManager.GetInstance().GetHeight(transform.position) + groundedOffset)
            {
                transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position) + groundedOffset, transform.position.z);
            }
            
            // gun rotation
            switch (State)
            {
                case EnemyState.Moving:
                {
                    Mmodel.SetActive(false);
                    Msphere.SetActive(true);
                    break;
                }
                case EnemyState.AttackCore:
                    Mmodel.SetActive(true);
                    Msphere.SetActive(false);
                    break;
                case EnemyState.AttackHands:
                {
                    Mmodel.SetActive(true);
                    Msphere.SetActive(false);
                    break;
                }
            }
        }
    }
}