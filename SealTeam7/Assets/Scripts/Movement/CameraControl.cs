using Input;
using UnityEngine;

namespace Movement
{
    public class CameraControl : MonoBehaviour
    {

        [SerializeField] private float sensitivityX;
        [SerializeField] private float sensitivityY;
        [SerializeField] private Transform orientation;
        private float _xRotation;
        private float _yRotation;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Update is called once per frame
        private void Update()
        {
            // Look Input
            Vector2 lookInput = InputController.GetInstance().GetLookInput();
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
