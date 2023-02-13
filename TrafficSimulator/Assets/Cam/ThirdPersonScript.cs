using Cam;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonScript : ControllableCamera
{
    [Range(0, 1)] [SerializeField] private float _rotateSpeed = 1f;
    private float _rotateDirection;

    private InputAction rotationInput;

    private void Awake()
    {
        _cmVirtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    private void Update()
    {
        //followTransform.rotation *= Quaternion.AngleAxis(_rotateDirection * _rotateSpeed, Vector3.up);
    }
    
    private void OnDisable()
    {
        OnDeactivation();
    }

    protected override void SetupInputActions()
    {
        rotationInput = UserInputManager.PlayerInputActions.Default.Rotate;
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
    }

    public override void OnDeactivation()
    {
        rotationInput.performed -= OnRotationInput;
        rotationInput.canceled -= OnRotationInput;
    }
}