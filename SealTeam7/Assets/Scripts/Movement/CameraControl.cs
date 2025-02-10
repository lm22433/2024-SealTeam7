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
        private float _xRotation;
        private float _yRotation;

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
            float y = lookInput.y * Time.deltaTime * sensitivityY;
            
            
            _yRotation += x;

            _xRotation -= y;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

            if (_yRotation > 360f) {
                _yRotation -= 360f;
            }

            if (_yRotation < -360f) {
                _yRotation += 360f;
            }

            // rotate the player
            transform.parent.rotation = Quaternion.Euler(0f, _yRotation, 0f);
            // rotate the camera
            transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
        }
    }
}
