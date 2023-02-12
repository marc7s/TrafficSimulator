using System;
using Cam;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private ControllableCamera[] _cameras;
    [SerializeField] private int _currentActiveCameraIndex = 0;
    
    private void Start()
    {
        _cameras[_currentActiveCameraIndex].GetComponent<ControllableCamera>().SetPriority(1);
        _cameras[_currentActiveCameraIndex].GetComponent<ControllableCamera>().SetActive(true);
        
    }

    void OnDebugButtonPressed(InputValue value)
    {
        SwitchPriority(1);
    }
    
    private void SwitchPriority(int i)
    {
        if(_currentActiveCameraIndex == 0)
        {
            _cameras[1].SetActive(true);
            _cameras[0].SetActive(false);
            _currentActiveCameraIndex = 1;
        }
        else
        {
            _cameras[1].SetActive(false); 
            _cameras[0].SetActive(true);
            _currentActiveCameraIndex = 0;
        }
    }
    
    
}