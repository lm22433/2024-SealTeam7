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
        [SerializeField] private LayerMask whatIsGround;
        
        [Header("Movement")]
        [SerializeField] private float rollSpeed;
        [SerializeField] private float tolerance;
        [SerializeField] private Transform NW;
        [SerializeField] private Transform NE;
        [SerializeField] private Transform SW;
        [SerializeField] private Transform SE;
        private float attackDelay;        
        public override void Init() 
        {
            base.Init();
            DeathDuration = 3.0f;
            BuriedAmount = 3.0f;
            attackDelay = 0.0f;
            State = EnemyState.Moving;
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
            //primaryRB.AddForce(Vector3.down * 9.8f, ForceMode.Acceleration);
            // test.y = 0;
            // test.Normalize();
            //primaryRB.AddForce(test * 20f, ForceMode.Impulse);
            
            DisallowShooting = !Grounded;
            // Debug.Log($"transform.position.y = {transform.position.y} --- (MapManager.GetInstance().GetHeight(transform.position) + groundedOffset) = ${(MapManager.GetInstance().GetHeight(transform.position) + groundedOffset)}");
            // if (transform.position.y < (MapManager.GetInstance().GetHeight(transform.position) + groundedOffset))
            // {
            //     // Debug.Log("REEEEEEEE");
            //     // Debug.Log(Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit2, transform.lossyScale.y, whatIsGround));
            //     // transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position) + groundedOffset, transform.position.z);
            //     // var test2 = Vector3.up;
            //     // Debug.Log("REEEEEEEE2");
            //     // test2.y = 0f;
            //     // test2.Normalize();
            //     // primaryRB.AddForce(test2 * 900f, ForceMode.Impulse);
            //     // Debug.Log(hit2.normal);
            //     
            // }

            // if (Grounded) 
            // {
            //     var normal = MapManager.GetInstance().GetNormal(transform.position);
            //     normal.y = 0;
            //     Debug.Log(normal);
            //     primaryRB.AddForce(normal.normalized * 9800f, ForceMode.Impulse);
            // }

            if (transform.position.y < (MapManager.GetInstance().GetHeight(transform.position) + (transform.lossyScale.y * 0.6f)))
            {
                primaryRB.linearVelocity = Vector3.zero;
                transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position) + (transform.lossyScale.y), transform.position.z);
            }

            // gun rotation
            switch (State)
            {
                case EnemyState.Moving:
                {
                    Mmodel.SetActive(false);
                    Msphere.SetActive(true);
                    if (Grounded)
                    {
                        float nwHeight = MapManager.GetInstance().GetHeight(Msphere.transform.position + NW.localPosition);
                        float neHeight = MapManager.GetInstance().GetHeight(Msphere.transform.position + NE.localPosition);
                        float swHeight = MapManager.GetInstance().GetHeight(Msphere.transform.position + SW.localPosition);
                        float seHeight = MapManager.GetInstance().GetHeight(Msphere.transform.position + SE.localPosition);
                        float sum = nwHeight +neHeight +swHeight +seHeight;
                        nwHeight = 1 - (nwHeight / sum);
                        neHeight = 1 - (neHeight / sum);
                        swHeight = 1 - (swHeight / sum);
                        seHeight = 1 - (seHeight / sum);
                        Vector3 dir = (NW.localPosition * nwHeight) + (NE.localPosition * neHeight) + (SW.localPosition * swHeight) + (SE.localPosition * seHeight);
                        if (dir.magnitude >= tolerance) dir = dir.normalized;
                        else dir = Vector3.zero;
                        primaryRB.AddForce(dir * rollSpeed, ForceMode.VelocityChange);
                        //primaryRB.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
                    }
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