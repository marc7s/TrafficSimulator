using UnityEngine;
using UnityEngine.InputSystem;

public class CinemachineSwitcher : MonoBehaviour
{
    [SerializeField] 
    private InputAction _camera1;
    [SerializeField] 
    private InputAction _camera2;
    [SerializeField] 
    private InputAction _camera_2D;
    [SerializeField] 
    private InputAction _camera_FPV;
    private Animator _anim;
    

    void Awake()
    {
        _anim = GetComponent<Animator>();
    }
    void Start()
    {
        _camera1.performed += _ => SwitchCamera(1);
        _camera2.performed += _ => SwitchCamera(2);
        _camera_2D.performed += _ => SwitchCamera(3);
        _camera_FPV.performed += _ => SwitchCamera(4);

    }

    public void OnEnable()
    {
        _camera1.Enable();
        _camera2.Enable();
        _camera_2D.Enable();
        _camera_FPV.Enable();
    }

    public void OnDisable()
    {
        _camera1.Disable();
        _camera2.Disable();
        _camera_2D.Disable();
        _camera_FPV.Disable();
    }

    // Plays animation and changes camera depending on the button pressed
    public void SwitchCamera(int camera)
    {
        switch (camera)
        {
            case 1:
                _anim.Play("FreeLookCamera");
                break;
            case 2:
                _anim.Play("FreeLookCamera2");
                break;
            case 3:
                _anim.Play("2DCamera");
                break;
            case 4:
                _anim.Play("FPVCamera");
                break;
        }
    }
}
