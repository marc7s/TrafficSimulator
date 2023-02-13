using Cam;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : ControllableCamera
{
    [Range(0, 1)] [SerializeField] private float _rotateSpeed = 1f;
    private float _rotateDirection;

    private InputAction rotationInput;
    private InputAction escapeInput;
    private InputAction zoomInput;

    private void Awake()
    {
        _cmVirtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    private void Update()
    {
        if(followTransform != null)
        followTransform.rotation *= Quaternion.AngleAxis(_rotateDirection * _rotateSpeed, Vector3.up);
        HandleZoom();
    }
    
    private void OnDisable()
    {
        OnDeactivation();
    }
    
    
    [SerializeField] private float _minZoom = 40f;
    [SerializeField] private float _maxZoom = 10f;
    private float _playerZoomInput;
    [SerializeField] private float _zoomSpeed = 1f;
    private float _targetZoom = 50f;

    private void HandleZoom()
    {
        _targetZoom -= _playerZoomInput;
        _targetZoom = Mathf.Clamp(_targetZoom, _maxZoom, _minZoom);
        _cmVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(_cmVirtualCamera.m_Lens.FieldOfView,
            _targetZoom, Time.deltaTime * _zoomSpeed);
    }
    
    private void OnZoomInput(InputAction.CallbackContext ctx)
    {
        _playerZoomInput = Mathf.Clamp(ctx.ReadValue<float>(), -1f, 1f);
    }

    protected override void SetupInputActions()
    {
        rotationInput = UserInputManager.PlayerInputActions.Default.Rotate;
        zoomInput = UserInputManager.PlayerInputActions.Default.Zoom;
        escapeInput = UserInputManager.PlayerInputActions.Default.Escape;
    }

    private void OnRotationInput(InputAction.CallbackContext ctx)
    {
        print("hi");
        _rotateDirection = ctx.ReadValue<float>();
    }

    public override void OnActivation()
    {
        SetupInputActions();
        rotationInput.performed += OnRotationInput;
        rotationInput.canceled += OnRotationInput;

        zoomInput.performed += OnZoomInput;
        zoomInput.canceled += OnZoomInput;

        escapeInput.performed += OnEscapeInput;
    }

    private void OnEscapeInput(InputAction.CallbackContext obj)
    {
        CameraSwitcher.TogglePreviousCamera();
    }

    public override void OnDeactivation()
    {
        rotationInput.performed -= OnRotationInput;
        rotationInput.canceled -= OnRotationInput;
    }
}