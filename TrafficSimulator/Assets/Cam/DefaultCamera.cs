using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cam
{
    /// <summary>
    /// Represents the initial camera state. It provides functionality for moving, rotating,
    /// and zooming the camera, as well as handling input and toggling between transforms to follow.
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
        private float _edgeScrollSensitivity = 0.1f;
        private float _targetZoom = 50f;

        private Transform _toggledTransform;
        private bool _hasToggledTransform;
        
        private Vector3 _mousePointDirection;
        private bool _isNearScreenBorder;
        
        private bool _isMovingTowardsTarget = false;
        [SerializeField] private float _togglePanSpeed = 0.3f;

        protected override void Awake()
        {
            IsDefault = true;
            base.Awake();
        }
    
        private void Update()
        {
            if (_hasToggledTransform && !_isMovingTowardsTarget) FollowTransform.position = _toggledTransform.transform.position;
        }

        public override void HandlePointInput(Vector2 pointPosition)
        {
            if (!_enableEdgeScrolling) return;
            
            Vector2 viewportPosition = Camera.main.ScreenToViewportPoint(pointPosition);
            float horizontal = 0f;
            float vertical = 0f;
            
            if (viewportPosition.x < _edgeScrollSensitivity) horizontal = -1f;
            else if (viewportPosition.x > 1 - _edgeScrollSensitivity) horizontal = 1f;

            if (viewportPosition.y < _edgeScrollSensitivity) vertical = -1f;
            else if (viewportPosition.y > 1 - _edgeScrollSensitivity) vertical = 1f;
            
            _isNearScreenBorder = (horizontal != 0f) || (vertical != 0f);
            _mousePointDirection = new Vector3(horizontal, vertical);
        }

        public override void HandleClickInput(InputAction.CallbackContext ctx)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            // Important that the toggled GameOjbect is tagged with "Vehicle"
            if (Physics.Raycast(ray, out RaycastHit hitInfo) && hitInfo.transform.CompareTag("Vehicle"))
            {
                SetToggledTransform(hitInfo.collider.transform);
            }
            else
            {
                if(_hasToggledTransform) SetToggledTransformToNull();
            }
        }

        public override void HandleDoubleClickInput(InputAction.CallbackContext ctx)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hitInfo) && hitInfo.transform.Equals(_toggledTransform))
            {
                CameraManager.CameraTarget = FollowTransform;
                CameraManager.ToggleThirdPersonCamera();
            }
        }

        public override void Move(Vector3 direction)
        {
            // Ignore the input argument if the mouse is near the screen border
            if (_isNearScreenBorder) direction = _mousePointDirection.normalized;

            if (direction.sqrMagnitude != 0 && _hasToggledTransform) SetToggledTransformToNull();
            Vector3 translatedDirection = TranslateDirectionToForward(direction.y, direction.x);
            FollowTransform.position += translatedDirection * _movementSpeed * Time.deltaTime;
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
        
        private void SetToggledTransformToNull()
        {
            _toggledTransform.GetComponent<Outline>().enabled = false;
            _toggledTransform = null;
            _hasToggledTransform = false;
        }

        private void SetToggledTransform(Transform toggledTransform)
        {
            Outline outline = toggledTransform.GetComponent<Outline>();
            outline.enabled = true;
            _toggledTransform = toggledTransform;
            _hasToggledTransform = true;
            StartCoroutine(PanToTarget(toggledTransform.position));
        }
        
        private Vector3 TranslateDirectionToForward(float forwardScalar, float sidewaysScalar)
        {
            return FollowTransform.forward * forwardScalar + transform.right * sidewaysScalar;
        }

        private IEnumerator PanToTarget(Vector3 target)
        {
            _isMovingTowardsTarget = true;
            float startTime = Time.time;
            float journeyLength = Vector3.Distance(FollowTransform.position, target);

            while (Vector3.Distance(FollowTransform.position, target) > 0.01f && _hasToggledTransform)
            {
                float distCovered = (Time.time - startTime) * _togglePanSpeed;
                float fractionOfJourney = distCovered / journeyLength;
                FollowTransform.position = Vector3.Lerp(FollowTransform.position, target, fractionOfJourney);
                yield return null;
            }
            _isMovingTowardsTarget = false;
        }
    }
}