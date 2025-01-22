using UnityEngine;

namespace Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Weapons")]
        public GunWeapon primaryWeapon;
        public GunWeapon secondaryWeapon;
        public MeleeWeapon meleeWeapon;

        private Weapon _currentWeapon;
        private GameObject _currentWeaponInstance;
        
        [Header("References")]
        [SerializeField] private Transform weaponHolder;
    
        private void Start()
        {
            EquipWeapon(primaryWeapon);
        }

        public void EquipWeapon(Weapon weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("Attempted to equip a null weapon. This should never happen.");
                return;
            }

            if (weapon == _currentWeapon)
            {
                return;
            }

            if (_currentWeaponInstance != null)
            {
                Destroy(_currentWeaponInstance);
            }

            // Instantiate the new weapon model
            _currentWeaponInstance = Instantiate(weapon.weaponModel, weaponHolder);
            _currentWeaponInstance.transform.localPosition = weapon.spawnPosition;
            _currentWeaponInstance.transform.localEulerAngles = weapon.spawnRotation;

            _currentWeapon = weapon;
            
            // Initialize gun-specific features
            if (_currentWeapon is GunWeapon gun)
            {
                WeaponInstance instance = _currentWeaponInstance.GetComponent<WeaponInstance>();
                if (instance == null)
                {
                    Debug.LogError("Weapon model prefab is missing WeaponInstance component!");
                    return;
                }
                gun.Initialize(instance);
            }
        }


        private void Update()
        {
            switch (_currentWeapon)
            {
                case GunWeapon gun:
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        gun.TryReload();
                    }

                    if (gun.isAutomatic)
                    {
                        if (Input.GetButton("Fire1"))
                        {
                            gun.Attack();
                        }
                    }
                    else
                    {
                        if (Input.GetButtonDown("Fire1"))
                        {
                            gun.Attack();
                        }
                    }

                    break;
                }
                case MeleeWeapon melee:
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        melee.Attack();
                    }

                    break;
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                EquipWeapon(primaryWeapon);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                EquipWeapon(secondaryWeapon);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                EquipWeapon(meleeWeapon);
            }
        }
    }
}
