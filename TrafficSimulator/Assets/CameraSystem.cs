using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private float _movementSpeed = 50f;
    [SerializeField] private float _rotationSpeed = 50f;
    private Vector3 _moveDirection;
    private float _rotateDirection;

    // Should be set by a function of the current screen size
    private int _edgeScrollSize = 20;

    // Debugging 
    private Vector2 _playerMovementInput;
    private Vector2 _playerPointInput;

    private void Update()
    {
        transform.position += _moveDirection * (_movementSpeed * Time.deltaTime);
        transform.eulerAngles += new Vector3(0, _rotateDirection * _rotationSpeed * Time.deltaTime, 0);
    }

    private void OnMove(InputValue value)
    {
        _playerMovementInput = value.Get<Vector2>();
        _moveDirection = TranslateDirectionToForward(_playerMovementInput.y, _playerMovementInput.x);
    }

    private void OnRotate(InputValue value)
    {
        _rotateDirection = value.Get<float>();
    }

    private void OnPoint(InputValue value)
    {
        _playerPointInput = value.Get<Vector2>();
        float forward = 0;
        float sideways = 0;
        if (_playerPointInput.y < _edgeScrollSize)
        {
            forward = -1f;
        }
        else if (_playerPointInput.y > Screen.height - _edgeScrollSize)
        {
            forward = 1f;
        }

        if (_playerPointInput.x < _edgeScrollSize)
        {
            sideways = -1f;
        }
        else if (_playerPointInput.x > Screen.width - _edgeScrollSize)
        {
            sideways = 1f;
        }

        _moveDirection = TranslateDirectionToForward(forward, sideways);
    }

    private Vector3 TranslateDirectionToForward(float forward, float sideways)
    {
        return transform.forward * forward + transform.right * sideways;
    }
}