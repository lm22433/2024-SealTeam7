using UnityEngine;

namespace Weapons
{
    public abstract class Weapon : ScriptableObject
    {
        [Header("Weapon Settings")]
        public string weaponName;
        
        [Header("Weapon Model Settings")]
        public GameObject weaponModel;
        public Vector3 spawnPosition;
        public Vector3 spawnRotation;

        public abstract void Initialize(WeaponInstance weaponInstance);
        public abstract void Attack();
    }
}
