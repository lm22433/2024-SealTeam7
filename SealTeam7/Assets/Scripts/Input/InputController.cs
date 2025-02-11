using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public enum InputType
    {
        KeyboardMouse,
        Xbox,
        Unknown
    }
    
    public class InputController : MonoBehaviour
    {
        private PlayerInputActions _playerInputActions;
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInputActions = new PlayerInputActions();
            _playerInputActions.Enable();

            _playerInput = GetComponent<PlayerInput>();
        }
        
        private InputType GetInputType()
        {
            return _playerInput.currentControlScheme switch
            {
                "Keyboard and Mouse" => InputType.KeyboardMouse,
                "Xbox Controller" => InputType.Xbox,
                _ => InputType.Unknown
            };
        }
        
        public bool IsUsingController()
        {
            return GetInputType() == InputType.Xbox;
        }
        
        // Common Input Methods

        public Vector2 GetMoveInput()
        {
            return _playerInputActions.Player.Move.ReadValue<Vector2>();
        }
        
        public Vector2 GetLookInput()
        {
            return _playerInputActions.Player.Look.ReadValue<Vector2>();
        }
        
        public bool GetJumpInput()
        {
            return _playerInputActions.Player.Jump.triggered;
        }
        
        public bool GetSprintInput()
        {
            return _playerInputActions.Player.Sprint.triggered;
        }

        public bool GetReloadInput()
        {
            return _playerInputActions.Player.Reload.triggered;
        }

        public bool GetMeleeInput()
        {
            return _playerInputActions.Player.Melee.triggered;
        }

        public bool GetCrouchInput()
        {
            return _playerInputActions.Player.Crouch.triggered;
        }

        public bool GetSwapWeaponInput()
        {
            return _playerInputActions.Player.SwapWeapon.triggered;
        }

        public bool GetAimInputHeld()
        {
            return _playerInputActions.Player.Aim.IsPressed();
        }
        
        public bool GetShootInputPressed()
        {
            return _playerInputActions.Player.Shoot.WasPressedThisFrame();
        }

        public bool GetShootInputHeld()
        {
            return _playerInputActions.Player.Shoot.IsPressed();
        }
        
        // Keyboard Only Inputs

        public bool GetEquipPrimaryInput()
        {
            return _playerInputActions.Player.EquipPrimary.triggered;
        }
        
        public bool GetEquipSecondaryInput()
        {
            return _playerInputActions.Player.EquipSecondary.triggered;
        }

        public float GetScrollSwapWeaponInput()
        {
            return _playerInputActions.Player.ScrollSwapWeapon.ReadValue<Vector2>().y;
        }
    }
}
