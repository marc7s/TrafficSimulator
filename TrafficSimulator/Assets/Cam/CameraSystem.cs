using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private bool _enableEdgeScrolling;
    [SerializeField] private float _movementSpeed = 50f;
    [SerializeField] private float _rotationSpeed = 50f;
    [SerializeField] private float _minZoom = 40f;
    [SerializeField] private float _maxZoom = 10f;
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;

    // Should be set by a function of the current screen size
    private readonly int _edgeScrollSize = 20;
    private Vector3 _moveDirection;

    private Vector2 _playerMovementInput;
    private Vector2 _playerPointInput;
    private float _playerZoomInput;
    private float _rotateDirection;

    private float _targetZoom = 50f;
    
    public Transform followTarget;
    public Transform cameraAim;
    private void Update()
    {
        if(followTarget != null)
        {
            cameraAim.transform.position = followTarget.position;
        }
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void HandleMovement()
    {
        cameraAim.transform.position += _moveDirection * (_movementSpeed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        cameraAim.transform.eulerAngles += new Vector3(0, _rotateDirection * _rotationSpeed * Time.deltaTime, 0);
    }

    private void HandleZoom()
    {
        _targetZoom -= _playerZoomInput;

        _targetZoom = Mathf.Clamp(_targetZoom, _maxZoom, _minZoom);
        _cinemachineVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(_cinemachineVirtualCamera.m_Lens.FieldOfView,
            _targetZoom, Time.deltaTime * _zoomSpeed);
    }

    private void OnMove(InputValue value)
    {
        followTarget = null;
        _playerMovementInput = value.Get<Vector2>();
        _moveDirection = TranslateDirectionToForward(_playerMovementInput.y, _playerMovementInput.x);
    }

    private void OnRotate(InputValue value)
    {
        _rotateDirection = value.Get<float>();
    }

    private void OnPoint(InputValue value)
    {
        if (!_enableEdgeScrolling) return;

        _playerPointInput = value.Get<Vector2>();
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
    
    private void OnClick(InputValue value)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if(Physics.Raycast(ray, out RaycastHit hitInfo) && hitInfo.transform.CompareTag("Vehicle"))
        {
            followTarget = hitInfo.transform;
            Debug.Log("Object detected: " + hitInfo.transform.name);
        }
        else
        {
            if(followTarget != null)
            {
                cameraAim.transform.position = followTarget.position;
                followTarget = null;
            }

        }
    }

    private void OnZoom(InputValue value)
    {
        _playerZoomInput = Mathf.Clamp(value.Get<float>(), -1f, 1f);
    }

    private Vector3 TranslateDirectionToForward(float forwardScalar, float sidewaysScalar)
    {
        return cameraAim.transform.forward * forwardScalar + transform.right * sidewaysScalar;
    }
}