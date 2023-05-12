using System.Collections;
using Cinemachine;
using UnityEngine;
using User;
using CameraState = Cam.CameraState;

namespace Cam
{
    /// <summary>
    /// This camera state represents a first-person view for a driver in a vehicle.
    /// </summary>
    public class FirstPersonCamera : CameraState
    {
        [SerializeField]
        private float _mouseSensitivity = 100f;

        private float _yaw;
        private float _pitch;

        private CinemachinePOV _pov;

        protected override void Awake()
        {
            base.Awake();
            _pov = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
            
            if (_pov == null)
                Debug.LogError("CinemachinePOV component not found on the Virtual Camera");
        }

        public override void Look(Vector2 lookDirection)
        {
            UpdateYawAndPitch(lookDirection);
            ApplyRotationToPOV();
        }

        private void UpdateYawAndPitch(Vector2 lookDirection)
        {
            lookDirection *= Time.deltaTime * _mouseSensitivity;

            _yaw += lookDirection.x;
            _pitch -= lookDirection.y;
            _pitch = Mathf.Clamp(_pitch, -90f, 90f);
        }

        private void ApplyRotationToPOV()
        {
            if (_pov != null)
            {
                _pov.m_HorizontalAxis.Value = _yaw;
                _pov.m_VerticalAxis.Value = _pitch;
            }
        }

        public override void SetActive(CameraManager cameraManager)
        {
            ResetYawAndPitch();

            base.SetActive(cameraManager);

            UserSelectManager.Instance.CanSelectNewObject = false;

            SetCameraParentToFirstPersonPivot();

            LockAndHideCursor();

            ResetPOVValues();
        }

        private void ResetYawAndPitch()
        {
            _yaw = 0;
            _pitch = 0;
        }

        private void SetCameraParentToFirstPersonPivot()
        {
            Transform firstPersonPivot = UserSelectManager.Instance.SelectedGameObject.GetComponent<CarSelectable>().FirstPersonPivot;

            transform.SetParent(firstPersonPivot);
            transform.position = firstPersonPivot.position;
        }

        private void LockAndHideCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ResetPOVValues()
        {
            if (_pov != null)
            {
                _pov.m_HorizontalAxis.Value = 0;
                _pov.m_VerticalAxis.Value = 0;
            }
        }

        public override void SetInactive(CameraManager cameraManager)
        {
            base.SetInactive(cameraManager);

            UserSelectManager.Instance.CanSelectNewObject = true;

            UnlockAndShowCursor();
        }

        private void UnlockAndShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public override void HandleEscapeInput()
        {
            CameraManager.ToggleFocusCamera();
        }
    }
}
