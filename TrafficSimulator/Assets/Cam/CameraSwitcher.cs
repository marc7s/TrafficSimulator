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
        foreach (ControllableCamera cam in _cameras)
        {
            cam.CameraSwitcher = this;
        }
        _cameras[_currentActiveCameraIndex].GetComponent<ControllableCamera>().SetPriority(1);
        _cameras[_currentActiveCameraIndex].GetComponent<ControllableCamera>().SetActive(true);
    }

    public void ToggleThirdPersonCamera()
    {
        SwitchPriority(1);
    }
    
    private void SwitchPriority(int i)
    {
        _cameras[_currentActiveCameraIndex].SetPriority(0);
        _cameras[_currentActiveCameraIndex].OnDeactivation();
        _currentActiveCameraIndex = i;
        _cameras[_currentActiveCameraIndex].SetPriority(1);
        _cameras[_currentActiveCameraIndex].OnActivation();

        /*
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
        }*/
    }
    
    
    
    
}