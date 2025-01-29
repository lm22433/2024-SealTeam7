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
            InputController inputController = InputController.GetInstance();
            switch (_currentGun.fireMode)
            {
                case FireMode.Automatic:
                    if (inputController.GetShootInputHeld()) _currentGun.Attack();
                    break;
                case FireMode.SemiAutomatic:
                    if (inputController.GetShootInputPressed()) _currentGun.Attack();
                    break;
            }
            
            // Aiming
            bool isAimingHeld = inputController.GetAimInputHeld();
            if (isAimingHeld && !_isAiming) StartAiming();
            else if (!isAimingHeld && _isAiming) StopAiming();
            
            // Reloading
            if (inputController.GetReloadInput()) _currentGun.TryReload();
            
            
            // Melee Attack
            if (inputController.GetMeleeInput()) meleeWeapon.Attack();

            // Swapping Weapons
            if (inputController.GetSwapWeaponInput() || inputController.GetScrollSwapWeaponInput() != 0.0f) SwapWeapon();
            
            // Equip Specific Weapons
            if (inputController.GetEquipPrimaryInput()) EquipWeapon(primaryWeapon);
            if (inputController.GetEquipSecondaryInput()) EquipWeapon(secondaryWeapon);
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
