using System.Collections;
using Input;
using UnityEngine;

namespace Weapons
{
    public class GunBehaviour : MonoBehaviour
    {
        
        private GunData _gunData;
        private GunInstance _gunInstance;
        private Camera _camera;

        private int _currentAmmo;
        private int _totalAmmo;
        private float _nextFireTime;

        private bool _isShooting;
        private bool _isReloading;

        public void Initialize(GunData gunData, Camera playerCamera)
        {
            _gunData = gunData;
            _camera = playerCamera;
            _gunInstance = GetComponent<GunInstance>();
            _currentAmmo = gunData.magazineSize;
            _totalAmmo = gunData.maxAmmo;
        }

        public void TryShoot()
        {
            if (CanShoot()) Shoot();
            else if (_currentAmmo == 0 && !_isReloading) _gunInstance.PlayEmptySound();
        }

        private bool CanShoot()
        {
            bool isAutomaticReady = _gunData.fireMode == FireMode.Automatic && 
                                    InputController.GetInstance().GetShootInputHeld();
    
            bool isSemiAutoReady = _gunData.fireMode == FireMode.SemiAutomatic && 
                                   InputController.GetInstance().GetShootInputPressed();

            return !_isReloading && 
                   _currentAmmo > 0 && 
                   Time.time >= _nextFireTime &&
                   (isAutomaticReady || isSemiAutoReady);
        }

        private void Shoot()
        {
            _nextFireTime = Time.time + 60f / _gunData.fireRate;
            _currentAmmo--;
            
            _gunInstance.PlayMuzzleFlash();
            _gunInstance.PlayShootSound();
            
            Debug.Log("You fired a bullet! You have " + _currentAmmo + " bullets left!");

            if (Physics.Raycast(_camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)), out var hit, _gunData.range))
            {
                //TODO: Implement trail renderer.
                
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                damageable?.TakeDamage(_gunData.damage);
            }
        }

        public void TryReload()
        {
            if (_isReloading || _currentAmmo >= _gunData.magazineSize || _totalAmmo <= 0) return;

            StartCoroutine(Reload());
        }

        private IEnumerator Reload()
        {
            _isReloading = true;
            _gunInstance.PlayReloadSound();

            yield return new WaitForSeconds(_gunData.reloadTime);
            
            int reloadAmount = Mathf.Min(_gunData.magazineSize - _currentAmmo, _totalAmmo);
            _currentAmmo += reloadAmount;
            _totalAmmo -= reloadAmount;
            
            _isReloading = false;
        }
        
        public void SetVisible(bool isVisible) => gameObject.SetActive(isVisible);

    }
}