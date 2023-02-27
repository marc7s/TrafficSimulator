using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cam
{
    public class UserPointerManager : MonoBehaviour
    {
        private static UserPointerManager _instance;
        private InputAction _clickInput;
        private InputAction _doubleClickInput;
        private bool _hasToggledGameObject;
        private InputAction _pointInput;
        private GameObject _toggledGameObject;

        public static UserPointerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UserPointerManager>();
                    if (_instance == null)
                    {
                        var obj = new GameObject();
                        obj.name = nameof(UserPointerManager);
                        _instance = obj.AddComponent<UserPointerManager>();
                        DontDestroyOnLoad(obj);
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupInputActions();
            SubscribeToInput();
        }

        private void SubscribeToInput()
        {
            _pointInput.performed += OnPointInput;
            _clickInput.performed += OnClickInput;
            _doubleClickInput.performed += OnDoubleClickInput;
        }

        
        public static event Action OnDoubleClickOnToggledGameObject;
        public static event Action<GameObject> OnDoubleClickOnNewTogglableObject;
        
        private void OnDoubleClickInput(InputAction.CallbackContext obj)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hitInfo) && hitInfo.transform.CompareTag("Vehicle"))
            {
                // If double clicked on the already toggled GameObject
                if(hitInfo.transform.gameObject.Equals(_toggledGameObject))
                {
                    OnDoubleClickOnToggledGameObject?.Invoke();
                }
                else
                {
                    OnDoubleClickOnNewTogglableObject?.Invoke(hitInfo.transform.gameObject);
                }
            }
        }

        private void OnClickInput(InputAction.CallbackContext obj)
        {
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            // TODO: Introduce an interface instead of using tags if deemed necessary in the future. Probably a lot more
            // than cars should be able to be toggled
            if (Physics.Raycast(ray, out var hitInfo) && hitInfo.transform.CompareTag("Vehicle"))
            {
                SetToggledGameObject(hitInfo.transform.gameObject);
            }
            else
            {
                if (_hasToggledGameObject) SetToggledGameObjectToNull();
            }
        }

        public static event Action OnToggledGameObjectSetToNull;

        private void SetToggledGameObjectToNull()
        {
            _toggledGameObject = null;
            _hasToggledGameObject = false;
            OnToggledGameObjectSetToNull?.Invoke();
        }

        public static event Action<GameObject> OnToggledGameObjectChanged;

        private void SetToggledGameObject(GameObject go)
        {
            _toggledGameObject = go;
            _hasToggledGameObject = true;
            OnToggledGameObjectChanged?.Invoke(go);
        }


        private void OnPointInput(InputAction.CallbackContext obj)
        {
        }


        private void SetupInputActions()
        {
            _clickInput = UserInputManager.PlayerInputActions.Default.Click;
            _doubleClickInput = UserInputManager.PlayerInputActions.Default.DoubleClick;
            _pointInput = UserInputManager.PlayerInputActions.Default.Point;
        }
    }
}