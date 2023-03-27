using UnityEngine;
using UnityEngine.InputSystem;

namespace User
{
    /// <summary>
    ///     Manages user selection of objects in the scene.
    /// </summary>
    public class UserSelectManager : MonoBehaviour
    {
        /// <summary>
        ///     Delegate for handling double-clicked selected game object.
        /// </summary>
        /// <param name="newSelectedGameObject">The new selected game object.</param>
        public delegate void DoubleClickedSelectedGameObjectHandler(Selectable newSelectedGameObject);

        /// <summary>
        ///     Delegate for handling single-clicked selected game object.
        /// </summary>
        /// <param name="newSelectedGameObject">The new selected game object.</param>
        public delegate void SelectedGameObjectChangedHandler(Selectable newSelectedGameObject);

        private static UserSelectManager _instance;

        [HideInInspector] public bool CanSelectNewObject;

        private Camera _mainCamera;
        private InputAction _clickInput;
        private InputAction _doubleClickInput;
        private bool _hasSelectedGameObject;
        private InputAction _pointInput;

        /// <summary>
        ///     The currently selected game object.
        /// </summary>
        public Selectable SelectedGameObject { get; private set; }
        
        /// <summary>
        ///     Singleton instance of the UserSelectManager.
        /// </summary>
        public static UserSelectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UserSelectManager>();
                    if (_instance == null)
                    {
                        var obj = new GameObject();
                        obj.name = nameof(UserSelectManager);
                        _instance = obj.AddComponent<UserSelectManager>();
                        DontDestroyOnLoad(obj.transform.root);
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            InitializeSingletonInstance();
            _mainCamera = Camera.main;
            CanSelectNewObject = true;
        }

        private void Start()
        {
            SetupInputActions();
            SubscribeToInput();
        }

        private void InitializeSingletonInstance()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject.transform.root);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetupInputActions()
        {
            _clickInput = UserInputManager.PlayerInputActions.Default.Click;
            _doubleClickInput = UserInputManager.PlayerInputActions.Default.DoubleClick;
            _pointInput = UserInputManager.PlayerInputActions.Default.Point;
        }

        // Subscribe to input events
        private void SubscribeToInput()
        {
            _pointInput.performed += OnPointInput;
            _clickInput.performed += OnSingleClickInput;
            _doubleClickInput.performed += OnDoubleClickInput;
        }
        
        private void OnDisable()
        {
            UnsubscribeFromInput();
        }

        private void UnsubscribeFromInput()
        {
            _pointInput.performed -= OnPointInput;
            _clickInput.performed -= OnSingleClickInput;
            _doubleClickInput.performed -= OnDoubleClickInput;
        }

        private void OnSingleClickInput(InputAction.CallbackContext ctx)
        {
            OnClickInput(ctx, OnSelectedGameObject);
        }

        private void OnDoubleClickInput(InputAction.CallbackContext ctx)
        {
            OnClickInput(ctx, OnDoubleClickedSelectedGameObject);
        }

        /// <summary>
        ///     Event for handling double-clicked selected game object.
        /// </summary>
        public event SelectedGameObjectChangedHandler OnDoubleClickedSelectedGameObject;

        /// <summary>
        ///     Event for handling single-clicked selected game object.
        /// </summary>
        public event SelectedGameObjectChangedHandler OnSelectedGameObject;

        // Common method for handling click and double-click inputs
        private void OnClickInput(InputAction.CallbackContext ctx, SelectedGameObjectChangedHandler eventToInvoke)
        {
            var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(ray, out var hitInfo) && CanSelectNewObject)
            {
                SelectObjectFromClick(eventToInvoke, hitInfo);
            }
        }

        // Select the object based on the click event and invoke the corresponding event
        private void SelectObjectFromClick(SelectedGameObjectChangedHandler eventToInvoke, RaycastHit hitInfo)
        {
            Selectable hitSelectable = hitInfo.transform.GetComponent<Selectable>();
            if (hitSelectable != null)
            {
                if (hitSelectable.Equals(SelectedGameObject))
                {
                    eventToInvoke?.Invoke(SelectedGameObject);
                    return;
                }

                if (_hasSelectedGameObject)
                {
                    SelectedGameObject.Deselect();
                }

                hitSelectable.Select();
                SelectedGameObject = hitSelectable;
                _hasSelectedGameObject = true;

                eventToInvoke?.Invoke(SelectedGameObject);
            }
            else
            {
                DeselectCurrentObject();
            }
        }

        // Deselect the current object and invoke the OnSelectedGameObject event with null
        private void DeselectCurrentObject()
        {
            if (_hasSelectedGameObject)
            {
                SelectedGameObject.Deselect();
                SelectedGameObject = null;
                _hasSelectedGameObject = false;

                OnSelectedGameObject?.Invoke(null);
            }
        }

        // Method for handling point input, can be used for hover effects in the future
        private void OnPointInput(InputAction.CallbackContext obj)
        {
            // Can be used to trigger hover effects later on
        }
    }
}