using System.Collections;
using UnityEngine;
using User;

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

        private GameObject _toggledGameObject;
        private bool _hasToggledGameObject;
        
        private Vector3 _mousePointDirection;
        private bool _isNearScreenBorder;
        
        private bool _isMovingTowardsTarget = false;
        [SerializeField] private float _togglePanSpeed = 0.3f;
        
        private void Update()
        {
            if (_hasToggledGameObject && !_isMovingTowardsTarget) FollowTransform.position = _toggledGameObject.transform.position;
        }
        
        public override void SetActive(CameraManager cameraManager)
        {
            base.SetActive(cameraManager);
            UserSelectManager.Instance.CanSelectNewObject = true;
            UserSelectManager.Instance.OnSelectedGameObject += HandleNewGameObjectSelection;
            UserSelectManager.Instance.OnDoubleClickedSelectedGameObject += HandleGameObjectDoubleClickSelection;
        }

        public override void SetInactive(CameraManager cameraManager)
        {
            base.SetInactive(cameraManager);
            UserSelectManager.Instance.OnSelectedGameObject -= HandleNewGameObjectSelection;
            UserSelectManager.Instance.OnDoubleClickedSelectedGameObject -= HandleGameObjectDoubleClickSelection;
        }

        private void HandleNewGameObjectSelection(Selectable selectable)
        {
            StopAllCoroutines();
            if(selectable == null)
            {
                SetToggledGameObjectToNull();
                return;
            }
            SetToggledGameObject(selectable.gameObject);
            StartCoroutine(PanToTarget(selectable.transform));
        }
        
        private void HandleGameObjectDoubleClickSelection(Selectable selectable)
        {
            CameraManager.ToggleThirdPersonCamera();
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
        

        public override void Move(Vector3 direction)
        {
            // Ignore the input argument if the mouse is near the screen border
            if (_isNearScreenBorder) direction = _mousePointDirection.normalized;

            if (direction.sqrMagnitude != 0 && _hasToggledGameObject) SetToggledGameObjectToNull();
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
        
        private void SetToggledGameObjectToNull()
        {
            _toggledGameObject = null;
            _hasToggledGameObject = false;
            _isMovingTowardsTarget = false;
        }

        private void SetToggledGameObject(GameObject toggledGameObject)
        {
            _toggledGameObject = toggledGameObject;
            _hasToggledGameObject = true;
            StartCoroutine(PanToTarget(toggledGameObject.transform));
        }
        
        private Vector3 TranslateDirectionToForward(float forwardScalar, float sidewaysScalar)
        {
            return FollowTransform.forward * forwardScalar + transform.right * sidewaysScalar;
        }

        private IEnumerator PanToTarget(Transform target)
        {
            _isMovingTowardsTarget = true;
            float startTime = Time.time;
            float journeyLength = Vector3.Distance(FollowTransform.position, target.position);

            while (Vector3.Distance(FollowTransform.position, target.position) > 0.01f && _hasToggledGameObject)
            {
                float distCovered = (Time.time - startTime) * _togglePanSpeed;
                float fractionOfJourney = distCovered / journeyLength;
                FollowTransform.position = Vector3.Lerp(FollowTransform.position, target.position, fractionOfJourney);
                yield return null;
            }
            _isMovingTowardsTarget = false;
        }

    }
}