using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private float _movementSpeed = 50f;
    [SerializeField] private float _rotationSpeed = 50f;
    private Vector3 _moveDirection;
    private float _rotateDirection;


    // Debugging 
    public Vector2 PlayerMovementInput { get; private set; }
    
    private void Update()
    {
        transform.position += _moveDirection * (_movementSpeed * Time.deltaTime);
        transform.eulerAngles += new Vector3(0, _rotateDirection * _rotationSpeed * Time.deltaTime, 0);
    }

    private void OnMove(InputValue value)
    {
        PlayerMovementInput = value.Get<Vector2>();
        _moveDirection = transform.forward * PlayerMovementInput.y + transform.right * PlayerMovementInput.x;
    }

    private void OnRotate(InputValue value)
    {
        _rotateDirection = value.Get<float>();
    }
}