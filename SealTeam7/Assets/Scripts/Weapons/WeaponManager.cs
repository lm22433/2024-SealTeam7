using Input;
using UnityEngine;

namespace Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Guns")]
        public GunData primaryWeapon;
        public GunData secondaryWeapon;
        
        [Header("Melee Weapon")]
        public MeleeWeapon meleeWeapon;
        
        [Header("References")]
        [SerializeField] private Transform weaponHolder;
        private Camera _playerCamera;

        private GunBehaviour _currentGun;
        private GunBehaviour _primaryGunBehaviour;
        private GunBehaviour _secondaryGunBehaviour;

        private void Awake()
        {
            _playerCamera = Camera.main;
            InitializeWeapons();
            EquipWeapon(primaryWeapon);
        }
        
        private void InitializeWeapons()
        {
            _primaryGunBehaviour = CreateGunBehaviour(primaryWeapon);
            _secondaryGunBehaviour = CreateGunBehaviour(secondaryWeapon);
        }

        private GunBehaviour CreateGunBehaviour(GunData data)
        {
            GameObject instance = Instantiate(data.modelPrefab, weaponHolder);
            instance.transform.localPosition = data.spawnPosition;
            instance.transform.localEulerAngles = data.spawnRotation;
            instance.gameObject.SetActive(false);

            GunBehaviour gunBehaviour = instance.AddComponent<GunBehaviour>();
            gunBehaviour.Initialize(data, _playerCamera);
            return gunBehaviour;
        }

        private void EquipWeapon(GunData gunData)
        {
            GunBehaviour newGun = GetGunBehaviour(gunData);
            if (newGun == null || newGun == _currentGun) return;

            if (_currentGun != null)
            {
                _currentGun.SetVisible(false);
            }

            _currentGun = newGun;
            _currentGun.SetVisible(true);
        }

        private GunBehaviour GetGunBehaviour(GunData gunData) =>
            gunData == primaryWeapon ? _primaryGunBehaviour : _secondaryGunBehaviour;

        #region Player Input

        private void Update()
        {
            if (_currentGun == null) return;
            
            HandleShooting();
            HandleAiming();
            HandleReload();
            HandleMelee();
            HandleWeaponSwap();
            HandleWeaponEquip();
        }

        private void HandleShooting()
        {
            InputController inputController = InputController.GetInstance();

            if (inputController.GetShootInputHeld() || inputController.GetShootInputPressed())
            {
                _currentGun.TryShoot();
            }
        }

        private void HandleAiming()
        {
            InputController inputController = InputController.GetInstance();

            // TODO: if (inputController.GetAimInputHeld()) return;
        }

        private void HandleReload()
        {
            InputController inputController = InputController.GetInstance();

            if (inputController.GetReloadInput()) _currentGun.TryReload();
        }

        private void HandleMelee()
        {
            InputController inputController = InputController.GetInstance();

            if (inputController.GetMeleeInput()) meleeWeapon.Attack();
        }
        
        private void HandleWeaponSwap()
        {
            InputController inputController = InputController.GetInstance();

            if (inputController.GetScrollSwapWeaponInput() != 0.0f || inputController.GetSwapWeaponInput())
                EquipWeapon(_currentGun == _primaryGunBehaviour ? secondaryWeapon : primaryWeapon);
        }

        private void HandleWeaponEquip()
        {
            InputController inputController = InputController.GetInstance();
            
            if (inputController.GetEquipPrimaryInput()) EquipWeapon(primaryWeapon);
            if (inputController.GetEquipSecondaryInput()) EquipWeapon(secondaryWeapon);
        }
        
        #endregion
        
        public bool IsPrimaryWeaponEquipped()
        {
            return _currentGun == _primaryGunBehaviour;
        }
        
        public bool IsSecondaryWeaponEquipped()
        {
            return _currentGun == _secondaryGunBehaviour;
        }
    }
}
