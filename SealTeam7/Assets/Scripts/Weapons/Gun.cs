using System.Collections;
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
        public Quaternion spawnRotation = Quaternion.identity;
        
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

        // private readonly IntSyncVar _currentAmmo = new();
        // private readonly IntSyncVar _totalAmmo = new();
        // private readonly FloatSyncVar _nextFireTime = new();
        
        private int _currentAmmo;
        private int _totalAmmo;
        private float _nextFireTime;

        private bool _isShooting;
        private bool _isReloading;

        // public override void OnStartServer()
        // {
        //     base.OnStartServer();
        //
        //     _currentAmmo.Value = magazineSize;
        //     _totalAmmo.Value = maxAmmo;
        // }

        private void Awake()
        {
            _currentAmmo = magazineSize;
            _totalAmmo = maxAmmo;
        }

        // [ServerRpc(RequireOwnership = true)]
        // public void ServerShoot(Vector3 origin, Vector3 direction)
        // {
        //     if (CanShoot())
        //     {
        //         _nextFireTime.Value = Time.time + 60.0f / fireRate;
        //         _currentAmmo.Value--;
        //         
        //         ObserversPlayShootEffects();
        //     
        //         if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        //         {
        //             if (hit.collider.TryGetComponent(out IDamageable target))
        //                 target.TakeDamage(damage);
        //         }
        //     }
        //     else if (IsEmpty())
        //     {
        //         _nextFireTime.Value = Time.time + 60.0f / fireRate;
        //         
        //         TargetPlayEmptyMagazineSound(Owner);
        //     }
        // }

        public void Shoot(Vector3 origin, Vector3 direction)
        {
            if (CanShoot())
            {
                _nextFireTime = Time.time + 60.0f / fireRate;
                _currentAmmo--;

                if (muzzleFlash) muzzleFlash.Play();
                if (gunShotSound && gunAudioSource) gunAudioSource.PlayOneShot(gunShotSound);
                
                Damage(origin, direction);
            }
        }

        [ServerRpc]
        private void Damage(Vector3 origin, Vector3 direction)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
            {
                if (hit.collider.TryGetComponent(out IDamageable target))
                {
                    target.TakeDamage(damage);
                }
            }
        }

        // [ServerRpc(RequireOwnership = true)]
        // public void ServerBurstShoot()
        // {
        //     
        // }

        // [ObserversRpc]
        // private void ObserversPlayShootEffects()
        // {
        //     if (muzzleFlash) muzzleFlash.Play();
        //     if (gunShotSound && gunAudioSource) gunAudioSource.PlayOneShot(gunShotSound);
        // }
        //
        // [TargetRpc]
        // private void TargetPlayEmptyMagazineSound(NetworkConnection _)
        // {
        //     if (emptyMagazineSound && gunAudioSource) 
        //         gunAudioSource.PlayOneShot(emptyMagazineSound);
        // }

        // private bool CanShoot() => !_isReloading && _currentAmmo.Value > 0 && Time.time >= _nextFireTime.Value;
        // private bool IsEmpty() => !_isReloading && _currentAmmo.Value == 0 && Time.time >= _nextFireTime.Value; 

        private bool CanShoot() => !_isReloading && _currentAmmo > 0 && Time.time >= _nextFireTime;
        private bool IsEmpty() => !_isReloading && _currentAmmo == 0 && Time.time >= _nextFireTime; 

        // [ServerRpc(RequireOwnership = true)]
        // public void ServerReload()
        // {
        //     if (_isReloading || _currentAmmo.Value >= magazineSize || _totalAmmo.Value <= 0) return;
        //
        //     StartCoroutine(Reload());
        // }

        public void TryReload()
        {
            if (_isReloading || _currentAmmo >= magazineSize || _totalAmmo <= 0) return;

            StartCoroutine(Reload());
        }

        private IEnumerator Reload()
        {
            _isReloading = true;
            
            if (reloadSound && gunAudioSource) gunAudioSource.PlayOneShot(reloadSound);

            yield return new WaitForSeconds(reloadTime);

            int reloadAmount = Mathf.Min(magazineSize - _currentAmmo, _totalAmmo);
            _currentAmmo += reloadAmount;
            _totalAmmo -= reloadAmount;
            
            _isReloading = false;
        }

        // [Server]
        // private IEnumerator Reload()
        // {
        //     _isReloading = true;
        //     TargetPlayReloadSound(Owner);
        //     ObserversPlayReloadAnimation();
        //     
        //     yield return new WaitForSeconds(reloadTime);
        //     
        //     int reloadAmount = Mathf.Min(magazineSize - _currentAmmo.Value, _totalAmmo.Value);
        //     _currentAmmo.Value += reloadAmount;
        //     _totalAmmo.Value -= reloadAmount;
        //     
        //     _isReloading = false;
        // }
        //
        // [TargetRpc]
        // private void TargetPlayReloadSound(NetworkConnection _)
        // {
        //     if (reloadSound&& gunAudioSource) gunAudioSource.PlayOneShot(reloadSound);
        // }
        //
        // [ObserversRpc]
        // private void ObserversPlayReloadAnimation()
        // {
        //     // TODO: Implement reload animation.
        // }
    }
}