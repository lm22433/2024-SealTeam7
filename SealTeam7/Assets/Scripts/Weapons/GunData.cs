using UnityEngine;

namespace Weapons
{
    public enum FireMode
    {
        SemiAutomatic,
        Automatic,
        Burst
    }
    
    [CreateAssetMenu(fileName = "GunData", menuName = "Weapons/Gun Data", order = 0)]
    public class GunData : ScriptableObject
    {
        [Header("General Settings")]
        public string name;
        public string description;

        [Header("Model Settings")]
        public GameObject modelPrefab;
        public Vector3 spawnPosition;
        public Vector3 spawnRotation;
        
        [Header("Gun Settings")]
        public FireMode fireMode;
        public float damage = 10f;
        public float range = 100f;
        public float fireRate = 10f;

        [Header("Ammo Settings")] 
        public int magazineSize = 30;
        public int maxAmmo = 210;
        public float reloadTime = 2f;

        [Header("HUD Settings")] 
        public Sprite displaySprite;
        public Vector2 spriteScale;
        public Vector3 spritePosition;
    }
}