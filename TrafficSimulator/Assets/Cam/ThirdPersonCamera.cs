using UnityEngine;
using User;

namespace Cam
{
    /// <summary>
    /// Represents a third-person camera state.  It allows the camera to rotate horizontally, zoom in and out, and handle
    /// the Escape key input to switch to the previous camera. The class has several configurable properties,
    /// including rotate speed, minimum and maximum zoom, and zoom speed.
    /// </summary>
    public class ThirdPersonCamera : CameraState
    {
        [SerializeField] private float _rotateSpeed = 1f;
        [SerializeField] private float _minZoom = 50f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _zoomSpeed = 1f;

        private float _targetZoom = 50f;

        public override void SetActive(CameraManager cameraManager)
        {
            base.SetActive(cameraManager);
            UserSelectManager.Instance.CanSelectNewObject = false;
        }

        public override void RotateHorizontal(float horizontalRotation)
        {
            FollowTransform.rotation *= Quaternion.AngleAxis(horizontalRotation * _rotateSpeed * Time.deltaTime, Vector3.up);
        }

        public override void Zoom(float zoomValue)
        {
            _targetZoom -= zoomValue;
            _targetZoom = Mathf.Clamp(_targetZoom, _maxZoom, _minZoom);
            VirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(VirtualCamera.m_Lens.FieldOfView,
                _targetZoom, Time.deltaTime * _zoomSpeed);
        }

        public override void HandleEscapeInput()
        {
            CameraManager.ToggleDefaultCamera();
        }

        public override void HandleSpaceInput()
        {
            if (UserSelectManager.Instance.SelectedGameObject.GetComponent<CarSelectable>() == null)
            {
                return;
            }
            CameraManager.ToggleFirstPersonDriverCamera();
        }
    }
}