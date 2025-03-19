using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class AerialSpawner : Enemy
    {
        [SerializeField] private float flyHeight;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private EnemyData spawnee;
        private Vector3 _oppositePosition;

        public override void Init()
        {
            base.Init();
            
            LastAttack = attackInterval - 2.0f;
            var mapSize = MapManager.GetInstance().GetMapSize() * MapManager.GetInstance().GetMapSpacing();
            transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);
            _oppositePosition = new Vector3(mapSize - transform.position.x, flyHeight, mapSize - transform.position.z);
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
            if ((transform.position - _oppositePosition).sqrMagnitude < 1000f) EnemyManager.Kill(this);
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(_oppositePosition - transform.position).eulerAngles.y, transform.eulerAngles.z);
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