using Enemies.Utils;
using Map;
using Player;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies
{
    public class Spawner : Vehicle
    {
        [SerializeField] private EnemyData spawnee;

        public override void Init()
        {
            base.Init();
            LastAttack = attackInterval - 2.0f;
        }
        
        protected override void Attack(PlayerDamageable player)
        {
            EnemyManager.SpawnerSpawn(new Vector3(transform.position.x, transform.position.y + 2.0f, transform.position.z - 2.0f), spawnee, attackDamage);
        }
    }
}