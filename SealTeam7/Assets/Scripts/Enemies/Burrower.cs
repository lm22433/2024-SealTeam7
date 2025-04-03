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
        [SerializeField] private float diveSpeed;

        private void Start()
        {
            Rb.freezeRotation = true;
            Rb.detectCollisions = false;
        }

        protected override float Heuristic(Node start, Node end)
        {
            return start.WorldPos.y < burrowDepth + 20f ? 1000000000f : 0f;
        }

        protected override void Attack(PlayerDamageable toDamage)
        {
            toDamage.TakeDamage(attackDamage);
        }
        
        protected override void EnemyUpdate()
        {
            base.EnemyUpdate();
            
            transform.position = new Vector3(transform.position.x, burrowDepth, transform.position.z);
            coreTargetHeightOffset = transform.position.y - MapManager.GetInstance().GetHeight(transform.position);
            drill.Rotate(Time.deltaTime * drillSpeed * Vector3.forward);
        }
    }
}