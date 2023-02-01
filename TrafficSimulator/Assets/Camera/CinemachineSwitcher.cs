using UnityEngine;
using UnityEngine.InputSystem;

public class CinemachineSwitcher : MonoBehaviour
{
    [SerializeField] 
    private InputAction Camera1;
    [SerializeField] 
    private InputAction Camera2;
    [SerializeField] 
    private InputAction Camera_2D;
    [SerializeField] 
    private InputAction Camera_FPV;
    private Animator Anim;
    

    void Awake()
    {
        Anim = GetComponent<Animator>();
    }
    void Start()
    {
        Camera1.performed += _ => SwitchCamera(1);
        Camera2.performed += _ => SwitchCamera(2);
        Camera_2D.performed += _ => SwitchCamera(3);
        Camera_FPV.performed += _ => SwitchCamera(4);

    }

    public void OnEnable()
    {
        Camera1.Enable();
        Camera2.Enable();
        Camera_2D.Enable();
        Camera_FPV.Enable();
    }

    public void OnDisable()
    {
        Camera1.Disable();
        Camera2.Disable();
        Camera_2D.Disable();
        Camera_FPV.Disable();
    }

    // Plays animation and changes camera depending on the button pressed
    public void SwitchCamera(int Camera)
    {
        switch (Camera)
        {
            case 1:
                Anim.Play("FreeLookCamera");
                break;
            case 2:
                Anim.Play("FreeLookCamera2");
                break;
            case 3:
                Anim.Play("2DCamera");
                break;
            case 4:
                Anim.Play("FPVCamera");
                break;
        }
    }
}
