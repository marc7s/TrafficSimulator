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
        private static object _lock = new object();
        private static bool applicationIsQuitting;

        [HideInInspector] public bool CanSelectNewObject;

        private Camera _mainCamera;
        private InputAction _clickInput;
        private InputAction _doubleClickInput;
        private bool _hasSelectedGameObject;
        private InputAction _pointInput;
        public bool IsHoveringOldUIElement = false;
        public bool IsHoveringNewUIElement = false;

        public bool IsHoveringUIElement => IsHoveringOldUIElement || IsHoveringNewUIElement;

        /// <summary>
        ///     The currently selected game object.
        /// </summary>
        public Selectable SelectedGameObject { get; private set; }
        
        private Selectable _previousClickedSelectable;
        
        /// <summary>
        ///     Singleton instance of the UserSelectManager.
        /// </summary>
        public static UserSelectManager Instance
        {
            get
            {   
                if (applicationIsQuitting)
                    return null;
                
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<UserSelectManager>();
                        if (_instance == null)
                        {
                            GameObject obj = new GameObject();
                            obj.name = nameof(UserSelectManager);
                            _instance = obj.AddComponent<UserSelectManager>();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            applicationIsQuitting = false;
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
                _instance = this;
            else
                Destroy(gameObject);
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
            applicationIsQuitting = true;
        }

        private void UnsubscribeFromInput()
        {
            _pointInput.performed -= OnPointInput;
            _clickInput.performed -= OnSingleClickInput;
            _doubleClickInput.performed -= OnDoubleClickInput;
        }


        private void OnSingleClickInput(InputAction.CallbackContext ctx)
        {
            if(!IsHoveringUIElement)
            {
                _previousClickedSelectable = SelectedGameObject;
                OnClickInput(OnSelectedGameObject, true);
            }
        }

        private void OnDoubleClickInput(InputAction.CallbackContext ctx)
        {
            if(!IsHoveringUIElement)
            {
                if (_previousClickedSelectable == SelectedGameObject)
                    OnClickInput(OnDoubleClickedSelectedGameObject, false);
                else
                    _previousClickedSelectable = null;
            }
        }

        // ... (the rest of the code from the original script)
        /// <summary>
        ///     Event for handling double-clicked selected game object.
        /// </summary>
        public event SelectedGameObjectChangedHandler OnDoubleClickedSelectedGameObject;

        /// <summary>
        ///     Event for handling single-clicked selected game object.
        /// </summary>
        public event SelectedGameObjectChangedHandler OnSelectedGameObject;

        // Common method for handling click and double-click inputs
        private void OnClickInput(SelectedGameObjectChangedHandler eventToInvoke, bool deselectIfClickedAgain)
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(ray, out RaycastHit hitInfo) && CanSelectNewObject)
                SelectObjectFromClick(eventToInvoke, hitInfo, deselectIfClickedAgain);
        }

        // Select the object based on the click event and invoke the corresponding event
        private void SelectObjectFromClick(SelectedGameObjectChangedHandler eventToInvoke, RaycastHit hitInfo, bool deselectIfClickedAgain)
        {
            Selectable hitSelectable = hitInfo.transform.GetComponent<Selectable>();
            if (hitSelectable != null)
            {
                if (hitSelectable.Equals(SelectedGameObject))
                {
                    if(deselectIfClickedAgain)
                        DeselectCurrentObject();
                    else
                        eventToInvoke?.Invoke(SelectedGameObject);
                    
                    return;
                }

                if (_hasSelectedGameObject)
                    SelectedGameObject.Deselect();

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