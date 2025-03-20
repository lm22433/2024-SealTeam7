using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    public class HUDController : MonoBehaviour
    {
        // [SerializeField] private WeaponManager weaponManager;
        
        [SerializeField] private Image primaryWeaponBackground;
        [SerializeField] private Image secondaryWeaponBackground;

        [SerializeField] private RectTransform primaryWeaponIcon;
        [SerializeField] private RectTransform secondaryWeaponIcon;

        [SerializeField] private Sprite unselectedWeaponIcon;
        [SerializeField] private Sprite selectedWeaponIcon;

        // private void Start()
        // {
        //     primaryWeaponIcon.GetComponent<Image>().sprite = weaponManager.primaryWeapon.displaySprite;
        //     Debug.Log(weaponManager.primaryWeapon.spritePosition);
        //     primaryWeaponIcon.localPosition = weaponManager.primaryWeapon.spritePosition;
        //     primaryWeaponIcon.sizeDelta = weaponManager.primaryWeapon.spriteScale;
        //     
        //     secondaryWeaponIcon.GetComponent<Image>().sprite = weaponManager.secondaryWeapon.displaySprite;
        //     secondaryWeaponIcon.localPosition = weaponManager.secondaryWeapon.spritePosition;
        //     secondaryWeaponIcon.sizeDelta = weaponManager.secondaryWeapon.spriteScale;
        // }
        //
        // private void Update()
        // {
        //     if (weaponManager.IsPrimaryWeaponEquipped())
        //     {
        //         primaryWeaponBackground.sprite = selectedWeaponIcon;
        //         secondaryWeaponBackground.sprite = unselectedWeaponIcon;
        //     }
        //
        //     if (weaponManager.IsSecondaryWeaponEquipped())
        //     {
        //         primaryWeaponBackground.sprite = unselectedWeaponIcon;
        //         secondaryWeaponBackground.sprite = selectedWeaponIcon;
        //     }
        // }
    }
}
