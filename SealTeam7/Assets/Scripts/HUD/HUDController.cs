using UnityEngine;
using UnityEngine.UI;
using Weapons;

namespace HUD
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private WeaponManager weaponManager;
    
        [SerializeField] private Image primaryWeaponIcon;
        [SerializeField] private Image secondaryWeaponIcon;

        [SerializeField] private Sprite unselectedWeaponIcon;
        [SerializeField] private Sprite selectedWeaponIcon;

        private void Update()
        {
            if (weaponManager.IsPrimaryWeaponEquipped())
            {
                primaryWeaponIcon.sprite = selectedWeaponIcon;
                secondaryWeaponIcon.sprite = unselectedWeaponIcon;
            }
        
            if (weaponManager.IsSecondaryWeaponEquipped())
            {
                primaryWeaponIcon.sprite = unselectedWeaponIcon;
                secondaryWeaponIcon.sprite = selectedWeaponIcon;
            }
        }
    }
}
