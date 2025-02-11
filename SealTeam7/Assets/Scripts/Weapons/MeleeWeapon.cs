using UnityEngine;

namespace Weapons
{
    [CreateAssetMenu(fileName = "Melee", menuName = "Weapons/Melee", order = 1)]
    public class MeleeWeapon : ScriptableObject
    {
        [Header("General Settings")]
        public string meleeName;
        public string meleeDescription;
        
        [Header("Model Settings")]
        public GameObject meleeModel;
        public Vector3 spawnPosition;
        public Vector3 spawnRotation;
        
        [Header("Melee Settings")] 
        public float meleeDamage = 20f;
        public float meleeRange = 2f;
        
        [Header("Melee Effects")]
        public ParticleSystem hitParticles;
        
        [Header("Melee Audio")]
        public AudioSource meleeSound;
        public AudioSource hitSound;

        public void Attack()
        {
            Debug.Log("Melee Attack");
        }
    }
}