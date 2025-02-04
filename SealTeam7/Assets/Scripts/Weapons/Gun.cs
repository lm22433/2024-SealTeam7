using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace Weapons
{
    public enum FireMode
    {
        SemiAutomatic,
        Automatic,
        Burst
    }
    
    public class Gun : NetworkBehaviour
    {
        [Header("General Settings")]
        public string gunName;
        public string gunDescription;

        [Header("Model Settings")]
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
        
        [Header("Audio Settings")]
        public AudioSource gunAudioSource;
        public AudioClip gunShotSound;
        public AudioClip reloadSound;
        public AudioClip emptyMagazineSound;
        
        [Header("Visual Effect Settings")]
        public ParticleSystem muzzleFlash;

        [Header("HUD Settings")]
        public Sprite displaySprite;
        public Vector2 spriteScale;
        public Vector3 spritePosition;
        
        private int _currentAmmo;
        private int _totalAmmo;
        private float _nextFireTime;

        private bool _isShooting;
        private bool _isReloading;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _currentAmmo = magazineSize;
            _totalAmmo = maxAmmo;
        }

        public void Shoot()
        {
            if (!IsOwner) return;
            
            if (CanShoot()) ServerShoot();
            else if (_currentAmmo == 0 && !_isReloading) TargetPlayReloadSound(Owner);
        }
        
        [ServerRpc(RequireOwnership = true)]
        private void ServerShoot()
        {
            ObserverShoot();
        }

        [ObserversRpc]
        private void ObserverShoot()
        {
            if (muzzleFlash) muzzleFlash.Play();
            if (gunShotSound && gunAudioSource) gunAudioSource.PlayOneShot(gunShotSound);
        }

        [TargetRpc]
        private void TargetPlayReloadSound(NetworkConnection _)
        {
            if (emptyMagazineSound && gunAudioSource) 
                gunAudioSource.PlayOneShot(emptyMagazineSound);
        }

        private bool CanShoot() => !_isReloading && _currentAmmo > 0 && Time.time >= _nextFireTime;

        public void TryReload()
        {
            if (!IsOwner) return;
            
            Debug.Log("Reload");   
        }

        public void TryAim()
        {
            if (!IsOwner) return;
            
            Debug.Log("Aim");
        }

        public void TryUnaim()
        {
            if (!IsOwner) return;
            
            Debug.Log("Unaim");
        }
        
    }
}