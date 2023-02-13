using Cam;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class DefaultCamera : ControllableCamera
{
    [SerializeField] private bool _enableEdgeScrolling;
    [SerializeField] private float _movementSpeed = 50f;
    [SerializeField] private float _rotationSpeed = 50f;
    [SerializeField] private float _minZoom = 40f;
    [SerializeField] private float _maxZoom = 10f;
    [SerializeField] private float _zoomSpeed = 5f;
    
    // Should be set by a function of the current screen size
    private const int _edgeScrollSize = 20;
    private float _targetZoom = 50f;

    private GameObject _toggledGameObject;

    #region Input Fields
    private InputAction _clickInput;
    private InputAction _doubleClickInput;
    private InputAction _movementInput;
    private InputAction _pointInput;
    private InputAction _rotationInput;
    private InputAction _zoomInput;
    
    private Vector2 _playerPoint;
    private float _playerZoom;
    private float _rotateDirection;
    private Vector3 _moveDirection;
    #endregion
    
    private void Awake()
    {
        _cmVirtualCamera = GetComponent<CinemachineVirtualCamera>();
        SetPriority(1);
    }

    private void Start()
    {
        SetupInputActions();
    }

    private void Update()
    {
        if (_toggledGameObject != null) FollowTransform.position = _toggledGameObject.transform.position;

        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    protected override void SetupInputActions()
    {
        _movementInput = UserInputManager.PlayerInputActions.Default.Move;
        _rotationInput = UserInputManager.PlayerInputActions.Default.Rotate;
        _zoomInput = UserInputManager.PlayerInputActions.Default.Zoom;
        _clickInput = UserInputManager.PlayerInputActions.Default.Click;
        _doubleClickInput = UserInputManager.PlayerInputActions.Default.DoubleClick;
        _pointInput = UserInputManager.PlayerInputActions.Default.Point;
    }

    public override void OnActivation()
    {
        _movementInput.performed += OnMovementInput;
        _movementInput.canceled += OnMovementInput;

        _rotationInput.performed += OnRotationInput;
        _rotationInput.canceled += OnRotationInput;

        _zoomInput.performed += OnZoomInput;
        _zoomInput.canceled += OnZoomInput;

        _clickInput.performed += OnClickInput;
        _doubleClickInput.performed += OnDoubleClickInput;

        _pointInput.performed += OnPointInput;
    }

    private void OnPointInput(InputAction.CallbackContext ctx)
    {
        if (!_enableEdgeScrolling) return;

        _playerPoint = ctx.ReadValue<Vector2>();
        float forward = 0;
        float sideways = 0;
        if (_playerPoint.y < _edgeScrollSize)
            forward = -1f;
        else if (_playerPoint.y > Screen.height - _edgeScrollSize) forward = 1f;

        if (_playerPoint.x < _edgeScrollSize)
            sideways = -1f;
        else if (_playerPoint.x > Screen.width - _edgeScrollSize) sideways = 1f;

        _moveDirection = TranslateDirectionToForward(forward, sideways);
    }

    private void OnRotationInput(InputAction.CallbackContext ctx)
    {
        _rotateDirection = ctx.ReadValue<float>();
    }

    private void OnDoubleClickInput(InputAction.CallbackContext obj)
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hitInfo) && hitInfo.transform.gameObject.Equals(_toggledGameObject))
        {
            OnDeactivation();
            CameraSwitcher.ToggleThirdPersonCamera();
        }
    }

    private void OnClickInput(InputAction.CallbackContext ctx)
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hitInfo) && hitInfo.transform.CompareTag("Vehicle"))
        {
            _toggledGameObject = hitInfo.collider.gameObject;
        }
        else
        {
            _toggledGameObject = null;
        }
    }

    public override void OnDeactivation()
    {
        _movementInput.performed -= OnMovementInput;
        _movementInput.canceled -= OnMovementInput;

        _rotationInput.performed -= OnRotationInput;
        _rotationInput.canceled -= OnRotationInput;

        _zoomInput.performed -= OnZoomInput;
        _zoomInput.canceled -= OnZoomInput;

        _clickInput.performed -= OnClickInput;
        _pointInput.performed -= OnPointInput;
        _doubleClickInput.performed -= OnDoubleClickInput;
    }

    private void OnZoomInput(InputAction.CallbackContext ctx)
    {
        _playerZoom = Mathf.Clamp(ctx.ReadValue<float>(), -1f, 1f);
    }

    private void OnMovementInput(InputAction.CallbackContext ctx)
    {
        _toggledGameObject = null;
        _moveDirection = ctx.ReadValue<Vector2>();
        _moveDirection = TranslateDirectionToForward(_moveDirection.y, _moveDirection.x);
    }

    private void HandleMovement()
    {
        FollowTransform.position += _moveDirection * (_movementSpeed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        FollowTransform.eulerAngles += new Vector3(0, _rotateDirection * _rotationSpeed * Time.deltaTime, 0);
    }

    private void HandleZoom()
    {
        _targetZoom -= _playerZoom;
        _targetZoom = Mathf.Clamp(_targetZoom, _maxZoom, _minZoom);
        _cmVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(_cmVirtualCamera.m_Lens.FieldOfView,
            _targetZoom, Time.deltaTime * _zoomSpeed);
    }

    private Vector3 TranslateDirectionToForward(float forwardScalar, float sidewaysScalar)
    {
        return FollowTransform.forward * forwardScalar + transform.right * sidewaysScalar;
    }
}