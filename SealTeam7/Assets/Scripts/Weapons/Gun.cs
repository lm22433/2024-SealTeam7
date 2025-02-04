using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
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

        private readonly IntSyncVar _currentAmmo = new();
        private readonly IntSyncVar _totalAmmo = new();
        private readonly FloatSyncVar _nextFireTime = new();

        private bool _isShooting;
        private bool _isReloading;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _currentAmmo.Value = magazineSize;
            _totalAmmo.Value = maxAmmo;
        }
        
        [ServerRpc(RequireOwnership = true)]
        public void ServerShoot()
        {
            if (CanShoot())
            {
                _nextFireTime.Value = Time.time + 60.0f / fireRate;
                _currentAmmo.Value--;
                
                ObserversPlayShootEffects();
            
                // TODO: Handle raycast.
            }
            else if (IsEmpty())
            {
                _nextFireTime.Value = Time.time + 60.0f / fireRate;
                
                TargetPlayEmptyMagazineSound(Owner);
            }
        }

        [ObserversRpc]
        private void ObserversPlayShootEffects()
        {
            if (muzzleFlash) muzzleFlash.Play();
            if (gunShotSound && gunAudioSource) gunAudioSource.PlayOneShot(gunShotSound);
        }

        [TargetRpc]
        private void TargetPlayEmptyMagazineSound(NetworkConnection _)
        {
            if (emptyMagazineSound && gunAudioSource) 
                gunAudioSource.PlayOneShot(emptyMagazineSound);
        }

        private bool CanShoot() => !_isReloading && _currentAmmo.Value > 0 && Time.time >= _nextFireTime.Value;
        private bool IsEmpty() => !_isReloading && _currentAmmo.Value == 0 && Time.time >= _nextFireTime.Value; 

        [ServerRpc(RequireOwnership = true)]
        public void ServerReload()
        {
            if (_isReloading || _currentAmmo.Value >= magazineSize || _totalAmmo.Value <= 0) return;

            StartCoroutine(Reload());
        }

        [Server]
        private IEnumerator Reload()
        {
            _isReloading = true;
            TargetPlayReloadSound(Owner);
            ObserversPlayReloadAnimation();
            
            yield return new WaitForSeconds(reloadTime);
            
            int reloadAmount = Mathf.Min(magazineSize - _currentAmmo.Value, _totalAmmo.Value);
            _currentAmmo.Value += reloadAmount;
            _totalAmmo.Value -= reloadAmount;
            
            _isReloading = false;
        }

        [TargetRpc]
        private void TargetPlayReloadSound(NetworkConnection _)
        {
            if (reloadSound&& gunAudioSource) gunAudioSource.PlayOneShot(reloadSound);
        }

        [ObserversRpc]
        private void ObserversPlayReloadAnimation()
        {
            // TODO: Implement reload animation.
        }
    }
}