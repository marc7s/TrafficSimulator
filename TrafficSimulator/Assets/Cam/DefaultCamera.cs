using System.Collections;
using Cinemachine;
using UnityEngine;
using User;

namespace Cam
{
    public class DefaultCamera : CameraState
    {
        [Header("Edge Scrolling Settings")]
        [SerializeField] private bool _enableEdgeScrolling;
        [Range(0, 0.3f)]
        private float _edgeScrollSensitivity = 0.1f;

        [Header("Movement Settings")]
        [SerializeField] private float _movementSpeed = 50f;
        [SerializeField] private float _rotationSpeed = 50f;

        [Header("Zoom Settings")]
        [SerializeField] private float _followOffsetMin = 5f;
        [SerializeField] private float _followOffsetMax = 50f;
        [SerializeField] private float _zoomLerpSpeed = 5f;
        [SerializeField] private float _zoomScrollFactor = 2f;

        [Header("Toggle Follow Settings")]
        [SerializeField] private float _togglePanSpeed = 0.3f;

        private GameObject _toggledGameObject;
        private bool _hasToggledGameObject;
        private Vector3 _mousePointDirection;
        private bool _isNearScreenBorder;
        private bool _isMovingTowardsTarget = false;
        private Vector3 _initialFollowOffset;
        private Vector3 _beforeSwitchFollowOffset;
        private CinemachineTransposer _cinemachineTransposer;

        private Vector3 FollowOffset
        {
            get => _cinemachineTransposer.m_FollowOffset;
            set => _cinemachineTransposer.m_FollowOffset = value;
        }

        private void Start()
        {
            _cinemachineTransposer = VirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            _initialFollowOffset = _cinemachineTransposer.m_FollowOffset;
        }

        private void Update()
        {
            if (_hasToggledGameObject && !_isMovingTowardsTarget) FollowTransform.position = _toggledGameObject.transform.position;
        }

        public override void SetActive(CameraManager cameraManager)
        {
            base.SetActive(cameraManager);
            FollowOffset = _initialFollowOffset;
            FollowOffset = Mathf.Approximately(_beforeSwitchFollowOffset.magnitude, 0f) ? _initialFollowOffset : _beforeSwitchFollowOffset;
            UserSelectManager.Instance.CanSelectNewObject = true;
            UserSelectManager.Instance.OnSelectedGameObject += HandleNewGameObjectSelection;
            UserSelectManager.Instance.OnDoubleClickedSelectedGameObject += HandleGameObjectDoubleClickSelection;
        }

        public override void SetInactive(CameraManager cameraManager)
        {
            _beforeSwitchFollowOffset = _cinemachineTransposer.m_FollowOffset;
            base.SetInactive(cameraManager);
            UserSelectManager.Instance.OnSelectedGameObject -= HandleNewGameObjectSelection;
            UserSelectManager.Instance.OnDoubleClickedSelectedGameObject -= HandleGameObjectDoubleClickSelection;
        }

        private void HandleNewGameObjectSelection(Selectable selectable)
        {
            StopAllCoroutines();
            if (selectable == null)
            {
                SetToggledGameObject();
                return;
            }
            SetToggledGameObject(selectable.gameObject);
            StartCoroutine(PanToTarget(selectable.transform));
        }

        private void HandleGameObjectDoubleClickSelection(Selectable selectable)
        {
            CameraManager.ToggleFocusCamera();
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
            if (_isNearScreenBorder) direction = _mousePointDirection.normalized;

            if (direction.sqrMagnitude != 0 && _hasToggledGameObject) SetToggledGameObject(null);
            Vector3 translatedDirection = TranslateDirectionToForward(direction.y, direction.x);
            FollowTransform.position += translatedDirection * _movementSpeed * Time.deltaTime;
        }

        public override void RotateHorizontal(float horizontalRotation)
        {
            FollowTransform.eulerAngles += new Vector3(0, horizontalRotation * _rotationSpeed * Time.deltaTime, 0);
        }
        
        public override void Zoom(float zoomValue)
        {
            Vector3 zoomDirection = FollowOffset.normalized;
    
            if (zoomValue > 0)
            {
                FollowOffset -= zoomDirection * _zoomScrollFactor;
            }
            else if (zoomValue < 0)
            {
                FollowOffset += zoomDirection * _zoomScrollFactor;
            }
            if (FollowOffset.magnitude < _followOffsetMin)
            {
                FollowOffset = zoomDirection * _followOffsetMin;
            }
            else if (FollowOffset.magnitude > _followOffsetMax)
            {
                FollowOffset = zoomDirection * _followOffsetMax;
            }
    
            _cinemachineTransposer.m_FollowOffset =
                Vector3.Lerp(_cinemachineTransposer.m_FollowOffset,
                    FollowOffset,
                    Time.deltaTime * _zoomLerpSpeed);
        }

        private void SetToggledGameObject(GameObject toggledGameObject = null)
        {
            _toggledGameObject = toggledGameObject;
            _hasToggledGameObject = toggledGameObject != null;
            _isMovingTowardsTarget = false;
        }

        private Vector3 TranslateDirectionToForward(float forwardScalar, float sidewaysScalar)
        {
            return FollowTransform.forward * forwardScalar + FollowTransform.right * sidewaysScalar;
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
