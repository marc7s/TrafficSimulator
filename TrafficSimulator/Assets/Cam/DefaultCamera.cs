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
    private readonly int _edgeScrollSize = 20;

    private Vector3 _moveDirection;
    private Vector2 _playerMovementInput;
    private Vector2 _playerPointInput;
    private float _playerZoomInput;
    private float _rotateDirection;
    private float _targetZoom = 50f;

    private GameObject _toggledGameObject;

    private InputAction _clickInput;
    private InputAction _doubleClickInput;
    private InputAction _movementInput;
    private InputAction _pointInput;
    private InputAction _rotationInput;
    private InputAction _zoomInput;

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
        if (!IsActive) return;
        if (_toggledGameObject != null) followTransform.position = _toggledGameObject.transform.position;

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

        _playerPointInput = ctx.ReadValue<Vector2>();
        float forward = 0;
        float sideways = 0;
        if (_playerPointInput.y < _edgeScrollSize)
            forward = -1f;
        else if (_playerPointInput.y > Screen.height - _edgeScrollSize) forward = 1f;

        if (_playerPointInput.x < _edgeScrollSize)
            sideways = -1f;
        else if (_playerPointInput.x > Screen.width - _edgeScrollSize) sideways = 1f;

        _moveDirection = TranslateDirectionToForward(forward, sideways);
    }

    private void OnRotationInput(InputAction.CallbackContext ctx)
    {
        _rotateDirection = ctx.ReadValue<float>();
    }

    private void OnDoubleClickInput(InputAction.CallbackContext obj)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
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
            Debug.Log("Object detected: " + hitInfo.transform.name);
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
        _playerZoomInput = Mathf.Clamp(ctx.ReadValue<float>(), -1f, 1f);
    }

    private void OnMovementInput(InputAction.CallbackContext ctx)
    {
        _toggledGameObject = null;
        _moveDirection = ctx.ReadValue<Vector2>();
        _moveDirection = TranslateDirectionToForward(_moveDirection.y, _moveDirection.x);
    }

    private void HandleMovement()
    {
        followTransform.position += _moveDirection * (_movementSpeed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        followTransform.eulerAngles += new Vector3(0, _rotateDirection * _rotationSpeed * Time.deltaTime, 0);
    }

    private void HandleZoom()
    {
        _targetZoom -= _playerZoomInput;
        _targetZoom = Mathf.Clamp(_targetZoom, _maxZoom, _minZoom);
        _cmVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(_cmVirtualCamera.m_Lens.FieldOfView,
            _targetZoom, Time.deltaTime * _zoomSpeed);
    }

    private Vector3 TranslateDirectionToForward(float forwardScalar, float sidewaysScalar)
    {
        return followTransform.forward * forwardScalar + transform.right * sidewaysScalar;
    }
}