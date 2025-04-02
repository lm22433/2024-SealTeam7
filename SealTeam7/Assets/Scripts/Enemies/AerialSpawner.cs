using Enemies.Utils;
using Map;
using Player;
using UnityEngine;

namespace Enemies
{
    public class AerialSpawner : Aircraft
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private EnemyData spawnee;
        private Vector3 _oppositePosition;

        public override void Init()
        {
            base.Init();
            
            LastAttack = attackInterval - 2.0f;
            var mapSize = MapManager.GetInstance().GetMapSize() * MapManager.GetInstance().GetMapSpacing();
            _oppositePosition = new Vector3(mapSize - transform.position.x, flyHeight, mapSize - transform.position.z);
        }

        protected override float Heuristic(Node start, Node end)
        {
            return 1 / (start.WorldPos - EnemyManager.godlyCore.transform.position).sqrMagnitude * 1000f;
        }
        
        protected override void Attack(PlayerDamageable toDamage)
        {
            for (var i = 0; i < attackDamage; i++)
            {
                var offset = new Vector3((Random.Range(0, 1) * 2 - 1) * i, 0f, (Random.Range(0, 1) * 2 - 1) * i);
                EnemyManager.SpawnEnemies(spawnee, spawnPoint.position + offset, spawnPoint.rotation);
            }
        }

        protected override void UpdateTarget()
        {
            TargetPosition = _oppositePosition;
        }

        protected override void UpdateState()
        {
            if (State is EnemyState.Dying) return;
            if (State is not EnemyState.Idle) State = EnemyState.MoveAndAttack;
        }

        protected override void EnemyUpdate()
        {
            base.EnemyUpdate();
            if ((transform.position - TargetPosition).sqrMagnitude < 1000f) EnemyManager.Kill(this);
        }
    }
}