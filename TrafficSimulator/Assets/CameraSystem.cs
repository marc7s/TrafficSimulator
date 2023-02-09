using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private float _speed = 50f;
    private Vector3 _moveDirection;


    // Debugging 
    public Vector2 PlayerMovementInput { get; private set; }

    private void Update()
    {
        transform.position += _moveDirection * (_speed * Time.deltaTime);
    }

    private void OnMove(InputValue value)
    {
        PlayerMovementInput = value.Get<Vector2>();
        _moveDirection = transform.forward * PlayerMovementInput.y + transform.right * PlayerMovementInput.x;
    }
}