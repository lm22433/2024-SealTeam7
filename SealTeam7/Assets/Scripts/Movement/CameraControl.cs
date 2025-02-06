using Input;
using UnityEngine;

namespace Movement
{
    public class CameraControl : MonoBehaviour
    {

        [Header("Mouse Sensitivity")]
        [SerializeField] private float mouseSensitivityX;
        [SerializeField] private float mouseSensitivityY;
        
        [Header("Controller Sensitivity")]
        [SerializeField] private float controllerSensitivityX;
        [SerializeField] private float controllerSensitivityY;
        
        [SerializeField] private Transform orientation;
        
        private float _xRotation;
        private float _yRotation;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // Look Input
            Vector2 lookInput = InputController.GetInstance().GetLookInput();
            
            float sensitivityX = InputController.GetInstance().IsUsingController() ? controllerSensitivityX : mouseSensitivityX;
            float sensitivityY = InputController.GetInstance().IsUsingController() ? controllerSensitivityY : mouseSensitivityY;
            
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

            //rotate the camera
            transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
            orientation.rotation = Quaternion.Euler(0, _yRotation, 0);
        }
    }
}
