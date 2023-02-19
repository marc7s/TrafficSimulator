using UnityEngine;
using UnityEngine.InputSystem;

namespace Cam
{
    /// <summary>
    /// Represents the initial camera state. It provides functionality for moving, rotating,
    /// and zooming the camera, as well as handling input and toggling between objects.
    /// It also has a number of serialized fields for configuring movement and zoom speed,
    /// minimum and maximum zoom, and edge scrolling
    /// behavior (which is not yet implemented).
    /// </summary>
    public class DefaultCamera : CameraState
    {
        // Edge Scrolling is not implemented yet!
        [SerializeField] private bool _enableEdgeScrolling;
        [SerializeField] private float _movementSpeed = 50f;
        [SerializeField] private float _rotationSpeed = 50f;
        [SerializeField] private float _minZoom = 40f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _zoomSpeed = 5f;
    
        // Should be set by a function of the current screen size
        [Range(0,0.3f)]
        private float _edgeScrollSensitivty = 0.1f;
        private float _targetZoom = 50f;

        private GameObject _toggledGameObject;
        private bool _hasToggledGameObject;
        
        private Vector3 _mousePointDirection;
        private bool _isNearScreenBorder;

        protected override void Awake()
        {
            IsDefault = true;
            base.Awake();
        }
    
        private void Update()
        {
            if (_hasToggledGameObject) FollowTransform.position = _toggledGameObject.transform.position;
        }

        public override void HandlePointInput(Vector2 pointPosition)
        {
            if (!_enableEdgeScrolling) return;
            
            Vector2 viewportPosition = Camera.main.ScreenToViewportPoint(pointPosition);
            float horizontal = 0f;
            float vertical = 0f;
            
            print(viewportPosition);
            if (viewportPosition.x < _edgeScrollSensitivty) horizontal = -1f;
            else if (viewportPosition.x > 1 - _edgeScrollSensitivty) horizontal = 1f;

            if (viewportPosition.y < _edgeScrollSensitivty) vertical = -1f;
            else if (viewportPosition.y > 1 - _edgeScrollSensitivty) vertical = 1f;
            
            _isNearScreenBorder = (horizontal != 0f) || (vertical != 0f);
            _mousePointDirection = new Vector3(horizontal, vertical);
        }

        public override void HandleClickInput(InputAction.CallbackContext ctx)
        {
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hitInfo) && hitInfo.transform.CompareTag("Vehicle"))
            {
                SetToggledGameObject(hitInfo.collider.gameObject);
            }
            else
            {
                if(_hasToggledGameObject) SetToggledGameObjectToNull();
            }
        }

        public override void HandleDoubleClickInput(InputAction.CallbackContext ctx)
        {
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hitInfo) && hitInfo.transform.gameObject.Equals(_toggledGameObject))
            {
                CameraManager.CameraTarget = FollowTransform;
                CameraManager.ToggleThirdPersonCamera();
            }
        }

        public override void Move(Vector3 direction)
        {
            // Ignore the input argument if the mouse is near the screen border
            if (_isNearScreenBorder) direction = _mousePointDirection.normalized;

            if (direction.sqrMagnitude != 0 && _hasToggledGameObject) SetToggledGameObjectToNull();
            Vector3 translatedDirection = TranslateDirectionToForward(direction.y, direction.x);
            FollowTransform.position += translatedDirection * (_movementSpeed * Time.deltaTime);
        }

        public override void RotateHorizontal(float horizontalRotation)
        {
            FollowTransform.eulerAngles += new Vector3(0, horizontalRotation * _rotationSpeed * Time.deltaTime, 0);
        }

        public override void Zoom(float zoomValue)
        {
            _targetZoom -= zoomValue;
            _targetZoom = Mathf.Clamp(_targetZoom, _maxZoom, _minZoom);
            VirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(VirtualCamera.m_Lens.FieldOfView,
                _targetZoom, Time.deltaTime * _zoomSpeed);
        }
        
        private void SetToggledGameObjectToNull()
        {
            _toggledGameObject.GetComponent<Outline>().enabled = false;
            _toggledGameObject = null;
            _hasToggledGameObject = false;
        }

        private void SetToggledGameObject(GameObject toggledGameObject)
        {
            Outline outline = toggledGameObject.GetComponent<Outline>();
            outline.enabled = true;
            _toggledGameObject = toggledGameObject;
            _hasToggledGameObject = true;
        }
        
        private Vector3 TranslateDirectionToForward(float forwardScalar, float sidewaysScalar)
        {
            return FollowTransform.forward * forwardScalar + transform.right * sidewaysScalar;
        }
    }
}