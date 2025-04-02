using Enemies.Utils;
using Map;
using Player;
using Projectiles;
using UnityEngine;
using UnityEngine.Serialization;

namespace Enemies
{
    public class Mech : Enemy
    {
        [SerializeField] private GameObject mmodel;
        [SerializeField] private GameObject msphere;
        [SerializeField] private Rigidbody primaryRb;
        [SerializeField] private LayerMask whatIsGround;
        
        [Header("Movement")]
        [SerializeField] private float rollSpeed;
        [SerializeField] private float tolerance;
        [SerializeField] private Transform nw;
        [SerializeField] private Transform ne;
        [SerializeField] private Transform sw;
        [SerializeField] private Transform se;
        
        [Header("Projectile")]
        [SerializeField] private float missileTargetChangeTime;
        [SerializeField] private float newMissileTargetRadius;
        [SerializeField] private Transform gun;
        private Projectile _proj;
        private float _lastTargetChange;
        
        private float _attackDelay;
        public override void Init() 
        {
            base.Init();
            
            BuriedAmount = 3.0f;
            _attackDelay = 0.0f;
            
            _lastTargetChange = 0f;
            _proj = null;
        }
        
        protected override void Attack(PlayerDamageable toDamage)
        {
            GameObject obj = ProjectilePool.GetInstance().GetFromPool(projectileType, muzzle?.position ?? transform.position,
                Quaternion.LookRotation(TargetPosition - (muzzle?.position ?? transform.position)));
            obj.transform.parent = EnemyManager.transform;

            if (!obj.TryGetComponent(out _proj)) Debug.LogError("Projectile component not found");
            
            _proj.Init();
            _proj.projectileType = projectileType;
            _proj.TargetPosition = TargetPosition;
            _proj.ToDamage = toDamage;
            _proj.Damage = attackDamage;
        }

        protected override float Heuristic(Node start, Node end)
        {
            return Mathf.Max(start.WorldPos.y - start.Parent?.WorldPos.y ?? start.WorldPos.y, 0f) * 100f;
        }

        protected override void UpdateState()
        {
            _attackDelay += Time.deltaTime;
            if (State is EnemyState.Moving) _attackDelay = 0.0f;
            else if (_attackDelay >= 10.0f)
            {
                _attackDelay = 0.0f;
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
                primaryRb.linearVelocity = Vector3.zero;
                transform.position = new Vector3(transform.position.x, MapManager.GetInstance().GetHeight(transform.position) + (transform.lossyScale.y), transform.position.z);
            }
            
            _lastTargetChange += Time.deltaTime;

            if (_proj)
            {
                if (!_proj.gameObject.activeInHierarchy) _proj = null;
                else if (_lastTargetChange > missileTargetChangeTime)
                {
                    var randomCircle = Random.insideUnitCircle.normalized * newMissileTargetRadius;
                    _proj.TargetPosition = _proj.transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                }
            }
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    mmodel.SetActive(false);
                    msphere.SetActive(true);
                    if (Grounded)
                    {
                        float nwHeight = MapManager.GetInstance().GetHeight(msphere.transform.position + nw.localPosition);
                        float neHeight = MapManager.GetInstance().GetHeight(msphere.transform.position + ne.localPosition);
                        float swHeight = MapManager.GetInstance().GetHeight(msphere.transform.position + sw.localPosition);
                        float seHeight = MapManager.GetInstance().GetHeight(msphere.transform.position + se.localPosition);
                        float sum = nwHeight +neHeight +swHeight +seHeight;
                        nwHeight = 1 - (nwHeight / sum);
                        neHeight = 1 - (neHeight / sum);
                        swHeight = 1 - (swHeight / sum);
                        seHeight = 1 - (seHeight / sum);
                        Vector3 dir = (nw.localPosition * nwHeight) + (ne.localPosition * neHeight) + (sw.localPosition * swHeight) + (se.localPosition * seHeight);
                        if (dir.magnitude >= tolerance) dir = dir.normalized;
                        else dir = Vector3.zero;
                        primaryRb.AddForce(dir * rollSpeed, ForceMode.VelocityChange);
                        //primaryRB.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
                    }
                    break;
                }
                case EnemyState.AttackCore:
                    mmodel.SetActive(true);
                    msphere.SetActive(false);
                    break;
                case EnemyState.AttackHands:
                {
                    mmodel.SetActive(true);
                    msphere.SetActive(false);
                    break;
                }
            }
        }
    }
}