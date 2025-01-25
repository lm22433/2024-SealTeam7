using UnityEngine;
using Weapons.Gun;

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
        
        private float _previousTriggerValue;
        
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
            switch (_currentGun.fireMode)
            {
                case FireMode.Automatic:
                {
                    if (Input.GetButton("Shoot1") || Input.GetAxis("Shoot2") > 0.5f)
                    {
                        _currentGun.Attack();
                    }

                    break;
                }
                case FireMode.SemiAutomatic:
                {
                    if (Input.GetButtonDown("Shoot1") || TriggerPressed())
                    {
                        _currentGun.Attack();
                    }

                    break;
                }
            }
            
            // Reloading
            if (Input.GetButtonDown("Reload"))
            {
                _currentGun.TryReload();
            }
            
            // Melee Attack
            if (Input.GetButtonDown("Melee"))
            {
                meleeWeapon.Attack();
            }

            // Scrolling to Swap Weapons
            if (Input.GetAxis("Mouse ScrollWheel") != 0.0f)
            {
                SwapWeapon();
            }
            
            // Swap Weapons
            if (Input.GetButtonDown("SwapWeapon"))
            {
                SwapWeapon();
            }
            
            // Equip Primary Weapon
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                EquipWeapon(primaryWeapon);
            }
            
            // Equip Primary Secondary Weapon
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                EquipWeapon(secondaryWeapon);
            }
        }
        
        private void SwapWeapon ()
        {
            EquipWeapon(_currentGun == primaryWeapon ? secondaryWeapon : primaryWeapon);
        }
        
        private bool TriggerPressed()
        {
            float triggerThreshold = 0.5f;
            float triggerValue = Input.GetAxis("Shoot2");
            bool wasPressed = _previousTriggerValue <= triggerThreshold && triggerValue > triggerThreshold;
            _previousTriggerValue = triggerValue;
            return wasPressed;
        }
    }
}
