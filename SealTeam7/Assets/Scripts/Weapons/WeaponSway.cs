using Player.Input;
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
            Vector2 lookInput = InputController.GetInstance().GetLookInput();
            float x = lookInput.x * swayMultiplier;
            float y = lookInput.y * swayMultiplier;

            Quaternion rotationX = Quaternion.AngleAxis(-y, Vector3.right);
            Quaternion rotationY = Quaternion.AngleAxis(x, Vector3.up);
            
            Quaternion targetRotation = rotationX * rotationY;
            
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);
        }
    }
}