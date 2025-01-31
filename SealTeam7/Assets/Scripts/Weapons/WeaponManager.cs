using Input;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private bool _isAiming;
        
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

            _isAiming = false;
        }


        private void Update()
        {
            switch (_currentGun.fireMode)
            {
                case FireMode.Automatic:
                {
                    if (InputController.GetInstance().GetShootInputHeld())
                    {
                        _currentGun.Attack();
                    }
            
                    break;
                }
                case FireMode.SemiAutomatic:
                {
                    if (InputController.GetInstance().GetShootInputPressed())
                    {
                        _currentGun.Attack();
                    }
            
                    break;
                }
            }
            
            // Aiming
            if (InputController.GetInstance().GetAimInputHeld() && !_isAiming)
            {
                StartAiming();
            } else if (!InputController.GetInstance().GetAimInputHeld() && _isAiming)
            {
                StopAiming();
            }
            
            // Reloading
            if (InputController.GetInstance().GetReloadInput())
            {
                _currentGun.TryReload();
            }
            
            // Melee Attack
            if (InputController.GetInstance().GetMeleeInput())
            {
                meleeWeapon.Attack();
            }

            // Scrolling to Swap Weapons
            if (InputController.GetInstance().GetScrollSwapWeaponInput() != 0.0f)
            {
                SwapWeapon();
            }
            
            // Swap Weapons
            if (InputController.GetInstance().GetSwapWeaponInput())
            {
                SwapWeapon();
            }
            
            // Equip Primary Weapon
            if (InputController.GetInstance().GetEquipPrimaryInput())
            {
                EquipWeapon(primaryWeapon);
            }
            
            // Equip Primary Secondary Weapon
            if (InputController.GetInstance().GetEquipSecondaryInput())
            {
                EquipWeapon(secondaryWeapon);
            }
        }
        
        private void StartAiming() 
        {
            _isAiming = true;
            Debug.Log("Started Aiming");
        }

        private void StopAiming()
        {
            _isAiming = false;
            Debug.Log("Stopped Aiming");
        }
        
        private void SwapWeapon ()
        {
            EquipWeapon(_currentGun == primaryWeapon ? secondaryWeapon : primaryWeapon);
        }
        
        public bool IsPrimaryWeaponEquipped()
        {
            return _currentGun == primaryWeapon;
        }
        
        public bool IsSecondaryWeaponEquipped()
        {
            return _currentGun == secondaryWeapon;
        }
    }
}
