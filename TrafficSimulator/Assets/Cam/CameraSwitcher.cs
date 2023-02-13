using System;
using Cam;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private ControllableCamera[] _cameras;
    [SerializeField] private int _currentActiveCameraIndex = 0;
    [SerializeField] private int _previousActiveCameraIndex;
    [SerializeField] private Transform cameraTarget;
    
    
    private void Start()
    {
        foreach (ControllableCamera cam in _cameras)
        {
            cam.CameraSwitcher = this;
        }
        _cameras[_currentActiveCameraIndex].GetComponent<ControllableCamera>().SetFollowTransform(cameraTarget);
        _cameras[_currentActiveCameraIndex].GetComponent<ControllableCamera>().SetPriority(1);
        _cameras[_currentActiveCameraIndex].GetComponent<ControllableCamera>().SetActive(true);
    }

    public void ToggleThirdPersonCamera()
    {
        SwitchPriority(1);
        
    }
    
    public void TogglePreviousCamera()
    {
        SwitchPriority(_previousActiveCameraIndex);
    }
    
    private void SwitchPriority(int i)
    {
        _previousActiveCameraIndex = _currentActiveCameraIndex;
        
        _cameras[_previousActiveCameraIndex].SetPriority(0);
        _cameras[_previousActiveCameraIndex].OnDeactivation();
        _currentActiveCameraIndex = i;
        _cameras[_currentActiveCameraIndex].SetPriority(1);
        _cameras[_currentActiveCameraIndex].SetFollowTransform(cameraTarget);
        _cameras[_currentActiveCameraIndex].OnActivation();
    }
    
    
    
    
}