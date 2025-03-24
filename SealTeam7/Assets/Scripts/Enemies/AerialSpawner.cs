using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class AerialSpawner : Enemy
    {
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
            return 1 / (start.WorldPos - EnemyManager.godlyCore.transform.position).sqrMagnitude * 1000f;
        }
        
        protected override void Attack(PlayerDamageable toDamage)
        {
            EnemyManager.SpawnerSpawn(spawnPoint.position, spawnee, attackDamage);
        }

        protected override void UpdateTarget()
        {
            TargetPosition = _oppositePosition;
        }

        protected override void EnemyUpdate()
        {
            if ((transform.position - TargetPosition).sqrMagnitude < 1000f) EnemyManager.Kill(this);
            TargetRotation = Quaternion.Euler(transform.eulerAngles.x, Quaternion.LookRotation(TargetPosition - transform.position).eulerAngles.y, transform.eulerAngles.z);
        }

        protected override void EnemyFixedUpdate()
        {
            if (State is not (EnemyState.Moving or EnemyState.Dying) && !DisallowMovement) Rb.AddForceAtPosition(TargetDirection * (acceleration * 10f), Rb.worldCenterOfMass + forceOffset);
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}