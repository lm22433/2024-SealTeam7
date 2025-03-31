using Player;
using UnityEngine;
using UnityEngine.VFX;

namespace Enemies
{
    public class Necro : Vehicle
    {
        //[SerializeField] private VisualEffect necroRing;

        [SerializeField] private Transform siren;
        [SerializeField] private float sirenSpeed = 360f;

        public override void Init()
        {
            base.Init();
            //necroRing.Play();
            LastAttack = attackInterval - 2.0f;
        }

        // public override void SetupDeath()
        // {
        //     necroRing.Stop();
        //     base.SetupDeath();
        // }

        protected override void Attack(PlayerDamageable player)
        {
            EnemyManager.NecroSpawn(new Vector3(transform.position.x, transform.position.y + 2.0f, transform.position.z - 2.0f));
        }

        protected override void EnemyUpdate()
        {
            base.EnemyUpdate();
            siren.Rotate(Time.deltaTime * sirenSpeed * Vector3.up);
        }
    }
}