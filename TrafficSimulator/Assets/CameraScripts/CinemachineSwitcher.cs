using UnityEngine;
using UnityEngine.InputSystem;

public class CinemachineSwitcher : MonoBehaviour
{
    [SerializeField] 
    private InputAction camera1;
    [SerializeField] 
    private InputAction camera2;
    [SerializeField] 
    private InputAction camera3;
    [SerializeField] 
    private InputAction camera4;

    private Animator animator;
    private bool freeLookCamera = true;
    private int activeCamera = 1;
    

    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    void Start()
    {
        //action.performed += _ => SwitchCamera();
        camera1.performed += _ => SwitchCameraTemp(1);
        camera2.performed += _ => SwitchCameraTemp(2);
        camera3.performed += _ => SwitchCameraTemp(3);
        camera4.performed += _ => SwitchCameraTemp(4);

    }

    private void OnEnable()
    {
        //action.Enable();
        camera1.Enable();
        camera2.Enable();
        camera3.Enable();
        camera4.Enable();
    }

    private void OnDisable()
    {
        //action.Disable();
        camera1.Disable();
        camera2.Disable();
        camera3.Disable();
        camera4.Disable();
    }

    private void SwitchCamera()
    {
        if (freeLookCamera)
        {
            animator.Play("FreeLookCamera2");
        }
        else
        {
            animator.Play("FreeLookCamera");
        }
        freeLookCamera = !freeLookCamera;
    }

    private void SwitchCameraTemp(int camera)
    {
        switch (camera)
        {
            case 1:
                animator.Play("FreeLookCamera");
                activeCamera = 1;
                break;
            case 2:
                animator.Play("FreeLookCamera2");
                activeCamera = 2;
                break;
            case 3:
                animator.Play("2DCamera");
                activeCamera = 3;
                break;
            case 4:
                animator.Play("FPVCamera");
                activeCamera = 4;
                break;
        }
    }
}
