using Enemies.Utils;
using Player;
using UnityEngine;

namespace Enemies
{
    public class AerialSpawner : Enemy
    {
        [SerializeField] float flyHeight;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject projectile;
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
            EnemyManager.SpawnerSpawn(new Vector3(transform.position.x, transform.position.y + 2.0f, transform.position.z - 2.0f), spawnee, attackDamage);
        }

        protected override void EnemyFixedUpdate()
        {
            if (Rb.position.y > flyHeight) Rb.AddForce(Vector3.down, ForceMode.Impulse);
            if (Rb.position.y < flyHeight) Rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
    }
}