using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class Chinook : Helicopter
    {
        [SerializeField] private EnemyData[] spawnableEnemies;
        [SerializeField] private int spawnCount;
        
        protected override void Attack(PlayerDamageable toDamage)
        {
            base.Attack(toDamage);
            EnemyManager.GetInstance().SpawnerSpawn(transform.position, spawnableEnemies[Random.Range(0, spawnableEnemies.Length)], spawnCount);
        }
    }
}