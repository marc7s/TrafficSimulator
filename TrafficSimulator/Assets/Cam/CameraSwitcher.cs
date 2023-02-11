using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera[] _cameras;
    [SerializeField] private int _currentActiveCameraIndex = 0;
    private void Start()
    {
        foreach (CinemachineVirtualCamera cam in _cameras)
        {
            cam.Priority = 0;
        }
        _cameras[_currentActiveCameraIndex].Priority = 1;
    }

    void OnDebugButtonPressed(InputValue value)
    {
        SwitchPriority(1);
    }
    
    void DisableCamera(int i)
    {
        
    }

    private void SwitchPriority(int i)
    {
        if(_currentActiveCameraIndex == 0)
        {
            _cameras[1].Priority = 1;
            _cameras[0].Priority = 0;
            _currentActiveCameraIndex = 1;
        }
        else
        {
            _cameras[1].Priority = 0;
            _cameras[0].Priority = 1;
            _currentActiveCameraIndex = 0;
        }
    }
    
    
}