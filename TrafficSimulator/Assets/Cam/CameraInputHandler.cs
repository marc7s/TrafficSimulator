using System;
using UnityEngine;
using UnityEngine.InputSystem;
using User;

namespace Cam
{
    /// <summary>
    /// Handles camera input events.
    /// </summary>
    public class CameraInputHandler : IDisposable
    {
        public Vector2 Movement { get; private set; }
        public float Rotation { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public float Zoom { get; private set; }

        private InputAction _movementInput;
        private InputAction _rotationInput;
        private InputAction _lookDeltaInput;
        private InputAction _zoomInput;
        private InputAction _escapeInput;
        private InputAction _spaceInput;
        
        /// <summary>
        /// Occurs when the Escape key is pressed.
        /// </summary>
        public event Action OnEscapePressed;
        
        /// <summary>
        /// Occurs when the Space key is pressed.
        /// </summary>
        public event Action OnSpacePressed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraInputHandler"/> class.
        /// </summary>
        public CameraInputHandler()
        {
            SetupInputActions();
            SubscribeToInput();
        }

        private void SetupInputActions()
        {
            _movementInput = UserInputManager.PlayerInputActions.Default.Move;
            _rotationInput = UserInputManager.PlayerInputActions.Default.Rotate;
            _lookDeltaInput = UserInputManager.PlayerInputActions.Default.MouseLookDirection;
            _zoomInput = UserInputManager.PlayerInputActions.Default.Zoom;
            _escapeInput = UserInputManager.PlayerInputActions.Default.Escape;
            _spaceInput = UserInputManager.PlayerInputActions.Default.Space;
        }

        private void SubscribeToInput()
        {
            _movementInput.performed += OnMovementInput;
            _movementInput.canceled += OnMovementInput;
            _rotationInput.performed += OnRotationInput;
            _rotationInput.canceled += OnRotationInput;
            _lookDeltaInput.performed += OnLookDeltaInput;
            _lookDeltaInput.canceled += OnLookDeltaInput;
            _zoomInput.performed += OnZoomInput;
            _zoomInput.canceled += OnZoomInput;
            _escapeInput.performed += OnEscapeInput;
            _spaceInput.performed += OnSpaceInput;
        }

        private void OnEscapeInput(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                OnEscapePressed?.Invoke();
            }
        }

        private void OnSpaceInput(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                OnSpacePressed?.Invoke();
            }
        }

        private void OnMovementInput(InputAction.CallbackContext ctx)
        {
            Movement = ctx.ReadValue<Vector2>();
        }

        private void OnRotationInput(InputAction.CallbackContext ctx)
        {
            Rotation = ctx.ReadValue<float>();
        }

        private void OnLookDeltaInput(InputAction.CallbackContext ctx)
        {
            LookDelta = ctx.ReadValue<Vector2>();
        }

        private void OnZoomInput(InputAction.CallbackContext ctx)
        {
            Zoom = ctx.ReadValue<float>();
        }

        /// <summary>
        /// Disposes the <see cref="CameraInputHandler"/> instance and unsubscribes from input events.
        /// </summary>
        public void Dispose()
        {
            _movementInput.performed -= OnMovementInput;
            _movementInput.canceled -= OnMovementInput;
            _rotationInput.performed -= OnRotationInput;
            _rotationInput.canceled -= OnRotationInput;
            _zoomInput.performed -= OnZoomInput;
            _zoomInput.canceled -= OnZoomInput;
            _lookDeltaInput.performed -= OnLookDeltaInput;
            _lookDeltaInput.canceled -= OnLookDeltaInput;
            _escapeInput.performed -= OnEscapeInput;
            _spaceInput.performed -= OnSpaceInput;
        }
    }
}
