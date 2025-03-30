using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Burrower : Vehicle
    {
        [SerializeField] private Transform drill;
        [SerializeField] private float drillSpeed;
        [SerializeField] private float burrowDepth;
        private bool _burrowing;
        
        protected override float Heuristic(Node start, Node end)
        {
            return 0f;
        }

        protected override void Attack(PlayerDamageable toDamage)
        {
            toDamage.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            Rb.freezeRotation = _burrowing;
            Rb.detectCollisions = !_burrowing;
            
            DisallowShooting = Vector3.Dot(transform.forward, TargetPosition - transform.position) < 0.8f || !Grounded;
            drill.Rotate(Time.deltaTime * drillSpeed * Vector3.forward);
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    if (!_burrowing) _burrowing = true;
                    
                    transform.position = new Vector3(
                        transform.position.x,
                        MapManager.GetInstance().GetHeight(transform.position) - burrowDepth,
                        transform.position.z
                    );
                    
                    TargetDirection = (Path[PathIndex] - new Vector3(
                        transform.position.x,
                        MapManager.GetInstance().GetHeight(transform.position),
                        transform.position.z
                    )).normalized;
                    TargetRotation = Quaternion.LookRotation(TargetDirection, MapManager.GetInstance().GetNormal(transform.position));
                    
                    break;
                }
                case EnemyState.AttackCore:
                {
                    if (_burrowing)
                    {
                        Rb.linearVelocity = Vector3.zero;
                        transform.position = new Vector3(
                            transform.position.x,
                            MapManager.GetInstance().GetHeight(transform.position) + groundedOffset,
                            transform.position.z
                        );
                        
                        _burrowing = false;
                    }

                    break;
                }
                case EnemyState.AttackHands:
                {
                    if (_burrowing)
                    {
                        Rb.linearVelocity = Vector3.zero;
                        transform.position = new Vector3(
                            transform.position.x,
                            MapManager.GetInstance().GetHeight(transform.position) + groundedOffset,
                            transform.position.z
                        );
                        
                        _burrowing = false;
                    }

                    var xAngle = Quaternion.LookRotation(TargetPosition - drill.position).eulerAngles.x - transform.eulerAngles.x;
                    var drillRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    drill.localRotation = Quaternion.Slerp(drill.localRotation, drillRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    
                    break;
                }
                case EnemyState.Idle:
                {
                    if (_burrowing)
                    {
                        Rb.linearVelocity = Vector3.zero;
                        transform.position = new Vector3(
                            transform.position.x,
                            MapManager.GetInstance().GetHeight(transform.position) + groundedOffset,
                            transform.position.z
                        );
                        
                        _burrowing = false;
                    }
                    break;
                }
            }
        }
    }
}