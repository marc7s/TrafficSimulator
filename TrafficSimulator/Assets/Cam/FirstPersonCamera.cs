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

        // Store the current yaw and pitch values for the camera
        private float _yaw;
        private float _pitch;

        // Update the camera's rotation based on the input look direction
        public override void Look(Vector2 lookDirection)
        {
            /*print("Lookin... looki lookin");
            lookDirection *= Time.deltaTime * _mouseSensitivity;

            _yaw += lookDirection.x;
            _pitch -= lookDirection.y;
            _pitch = Mathf.Clamp(_pitch, -90f, 90f);

            CinemachinePOV pov = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
            if (pov != null)
            {
                pov.m_HorizontalAxis.Value = _yaw;
                pov.m_VerticalAxis.Value = _pitch;
            }*/
        }
        
        public override void SetActive(CameraManager cameraManager)
        {
            // Reset yaw and pitch values
            _yaw = 0;
            _pitch = 0;

            base.SetActive(cameraManager);
            UserSelectManager.Instance.CanSelectNewObject = false;

            // Get the first person pivot transform from the selected game object
            Transform firstPersonPivot = UserSelectManager.Instance.SelectedGameObject.
                GetComponent<CarSelectable>().FirstPersonPivot;

            // Set the camera's parent to the first person pivot
            transform.SetParent(firstPersonPivot);
            transform.position = firstPersonPivot.position;

            // Lock the cursor and hide it
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Immediately set POV values to 0
            CinemachinePOV pov = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
            if (pov != null)
            {
                pov.m_HorizontalAxis.Value = 0;
                pov.m_VerticalAxis.Value = 0;
            }

            // Start the coroutine to ensure POV values are reset at the end of the frame
            StartCoroutine(ResetPOVValues());
        }
    
        // Coroutine to reset POV values at the end of the frame
        private IEnumerator ResetPOVValues()
        {
            yield return new WaitForEndOfFrame();

            CinemachinePOV pov = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
            if (pov != null)
            {
                pov.m_HorizontalAxis.Value = 0;
                pov.m_VerticalAxis.Value = 0;
            }
        }

        // Set this camera as inactive and restore default settings
        public override void SetInactive(CameraManager cameraManager)
        {
            base.SetInactive(cameraManager);
            UserSelectManager.Instance.CanSelectNewObject = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Handle the escape input to toggle the previous camera
        public override void HandleEscapeInput()
        {
            CameraManager.ToggleThirdPersonCamera();
        }
    }
}