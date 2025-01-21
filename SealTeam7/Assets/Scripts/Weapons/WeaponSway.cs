using UnityEngine;

namespace Weapons
{
    public class WeaponSway : MonoBehaviour
    {
        [Header("Weapon Sway Settings")] 
        [SerializeField] private float smooth;
        [SerializeField] private float swayMultiplier;

        private void Update()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * swayMultiplier;
            float mouseY = Input.GetAxisRaw("Mouse Y") * swayMultiplier;

            Quaternion rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
            Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);
            
            Quaternion targetRotation = rotationX * rotationY;
            
            
        }
    }
}