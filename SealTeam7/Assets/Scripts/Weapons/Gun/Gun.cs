using System.Collections;
using UnityEngine;

namespace Weapons.Gun
{
    [CreateAssetMenu(fileName = "Gun", menuName = "Weapons/Gun", order = 0)]
    public class Gun : ScriptableObject
    {
        [Header("General Settings")]
        public string gunName;
        public string gunDescription;

        [Header("Model Settings")]
        public GameObject gunModel;
        public Vector3 spawnPosition;
        public Vector3 spawnRotation;
        
        [Header("Settings")]
        public bool isAutomatic;
        public float gunDamage = 10f;
        public float gunRange = 100f;
        public float gunFireRate = 10f;

        [Header("Ammo Settings")] 
        public int defaultMaxAmmo = 30;
        public float gunReloadTime = 2f;
        public int defaultTotalAmmo = 210;
        
        [System.NonSerialized] private bool _isReloading;
        [System.NonSerialized] private float _nextTimeToFire;
        [System.NonSerialized] private int _currentAmmo;
        [System.NonSerialized] private int _totalAmmo;
        [System.NonSerialized] private GunInstance _gunInstance;

        public void Initialize(GunInstance instance)
        {
            _gunInstance = instance;
            _gunInstance.Initialize();

            if (_currentAmmo == 0 && _totalAmmo == 0)
            {
                _currentAmmo = defaultMaxAmmo;
                _totalAmmo = defaultTotalAmmo;
            }

            _nextTimeToFire = 0f;
            _isReloading = false;
        }

        public void Attack()
        {
            if (CanFire())
            {
                Fire();
            }
            else if (_currentAmmo == 0 && !_isReloading)
            {
                Debug.Log("Plz reload!");
                _gunInstance.PlayEmptySound();
            }
        }

        private bool CanFire()
        {
            return !_isReloading && 
                   _currentAmmo > 0 && 
                   Time.time >= _nextTimeToFire;
        }

        private void Fire()
        {
            _nextTimeToFire = Time.time + 60f / gunFireRate;
            _currentAmmo--;
            
            _gunInstance.PlayMuzzleFlash();
            _gunInstance.PlayShootSound();
            
            Debug.Log("You fired a bullet! You have " + _currentAmmo + " bullets left!");

            RaycastHit hit;
            Ray ray = _gunInstance.GetFireRay();
            
            if (Physics.Raycast(ray, out hit, gunRange))
            {
                //TODO: Implement trail renderer.
                
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                damageable?.TakeDamage(gunDamage);
            }
        }

        public void TryReload()
        {
            if (!_isReloading && _currentAmmo < defaultMaxAmmo && _totalAmmo > 0)
            {
                _isReloading = true;
                _gunInstance.PlayReloadSound();

                _gunInstance.StartCoroutine(ReloadCoroutine());
            } 
        }

        private IEnumerator ReloadCoroutine()
        {
            yield return new WaitForSeconds(gunReloadTime);
            
            int ammoToReload = Mathf.Min(defaultMaxAmmo - _currentAmmo, _totalAmmo);
            _currentAmmo += ammoToReload;
            _totalAmmo -= ammoToReload;
            
            Debug.Log("You have reloaded " + ammoToReload + " bullets. You have " + _currentAmmo + " bullets in the mag. You have " + _totalAmmo + " bullets left!");
            
            _isReloading = false;
        }
    }
}