using Enemies.Utils;
using Map;
using Player;
using UnityEngine;
using System;
using UnityEngine.VFX;

namespace Enemies
{
    public class Burrower : Enemy
    {
        [SerializeField] private Transform drill;
        [SerializeField] private float drillSpeed;
        [SerializeField] private ParticleSystem[] dustTrails;
        [SerializeField] private float burrowDepth;
        
        protected override float Heuristic(Node start, Node end)
        {
            return (start.WorldPos.y - start.Parent?.WorldPos.y ?? start.WorldPos.y) * 100f;
        }
        
        protected override void EnemyUpdate()
        {
            DisallowMovement = Vector3.Dot(transform.up, MapManager.GetInstance().GetNormal(transform.position)) < 0.5f;
            DisallowShooting = Vector3.Dot(transform.forward, TargetPosition - transform.position) < 0.8f || !Grounded;
            drill.Rotate(Time.deltaTime * drillSpeed * Vector3.forward);
            
            switch (State)
            {
                case EnemyState.Moving:
                {
                    var coreTarget = new Vector3(
                        EnemyManager.godlyCore.transform.position.x,
                        MapManager.GetInstance().GetHeight(EnemyManager.godlyCore.transform.position) + coreTargetHeightOffset,
                        EnemyManager.godlyCore.transform.position.z
                    );
                    
                    if ((coreTarget - transform.position).sqrMagnitude > SqrAttackRange + stopShootingThreshold)
                    {
                        transform.position = new Vector3(transform.position.x,
                            MapManager.GetInstance().GetHeight(transform.position) - burrowDepth, transform.position.z);
                        Rb.freezeRotation = true;
                        Rb.detectCollisions = false;
                    }

                    drill.localRotation = Quaternion.Slerp(drill.localRotation,
                        Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x,
                        Quaternion.LookRotation((Path.Length > 0 ? Path[PathIndex] : TargetPosition) -
                                                transform.position).eulerAngles.y, transform.eulerAngles.z);

                    if (DisallowMovement || !Grounded)
                    {
                        foreach (var dustTrail in dustTrails)
                            if (dustTrail.isPlaying)
                                dustTrail.Stop();
                    }
                    else
                    {
                        foreach (var dustTrail in dustTrails)
                            if (!dustTrail.isPlaying)
                                dustTrail.Play();
                    }

                    break;
            }
                case EnemyState.AttackCore:
                    if (Grounded)
                    {
                        Rb.linearVelocity = Vector3.zero;
                        transform.position = new Vector3(transform.position.x,
                            MapManager.GetInstance().GetHeight(transform.position) + groundedOffset,
                            transform.position.z);
                    }

                    Rb.freezeRotation = false;
                    Rb.detectCollisions = true;
                    break;
                case EnemyState.AttackHands:
                {
                    if (Grounded)
                    {
                        Rb.linearVelocity = Vector3.zero;
                        transform.position = new Vector3(transform.position.x,
                            MapManager.GetInstance().GetHeight(transform.position) + groundedOffset,
                            transform.position.z);
                    }

                    Rb.freezeRotation = false;
                    Rb.detectCollisions = true;

                    var xAngle = Quaternion.LookRotation(TargetPosition - drill.position).eulerAngles.x -
                                 transform.eulerAngles.x;
                    TargetRotation = Quaternion.Euler(xAngle, 0f, 0f);
                    drill.localRotation = Quaternion.Slerp(drill.localRotation,
                        TargetRotation * Quaternion.AngleAxis(-90, Vector3.right), aimSpeed * Time.deltaTime);
                    TargetRotation = Quaternion.Euler(transform.eulerAngles.x,
                        Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y,
                        transform.eulerAngles.z);
                    break;
                }
                case EnemyState.Dying:
                {
                    foreach (var dustTrail in dustTrails)
                        if (dustTrail.isPlaying)
                            dustTrail.Stop();
                    break;
                }
                case EnemyState.Idle:
                {
                    if (Grounded)
                    {
                        Rb.linearVelocity = Vector3.zero;
                        transform.position = new Vector3(transform.position.x,
                            MapManager.GetInstance().GetHeight(transform.position) + groundedOffset,
                            transform.position.z);
                    }
                    Rb.detectCollisions = true;
                    Rb.freezeRotation = false;
                    break;
                }
            }
        }
    }
}