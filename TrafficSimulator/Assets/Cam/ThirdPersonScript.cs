using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonScript : MonoBehaviour, IAimable
{

    private float _rotateDirection;
    [Range(0,1)][SerializeField] private float _rotateSpeed = 1f;
    public Transform followTarget;
    
    private void OnRotate(InputValue value)
    {
        _rotateDirection = value.Get<float>();
        print(_rotateDirection);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        followTarget.rotation *= Quaternion.AngleAxis(_rotateDirection * _rotateSpeed, Vector3.up);
    }

    public void SetAimTarget(Transform aimTarget)
    {
        followTarget = aimTarget;
    }
}
