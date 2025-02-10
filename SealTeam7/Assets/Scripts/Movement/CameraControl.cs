using FishNet.Object;
using Input;
using UnityEngine;

namespace Movement
{
    public class CameraControl : NetworkBehaviour
    {

        [Header("Mouse Sensitivity")]
        [SerializeField] private float mouseSensitivityX;
        [SerializeField] private float mouseSensitivityY;
        
        [Header("Controller Sensitivity")]
        [SerializeField] private float controllerSensitivityX;
        [SerializeField] private float controllerSensitivityY;
        
        private InputController _inputController;

        public override void OnStartClient()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _inputController = GetComponentInParent<InputController>();
        }

        private void Update()
        {
            if (!IsOwner) return;
            
            // Look Input
            Vector2 lookInput = _inputController.GetLookInput();
            
            float sensitivityX = _inputController.IsUsingController() ? controllerSensitivityX : mouseSensitivityX;
            float sensitivityY = _inputController.IsUsingController() ? controllerSensitivityY : mouseSensitivityY;
            
            float x = lookInput.x * Time.deltaTime * sensitivityX;
            float y = -lookInput.y * Time.deltaTime * sensitivityY;

            // rotate the player
            transform.parent.Rotate(Vector3.up, x);
            // rotate the camera
            transform.Rotate(Vector3.right, y);
            
            Debug.Log(transform.rotation);
            
            // clamp
            transform.rotation = Quaternion.Euler(Mathf.Clamp(transform.rotation.eulerAngles.x, -90f, 90f), transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        }
    }
}
