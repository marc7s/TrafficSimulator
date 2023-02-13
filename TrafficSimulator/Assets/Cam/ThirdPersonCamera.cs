using Cam;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : ControllableCamera
{
    [Range(0, 1)] [SerializeField] private float _rotateSpeed = 1f;

    [SerializeField] private float _minZoom = 50f;
    [SerializeField] private float _maxZoom = 10f;
    [SerializeField] private float _zoomSpeed = 1f;
    
    private float _playerZoomInput;
    private float _rotateDirection;
    private float _targetZoom = 50f;
    
    private InputAction _rotationInput;
    private InputAction _zoomInput;
    private InputAction _escapeInput;

    private void Awake()
    {
        _cmVirtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    private void Update()
    {
        if (followTransform != null)
            followTransform.rotation *= Quaternion.AngleAxis(_rotateDirection * _rotateSpeed, Vector3.up);
        HandleZoom();
    }

    private void OnEnable()
    {
        SetupInputActions();
    }

    private void OnDisable()
    {
        OnDeactivation();
    }

    private void HandleZoom()
    {
        _targetZoom -= _playerZoomInput;
        _targetZoom = Mathf.Clamp(_targetZoom, _maxZoom, _minZoom);
        _cmVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(_cmVirtualCamera.m_Lens.FieldOfView,
            _targetZoom, Time.deltaTime * _zoomSpeed);
    }
    
    protected override void SetupInputActions()
    {
        _rotationInput = UserInputManager.PlayerInputActions.Default.Rotate;
        _zoomInput = UserInputManager.PlayerInputActions.Default.Zoom;
        _escapeInput = UserInputManager.PlayerInputActions.Default.Escape;
    }

    private void OnRotationInput(InputAction.CallbackContext ctx)
    {
        _rotateDirection = ctx.ReadValue<float>();
    }

    private void OnZoomInput(InputAction.CallbackContext ctx)
    {
        _playerZoomInput = Mathf.Clamp(ctx.ReadValue<float>(), -1f, 1f);
    }


    private void OnEscapeInput(InputAction.CallbackContext obj)
    {
        CameraSwitcher.TogglePreviousCamera();
    }

    public override void OnActivation()
    {
        _rotationInput.performed += OnRotationInput;
        _rotationInput.canceled += OnRotationInput;
        _zoomInput.performed += OnZoomInput;
        _zoomInput.canceled += OnZoomInput;
        _escapeInput.performed += OnEscapeInput;
    }

    public override void OnDeactivation()
    {
        _rotationInput.performed -= OnRotationInput;
        _rotationInput.canceled -= OnRotationInput;
    }
}