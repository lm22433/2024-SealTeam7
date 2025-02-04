using FishNet.Object;
using UnityEngine;

namespace Weapons
{
    public class Gun : NetworkBehaviour
    {
        public string gunName;
        public string gunDescription;

        public Vector3 spawnPosition;
        public Vector3 spawnRotation;
        
        public FireMode fireMode;
        public float damage = 10f;
        public float range = 100f;
        public float fireRate = 10f;

        public int magazineSize = 30;
        public int maxAmmo = 210;
        public float reloadTime = 2f;

        public Sprite displaySprite;
        public Vector2 spriteScale;
        public Vector3 spritePosition;
        
        private int _currentAmmo;
        private int _totalAmmo;
        private float _nextFireTime;

        private bool _isShooting;
        private bool _isReloading;
    }
}