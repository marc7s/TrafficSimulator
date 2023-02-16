using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cam
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public abstract class CameraState : MonoBehaviour
    {
        [SerializeField] protected Transform FollowTransform;
        protected CameraSwitcher CameraSwitcher;
        private CinemachineVirtualCamera _virtualCamera;
        public bool IsDefault = false;
        
        protected void Awake()
        {
            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            _virtualCamera.Priority = IsDefault ? 1 : 0;
        }

        public void SetActive(CameraSwitcher cameraSwitcher)
        {
            _virtualCamera.Priority = 1;
            CameraSwitcher = cameraSwitcher;
            FollowTransform = cameraSwitcher.CameraTarget;
        }

        public abstract void HandlePointInput(Vector2 pointPosition);
        public abstract void HandleClickInput(InputAction.CallbackContext ctx);
        public abstract void HandleDoubleClickInput(InputAction.CallbackContext ctx);
        public abstract void Move(Vector3 movement);
        public abstract void RotateHorizontal(float horizontalRotation);
        public abstract void Zoom(float zoomValue);
        public abstract void SetInactive(CameraSwitcher cameraSwitcher);
    }
}