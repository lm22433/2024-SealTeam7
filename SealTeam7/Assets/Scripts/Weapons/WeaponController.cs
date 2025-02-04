using FishNet.Object;
using Input;
using UnityEngine;

namespace Weapons
{
    public class WeaponController : NetworkBehaviour
    {
        [Header("Weapon Prefabs")] 
        public Gun primaryGunPrefab;
        public Gun secondaryGunPrefab;
        public MeleeWeapon meleeWeaponPrefab;
        
        [Header("References")]
        [SerializeField] private Transform weaponHolder;

        private Gun _equippedGun;
        private Gun _primaryGun;
        private Gun _secondaryGun;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnWeapons();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
            {
                EquipWeapon(true);
            }
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            if (_equippedGun == null) return;
            
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
                // _currentGun.TryShoot();
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

            // if (inputController.GetReloadInput()) _currentGun.TryReload();
        }

        private void HandleMelee()
        {
            InputController inputController = InputController.GetInstance();

            // if (inputController.GetMeleeInput()) meleeWeapon.Attack();
        }
        
        private void HandleWeaponSwap()
        {
            InputController inputController = InputController.GetInstance();

            if (inputController.GetScrollSwapWeaponInput() != 0.0f || inputController.GetSwapWeaponInput())
                EquipWeapon(_equippedGun == _secondaryGun);
        }

        private void HandleWeaponEquip()
        {
            InputController inputController = InputController.GetInstance();
            
            if (inputController.GetEquipPrimaryInput()) EquipWeapon(true);
            if (inputController.GetEquipSecondaryInput()) EquipWeapon(false);
        }

        [Server]
        private void SpawnWeapons()
        {
            if (primaryGunPrefab == null || secondaryGunPrefab == null)
            {
                Debug.LogError("Weapon prefabs are not assigned!");
                return;
            }

            GameObject primaryInstance = Instantiate(
                primaryGunPrefab.gameObject,
                primaryGunPrefab.spawnPosition,
                Quaternion.Euler(primaryGunPrefab.spawnRotation)
            );
            
            ServerManager.Spawn(primaryInstance, Owner);
            _primaryGun = primaryInstance.GetComponent<Gun>();

            GameObject secondaryInstance = Instantiate(
                secondaryGunPrefab.gameObject,
                secondaryGunPrefab.spawnPosition,
                Quaternion.Euler(secondaryGunPrefab.spawnRotation)
            );
            ServerManager.Spawn(secondaryInstance, Owner);
            _secondaryGun = secondaryInstance.GetComponent<Gun>();

            if (weaponHolder != null)
            {
                _primaryGun.transform.SetParent(weaponHolder, false);
                _secondaryGun.transform.SetParent(weaponHolder, false);
            }

            _primaryGun.gameObject.SetActive(false);
            _secondaryGun.gameObject.SetActive(false);
        }
        
        private void EquipWeapon(bool usePrimary)
        {
            if (!IsOwner) return;

            CmdEquipWeapon(usePrimary);
        }

        [ServerRpc(RequireOwnership = true)]
        private void CmdEquipWeapon(bool usePrimary)
        {
            if (usePrimary)
            {
                if (_equippedGun != _primaryGun)
                {
                    SetActiveWeapon(_primaryGun, _secondaryGun);
                }
            }
            else
            {
                if (_equippedGun != _secondaryGun)
                {
                    SetActiveWeapon(_secondaryGun, _primaryGun);
                }
            }
            RpcUpdateActiveWeapon(usePrimary);
        }

        [Server]
        private void SetActiveWeapon(Gun newActive, Gun newInactive)
        {
            if (newActive != null)
            {
                newActive.gameObject.SetActive(true);
                _equippedGun = newActive;
            }
            if (newInactive != null)
            {
                newInactive.gameObject.SetActive(false);
            }
        }

        [ObserversRpc]
        private void RpcUpdateActiveWeapon(bool isPrimaryActive)
        {
            if (isPrimaryActive)
            {
                if (_primaryGun != null) _primaryGun.gameObject.SetActive(true);
                if (_secondaryGun != null) _secondaryGun.gameObject.SetActive(false);
                _equippedGun = _primaryGun;
            }
            else
            {
                if (_primaryGun != null) _primaryGun.gameObject.SetActive(false);
                if (_secondaryGun != null) _secondaryGun.gameObject.SetActive(true);
                _equippedGun = _secondaryGun;
            }
        }

    }
}