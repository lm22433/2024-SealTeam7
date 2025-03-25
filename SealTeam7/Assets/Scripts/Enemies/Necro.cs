using Player;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies
{
    public class Necro : Vehicle
    {
        [SerializeField] private VisualEffect necroRing;

        public override void Init()
        {
            base.Init();
            necroRing.Play();
            LastAttack = attackInterval - 2.0f;
        }
        
        public override void SetupDeath()
        {
            necroRing.Stop();
            base.SetupDeath();
        }

        protected override void Attack(PlayerDamageable player)
        {
            EnemyManager.NecroSpawn(new Vector3(transform.position.x, transform.position.y + 2.0f, transform.position.z - 2.0f));
        }
    }
}