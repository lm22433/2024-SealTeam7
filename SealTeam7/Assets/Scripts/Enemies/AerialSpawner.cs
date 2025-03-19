using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class AerialSpawner : Enemy
    {
        [SerializeField] private float flyHeight;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private EnemyData spawnee;

        protected override void Start()
        {
            base.Start();
            LastAttack = attackInterval - 2.0f;
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
        }
        
        protected override float Heuristic(Node start, Node end)
        {
            return start.WorldPos.y > flyHeight - 10f ? 10000f : 0f;
        }
        
        protected override void Attack(PlayerDamageable toDamage)
        {
            EnemyManager.SpawnerSpawn(spawnPoint.position, spawnee, attackDamage);
        }

        protected override void EnemyUpdate()
        {
            TargetRotation = Quaternion.LookRotation(new Vector3(-transform.position.x, transform.position.y, -transform.position.z));
            TargetDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }

        protected override void EnemyFixedUpdate()
        {
            if (State is not (EnemyState.Moving or EnemyState.Dying) && !DisallowMovement) Rb.AddForceAtPosition(TargetDirection * (acceleration * 10f), Rb.worldCenterOfMass + forceOffset);
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}