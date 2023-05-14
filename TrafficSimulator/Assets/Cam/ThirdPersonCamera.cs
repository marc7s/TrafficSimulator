using UnityEngine;
using User;
using Cinemachine;
using UnityEngine.InputSystem;

namespace Cam
{
    /// <summary>
    /// Represents a third-person camera state.  It allows the camera to rotate horizontally, zoom in and out, and handle
    /// the Escape key input to switch to the previous camera. The class has several configurable properties,
    /// including rotate speed, minimum and maximum zoom, and zoom speed.
    /// </summary>
    public class ThirdPersonCamera : CameraState
    {
        [SerializeField][Range(1, 30)] private float _rotationSpeedFactor = 10;

        private GameObject _toggledGameObject;
        private Quaternion _rotationOffset = Quaternion.identity;
        private Cinemachine3rdPersonFollow _cinemachineFollow;
        
        [Header("Zoom Settings")]
        [SerializeField] private float _shoulderOffsetMin = 5f;
        [SerializeField] private float _shoulderOffsetMax = 20f;
        [SerializeField] private float _zoomScrollFactor = 2f;
        [SerializeField] private float _zoomLerpSpeed = 5f;

        private Vector3 ShoulderOffset
        {
            get => _cinemachineFollow.ShoulderOffset;
            set => _cinemachineFollow.ShoulderOffset = value;
        }

        private void Start()
        {
            _cinemachineFollow = VirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        }

        private void Update()
        {
            // Only update if this is the active camera
            if(CameraManager == null)
                return;
            
            if (!IsRotating && FollowTransform != null && _toggledGameObject != null)
            {
                FollowTransform.position = _toggledGameObject.transform.position;
                FollowTransform.rotation = _toggledGameObject.transform.rotation * _rotationOffset;
            }
        }

        public override void SetActive(CameraManager cameraManager)
        {
            base.SetActive(cameraManager);
            UserSelectManager.Instance.CanSelectNewObject = false;
            UserSelectManager.Instance.OnSelectedGameObject += HandleNewGameObjectSelection;
            GameObject followObject = UserSelectManager.Instance.SelectedGameObject?.gameObject;
            
            if(followObject != null)
                _toggledGameObject = followObject;

            RotationOrigin = FollowTransform.rotation;
        }

        public override void SetInactive(CameraManager cameraManager)
        {
            base.SetInactive(cameraManager);
            UserSelectManager.Instance.OnSelectedGameObject -= HandleNewGameObjectSelection;
        }

        private void OnDisable()
        {
            if (UserSelectManager.Instance != null)
                SetInactive(CameraManager);
        }

        private void HandleNewGameObjectSelection(Selectable selectable)
        {   
            SetToggledGameObject(selectable?.gameObject);
        }

        private void SetToggledGameObject(GameObject toggledGameObject = null)
        {
            _toggledGameObject = toggledGameObject;
        }

        public override void Rotate(Vector2 mouseOrigin)
        {
            Quaternion newRotation = OrbitCameraRotation(FollowTransform.rotation, Mouse.current.position.ReadValue(), mouseOrigin, _rotationSpeedFactor / 30f, true);
            Quaternion vehicleRotation = _toggledGameObject.transform.rotation;

            Quaternion newRotationOffset = Quaternion.identity * Quaternion.Inverse(newRotation);
            Quaternion vehicleRotationOffset = Quaternion.identity * Quaternion.Inverse(vehicleRotation);

            _rotationOffset = vehicleRotationOffset * Quaternion.Inverse(newRotationOffset);
            
            FollowTransform.position = _toggledGameObject.transform.position;
            FollowTransform.rotation = vehicleRotation * _rotationOffset;
        }

        public override void Zoom(float zoomValue)
        {
            Vector3 zoomDirection = ShoulderOffset.normalized;
    
            if (zoomValue > 0)
                ShoulderOffset -= zoomDirection * _zoomScrollFactor;
            else if (zoomValue < 0)
                ShoulderOffset += zoomDirection * _zoomScrollFactor;
            
            // Clamp the shoulder offset to the min and max values
            ShoulderOffset = zoomDirection * Mathf.Clamp(ShoulderOffset.magnitude, _shoulderOffsetMin, _shoulderOffsetMax);
    
            _cinemachineFollow.ShoulderOffset =
                Vector3.Lerp(_cinemachineFollow.ShoulderOffset,
                    ShoulderOffset,
                    Time.deltaTime * _zoomLerpSpeed);
        }

        public override void HandleEscapeInput()
        {
            CameraManager.ToggleDefaultCamera();
        }

        public override void HandleSpaceInput()
        {
            if (UserSelectManager.Instance.SelectedGameObject.GetComponent<CarSelectable>() == null)
                return;
            
            CameraManager.ToggleFirstPersonDriverCamera();
        }
    }
}