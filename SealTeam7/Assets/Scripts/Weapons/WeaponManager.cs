using UnityEngine;

namespace Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Guns")]
        public Gun.Gun primaryWeapon;
        public Gun.Gun secondaryWeapon;
        
        [Header("Melee Weapon")]
        public MeleeWeapon meleeWeapon;
        
        [Header("References")]
        [SerializeField] private Transform weaponHolder;

        private Gun.Gun _currentGun;
        private GameObject _currentGunInstance;
        
        private void Start()
        {
            EquipWeapon(primaryWeapon);
        }

        public void EquipWeapon(Gun.Gun gun)
        {
            if (gun == null)
            {
                Debug.LogWarning("Attempted to equip a null weapon. This should never happen.");
                return;
            }

            if (gun == _currentGun)
            {
                return;
            }

            if (_currentGunInstance != null)
            {
                Destroy(_currentGunInstance);
            }

            // Instantiate the new weapon model
            _currentGunInstance = Instantiate(gun.gunModel, weaponHolder);
            _currentGunInstance.transform.localPosition = gun.spawnPosition;
            _currentGunInstance.transform.localEulerAngles = gun.spawnRotation;

            _currentGun = gun;
            
            GunInstance instance = _currentGunInstance.GetComponent<GunInstance>();
            if (instance == null)
            {
                Debug.LogError("Weapon model prefab is missing WeaponInstance component!");
                return;
            }
            gun.Initialize(instance);
        }


        private void Update()
        {
            if (_currentGun is Gun.Gun gun)
            {
                if (Input.GetButtonDown("Reload"))
                {
                    gun.TryReload();
                }

                if (gun.isAutomatic)
                {
                    if (Input.GetButton("Shoot1") || Input.GetAxis("Shoot2") > 0.5f)
                    {
                        gun.Attack();
                    }
                }
                else
                {
                    // TODO: Implement semi automatic trigger
                    if (Input.GetButtonDown("Shoot1"))
                    {
                        gun.Attack();
                    }
                }
            }
            
            // Button to Melee
            if (Input.GetButtonDown("Melee"))
            {
                meleeWeapon.Attack();
            }

            // Scroll Wheel to Swap Weapons
            if (Input.GetAxis("Mouse ScrollWheel") != 0.0f)
            {
                SwapWeapon();
            }
            
            // Button to Swap Weapons
            if (Input.GetButtonDown("SwapWeapon"))
            {
                SwapWeapon();
            }
            
            // Button to Change to Primary Weapon
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                EquipWeapon(primaryWeapon);
            }
            
            // Button to Change to Secondary Weapon
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                EquipWeapon(secondaryWeapon);
            }
        }
        
        private void SwapWeapon ()
        {
            EquipWeapon(_currentGun == primaryWeapon ? secondaryWeapon : primaryWeapon);
        }
    }
}
