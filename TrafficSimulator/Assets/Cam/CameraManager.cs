using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cam
{
    /// <summary>
    /// State manager used to handle cameras. It allows switching between multiple camera states,
    /// with the current active camera defined by an index. The script also handles user input for camera movement, rotation, zoom, and other actions,
    /// and toggling between different cameras based on user input. Additionally, the script contains methods for setting up input actions and subscribing
    /// to user input events.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private CameraState[] _cameras;
        [SerializeField] private int _currentActiveCameraIndex = 0;
        [SerializeField] private int _previousActiveCameraIndex;
        [SerializeField] public Transform CameraTarget;
        private CameraState _currentActiveCamera;
        
        #region User Input
        private InputAction _movementInput;
        private InputAction _rotationInput;
        private InputAction _zoomInput;
        private InputAction _clickInput;
        private InputAction _doubleClickInput;
        private InputAction _pointInput;
        private InputAction _escapeInput;
        private InputAction _spaceInput;
    
        // Cached input values
        private Vector2 _movementFromUser;
        private float _rotationFromUser;
        private float _zoomFromUser;
   


        private void SetupInputActions()
        {
            _movementInput = UserInputManager.PlayerInputActions.Default.Move;
            _rotationInput = UserInputManager.PlayerInputActions.Default.Rotate;
            _zoomInput = UserInputManager.PlayerInputActions.Default.Zoom;
            _clickInput = UserInputManager.PlayerInputActions.Default.Click;
            _doubleClickInput = UserInputManager.PlayerInputActions.Default.DoubleClick;
            _pointInput = UserInputManager.PlayerInputActions.Default.Point;
            _escapeInput = UserInputManager.PlayerInputActions.Default.Escape;
            _spaceInput = UserInputManager.PlayerInputActions.Default.Space;
        }
        
        private void SubscribeToInput()
        {
            _movementInput.performed += OnMovementInput;
            _movementInput.canceled += OnMovementInput;
            _rotationInput.performed += OnRotationInput;
            _rotationInput.canceled += OnRotationInput;
            _zoomInput.performed += OnZoomInput;
            _zoomInput.canceled += OnZoomInput;
            
            _pointInput.performed += OnPointInput;
            _clickInput.performed += OnClickInput;
            _doubleClickInput.performed += OnDoubleClickInput;
            _escapeInput.performed += OnEscapeInput;
            _spaceInput.performed += OnSpaceInput;

        }

        private void OnSpaceInput(InputAction.CallbackContext ctx)
        {
            _currentActiveCamera.HandleSpaceInput();
        }

        private void OnMovementInput(InputAction.CallbackContext ctx)
        {
            _movementFromUser = ctx.ReadValue<Vector2>();
        }
    
        private void OnRotationInput(InputAction.CallbackContext ctx)
        {
            _rotationFromUser = ctx.ReadValue<float>();
        }
    
        private void OnZoomInput(InputAction.CallbackContext ctx)
        {
            _zoomFromUser = ctx.ReadValue<float>();
        }
    
        private void OnPointInput(InputAction.CallbackContext ctx)
        {
            _currentActiveCamera.HandlePointInput(ctx.ReadValue<Vector2>());
        }
    
        private void OnClickInput(InputAction.CallbackContext ctx)
        {
            _currentActiveCamera.HandleClickInput(ctx);
        }

        private void OnDoubleClickInput(InputAction.CallbackContext ctx)
        {
            _currentActiveCamera.HandleDoubleClickInput(ctx);
        }

        private void OnEscapeInput(InputAction.CallbackContext ctx)
        {
            _currentActiveCamera.HandleEscapeInput(ctx);
        }

        #endregion
    
        private void Update()
        {
            _currentActiveCamera.Move(_movementFromUser);
            _currentActiveCamera.RotateHorizontal(_rotationFromUser);
            _currentActiveCamera.Zoom(_zoomFromUser);
        }
    
        private void Awake()
        {
            _currentActiveCamera = _cameras[FindDefaultCameraIndex()];
            _currentActiveCamera.SetActive(this);
        }

        private void Start()
        {
            SetupInputActions();
            SubscribeToInput();
        }

        public void ToggleThirdPersonCamera()
        {
            SwitchActiveCamera(1);
        }
    
        public void TogglePreviousCamera()
        {
            SwitchActiveCamera(_previousActiveCameraIndex);
        }
    
        private void SwitchActiveCamera(int newIndex)
        {
            _previousActiveCameraIndex = _currentActiveCameraIndex;
            _currentActiveCameraIndex = newIndex;
        
            _cameras[_previousActiveCameraIndex].SetInactive(this);
            _cameras[_currentActiveCameraIndex].SetFollowTransform(CameraTarget);
            _cameras[_currentActiveCameraIndex].SetActive(this);
        }

        private int FindDefaultCameraIndex()
        {
            for (int i = 0; i < _cameras.Length; i++)
            {
                if (_cameras[i].IsDefault) return i;
            }

            // Set the first camera to default
            Debug.LogWarning("No default camera has been assigned!");
            return 0;
        }

        public void ToggleFirstPersonDriverCamera()
        {
            // Should move the responsibility of tracking state switches to each state. 
            SwitchActiveCamera(2);
        }
    }
}