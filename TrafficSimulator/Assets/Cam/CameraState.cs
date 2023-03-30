using Cinemachine;
using UnityEngine;

namespace Cam
{
    /// <summary>
    /// The CameraState class is an abstract class that extends MonoBehaviour and defines the basic functionality for a camera state.
    /// It requires the CinemachineVirtualCamera component to be attached and provides methods for setting the state as active or inactive,
    /// setting the follow transform of the active camera, and handling virtual input methods. The virtual input methods
    /// include handling escape input, point input, click input, double-click input, movement, horizontal rotation, and zooming. These methods
    /// do nothing until they have been overriden. 
    /// </summary>
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public abstract class CameraState : MonoBehaviour
    {
        [SerializeField] protected Transform FollowTransform;
        protected CameraManager CameraManager;
        protected CinemachineVirtualCamera VirtualCamera;
        public bool IsDefault = false;
        
        protected virtual void Awake()
        {
            VirtualCamera = GetComponent<CinemachineVirtualCamera>();
            // Cinemachine is internally hooked up to the old input system
            if(VirtualCamera.GetCinemachineComponent<CinemachinePOV>())
            {
                DisableInput();
            }
            VirtualCamera.Priority = IsDefault ? 1 : 0;
        }

        /// <summary>
        /// Sets this camera to be the active camera.
        /// </summary>
        /// <param name="cameraManager">the <see cref="CameraManager"/> responsible for this camera</param>
        public virtual void SetActive(CameraManager cameraManager)
        {
            VirtualCamera.Priority = 1;
            CameraManager = cameraManager;
            FollowTransform = cameraManager.CameraTarget.transform;
            cameraManager.InputHandler.OnEscapePressed += HandleEscapeInput;
            cameraManager.InputHandler.OnSpacePressed += HandleSpaceInput;

        }

        /// <summary>
        /// Sets this camera to inactive. Will remain the active camera if no other camera is set to active.
        /// </summary>
        /// <param name="cameraManager">the <see cref="CameraManager"/> responsible for this camera</param>
        public virtual void SetInactive(CameraManager cameraManager)
        {
            VirtualCamera.Priority = 0;
            CameraManager = null;
            cameraManager.InputHandler.OnEscapePressed -= HandleEscapeInput;
            cameraManager.InputHandler.OnSpacePressed -= HandleSpaceInput;
        }
        
        /// <summary>
        /// The transform that the active camera follows. Should be passed between cameras upon a camera switch. 
        /// </summary>
        /// <param name="followTransform">transform that the active camera should follow</param>
        public void SetFollowTransform(Transform followTransform)
        {
            FollowTransform = followTransform;
        }

        private void DisableInput()
        {
            CinemachinePOV pov = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
            if (pov != null)
            {
                pov.m_HorizontalAxis.m_MaxSpeed = 0f;
                pov.m_VerticalAxis.m_MaxSpeed = 0f;
            }
        }


        #region Virtual Input Methods
        public virtual void HandleEscapeInput()
        {
            return;
        }

        public virtual void HandlePointInput(Vector2 pointPosition)
        {
            return;
        }
        
        public virtual void Move(Vector3 movement)
        {
            return;
        }

        public virtual void RotateHorizontal(float horizontalRotation)
        {
            return;
        }
        
        public virtual void Look(Vector2 lookDirection)
        {
            return;
        }

        public virtual void Zoom(float zoomValue)
        {
            return;
        }
        #endregion

        public virtual void HandleSpaceInput()
        {
            return;
        }
    }
}