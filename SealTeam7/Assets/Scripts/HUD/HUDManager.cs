using UnityEngine;
using UnityEngine.UI;
using Weapons;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private WeaponManager weaponManager;
    
    [SerializeField] private Image primaryWeaponIcon;
    [SerializeField] private Image secondaryWeaponIcon;

    [SerializeField] private Sprite unselectedWeaponIcon;
    [SerializeField] private Sprite selectedWeaponIcon;
    
    void Start()
    {
        
    }

    void Update()
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
