using System.Collections;
using UnityEngine;

namespace Weapons
{
    [CreateAssetMenu(fileName = "Gun", menuName = "Weapons/Gun", order = 0)]
    public class GunWeapon : Weapon
    {
        [Header("Gun Settings")]
        public bool isAutomatic;
        public float gunDamage = 10f;
        public float gunRange = 100f;
        public float gunFireRate = 10f;

        [Header("Gun Ammo Settings")] 
        public int defaultMaxAmmo = 30;
        public float gunReloadTime = 2f;
        public int defaultTotalAmmo = 210;
        
        [System.NonSerialized] private bool isReloading;
        [System.NonSerialized] private float nextTimeToFire;
        [System.NonSerialized] private int currentAmmo;
        [System.NonSerialized] private int totalAmmo;
        [System.NonSerialized] private WeaponInstance weaponInstance;

        public override void Initialize(WeaponInstance instance)
        {
            weaponInstance = instance;
            weaponInstance.Initialize();

            if (currentAmmo == 0 && totalAmmo == 0)
            {
                currentAmmo = defaultMaxAmmo;
                totalAmmo = defaultTotalAmmo;
            }

            nextTimeToFire = 0f;
            isReloading = false;
        }

        public override void Attack()
        {
            if (CanFire())
            {
                Fire();
            }
            else if (currentAmmo == 0 && !isReloading)
            {
                Debug.Log("Plz reload!");
                weaponInstance.PlayEmptySound();
            }
        }

        private bool CanFire()
        {
            return !isReloading && 
                   currentAmmo > 0 && 
                   Time.time >= nextTimeToFire;
        }

        private void Fire()
        {
            nextTimeToFire = Time.time + 60f / gunFireRate;
            currentAmmo--;
            
            weaponInstance.PlayMuzzleFlash();
            weaponInstance.PlayShootSound();
            
            Debug.Log("You fired a bullet! You have " + currentAmmo + " bullets left!");

            RaycastHit hit;
            Ray ray = weaponInstance.GetFireRay();
            
            if (Physics.Raycast(ray, out hit, gunRange))
            {
            //     if (bulletTrailPrefab != null)
            //     {
            //         TrailRenderer trail = Instantiate(bulletTrailPrefab, weaponInstance.GetMuzzlePosition(), Quaternion.identity);
            //         MonoBehaviour runner = weaponInstance as MonoBehaviour;
            //         runner.StartCoroutine(SpawnTrail(trail, hit.point));
            //     }
            //
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            damageable?.TakeDamage(gunDamage);
            }
        }

        public void TryReload()
        {
            if (!isReloading && currentAmmo < defaultMaxAmmo && totalAmmo > 0)
            {
                isReloading = true;
                weaponInstance.PlayReloadSound();

                weaponInstance.StartCoroutine(ReloadCoroutine());
            } 
        }

        private IEnumerator ReloadCoroutine()
        {
            yield return new WaitForSeconds(gunReloadTime);
            
            int ammoToReload = Mathf.Min(defaultMaxAmmo - currentAmmo, totalAmmo);
            currentAmmo += ammoToReload;
            totalAmmo -= ammoToReload;
            
            Debug.Log("You have reloaded " + ammoToReload + " bullets. You have " + currentAmmo + " bullets in the mag. You have " + totalAmmo + " bullets left!");
            
            isReloading = false;
        }
    }
}