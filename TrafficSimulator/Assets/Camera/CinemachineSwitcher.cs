using UnityEngine;
using UnityEngine.InputSystem;

public class CinemachineSwitcher : MonoBehaviour
{
    [SerializeField] 
    private InputAction camera1;
    [SerializeField] 
    private InputAction camera2;
    [SerializeField] 
    private InputAction camera_2D;
    [SerializeField] 
    private InputAction camera_FPV;
    private Animator animator;
    

    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    void Start()
    {
        camera1.performed += _ => SwitchCamera(1);
        camera2.performed += _ => SwitchCamera(2);
        camera_2D.performed += _ => SwitchCamera(3);
        camera_FPV.performed += _ => SwitchCamera(4);

    }

    public void OnEnable()
    {
        camera1.Enable();
        camera2.Enable();
        camera_2D.Enable();
        camera_FPV.Enable();
    }

    public void OnDisable()
    {
        camera1.Disable();
        camera2.Disable();
        camera_2D.Disable();
        camera_FPV.Disable();
    }

    // Plays animation and changes camera depending on the button pressed
    public void SwitchCamera(int camera)
    {
        switch (camera)
        {
            case 1:
                animator.Play("FreeLookCamera");
                break;
            case 2:
                animator.Play("FreeLookCamera2");
                break;
            case 3:
                animator.Play("2DCamera");
                break;
            case 4:
                animator.Play("FPVCamera");
                break;
        }
    }
}
