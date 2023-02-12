using System;
using Cam;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonScript : ControllableCamera
{

    [Range(0,1)][SerializeField] private float _rotateSpeed = 1f;
    private float _rotateDirection;
    
    private InputAction rotationInput;
    
    private void Awake()
    {
        SetupInputActions();
    }

    protected override void SetupInputActions()
    {
        rotationInput = UserInputManager.PlayerInputActions.Default.Rotate;
    }

    private void OnRotation(InputAction.CallbackContext ctx)
    {
        _rotateDirection = ctx.ReadValue<float>();
    }

    void Update()
    {
        followTransform.rotation *= Quaternion.AngleAxis(_rotateDirection * _rotateSpeed, Vector3.up);
    }

    public void SetAimTarget(Transform aimTarget)
    {
        followTransform = aimTarget;
    }
    
    protected override void OnActivation()
    {
        rotationInput.performed += OnRotation;
        rotationInput.canceled += OnRotation;
    }

    protected override void OnDeactivation()
    {
        rotationInput.performed -= OnRotation;
        rotationInput.canceled -= OnRotation;
        
        SetPriority(0);
    }
    
    private void OnEnable()
    {
        OnActivation();
    }

    private void OnDisable()
    {
        OnDeactivation();
    }
}
