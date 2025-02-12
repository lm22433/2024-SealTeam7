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
        [SerializeField] private NetworkObject weaponHolder;
        private Camera _playerCamera;
        private InputController _inputController;

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
            if (IsOwner) EquipWeapon(true);
            _playerCamera = Camera.main;
            _inputController = GetComponentInParent<InputController>();
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            if (!_equippedGun) return;
            
            HandleShooting();
            HandleAiming();
            HandleReload();
            HandleMelee();
            HandleWeaponSwap();
            HandleWeaponEquip();
        }

        private void HandleShooting()
        {
            switch (_equippedGun.fireMode)
            {
                case FireMode.SemiAutomatic:
                    if (_inputController.GetShootInputPressed())
                        _equippedGun.Shoot(_playerCamera.transform.position, _playerCamera.transform.forward);
                    break;
                case FireMode.Automatic:
                    if (_inputController.GetShootInputHeld())
                        _equippedGun.Shoot(_playerCamera.transform.position, _playerCamera.transform.forward);
                    break;
                // case FireMode.Burst:
                //     if (_inputController.GetShootInputPressed())
                //         _equippedGun.ServerBurstShoot();
                //     break;
            }
        }

        private void HandleAiming()
        {
            // if (_inputController.GetAimInputHeld()) _equippedGun.TryAim();
            // else _equippedGun.TryUnaim();
        }

        private void HandleReload()
        {
            if (_inputController.GetReloadInput()) _equippedGun.TryReload();
        }

        private void HandleMelee()
        {
            // if (_inputController.GetMeleeInput()) meleeWeapon.Attack();
        }
        
        private void HandleWeaponSwap()
        {
            if (_inputController.GetScrollSwapWeaponInput() != 0.0f || _inputController.GetSwapWeaponInput())
                EquipWeapon(_equippedGun == _secondaryGun);
        }

        private void HandleWeaponEquip()
        {
            if (_inputController.GetEquipPrimaryInput()) EquipWeapon(true);
            if (_inputController.GetEquipSecondaryInput()) EquipWeapon(false);
        }

        [Server]
        private void SpawnWeapons()
        {
            if (primaryGunPrefab == null || secondaryGunPrefab == null)
            {
                Debug.LogError("Weapon prefabs are not assigned!");
                return;
            }

            GameObject primaryInstance = Instantiate(primaryGunPrefab.gameObject);
            NetworkObject primaryNetworkObject = primaryInstance.GetComponent<NetworkObject>();
            primaryNetworkObject.SetParent(weaponHolder);
            primaryNetworkObject.transform.SetLocalPositionAndRotation(primaryGunPrefab.spawnPosition, primaryGunPrefab.spawnRotation);
            ServerManager.Spawn(primaryInstance, Owner);
            _primaryGun = primaryInstance.GetComponent<Gun>();

            GameObject secondaryInstance = Instantiate(secondaryGunPrefab.gameObject);
            NetworkObject secondaryNetworkObject = secondaryInstance.GetComponent<NetworkObject>();
            secondaryNetworkObject.SetParent(weaponHolder);
            secondaryNetworkObject.transform.SetLocalPositionAndRotation(secondaryGunPrefab.spawnPosition, secondaryGunPrefab.spawnRotation);
            ServerManager.Spawn(secondaryInstance, Owner);
            _secondaryGun = secondaryInstance.GetComponent<Gun>();

            _primaryGun.gameObject.SetActive(false);
            _secondaryGun.gameObject.SetActive(false);
            
            RpcSyncWeapons(_primaryGun, _secondaryGun);
        }
        
        private void EquipWeapon(bool usePrimary)
        {
            if (!IsOwner) return;

            ServerEquipWeapon(usePrimary);
        }

        [ServerRpc(RequireOwnership = true)]
        private void ServerEquipWeapon(bool usePrimary)
        {
            if (usePrimary)
            {
                if (_equippedGun != _primaryGun)
                {
                    _primaryGun.gameObject.SetActive(true);
                    _secondaryGun.gameObject.SetActive(false);
                    _equippedGun = _primaryGun;
                }
            }
            else
            {
                if (_equippedGun != _secondaryGun)
                {
                    _primaryGun.gameObject.SetActive(false);
                    _secondaryGun.gameObject.SetActive(true);
                    _equippedGun = _secondaryGun;
                }
            }
            RpcUpdateActiveWeapon(usePrimary);
        }

        [ObserversRpc]
        private void RpcUpdateActiveWeapon(bool isPrimaryActive)
        {
            if (_primaryGun) _primaryGun.gameObject.SetActive(isPrimaryActive);
            if (_secondaryGun) _secondaryGun.gameObject.SetActive(!isPrimaryActive);
            
            _equippedGun = isPrimaryActive ? _primaryGun : _secondaryGun;
        }

        [ObserversRpc(BufferLast = true)]
        private void RpcSyncWeapons(Gun primaryGun, Gun secondaryGun)
        {
            _primaryGun = primaryGun;
            _secondaryGun = secondaryGun;
        }

    }
}