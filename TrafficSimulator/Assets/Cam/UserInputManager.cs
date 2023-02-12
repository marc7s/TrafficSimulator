using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;

public class UserInputManager : MonoBehaviour
{
    public static UserInputManager Instance { get; private set; }
    private PlayerInputActions _playerInputActions;
    public static PlayerInputActions PlayerInputActions => Instance._playerInputActions;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        _playerInputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _playerInputActions.Default.Enable();
    }
}
