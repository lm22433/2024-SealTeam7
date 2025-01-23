using UnityEngine;

namespace Weapons
{
    [CreateAssetMenu(fileName = "Melee", menuName = "Weapons/Melee", order = 1)]
    public class MeleeWeapon : Weapon
    {
        [Header("Melee Settings")] 
        public float meleeDamage = 20f;
        public float meleeRange = 2f;
        
        [Header("Melee Effects")]
        public ParticleSystem hitParticles;
        
        [Header("Melee Audio")]
        public AudioSource meleeSound;
        public AudioSource hitSound;
        
        public override void Initialize(WeaponInstance weaponInstance)
        {
            
        }

        public override void Attack()
        {
            Debug.Log("Melee Attack");
        }
    }
}