using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cam
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public abstract class ControllableCamera : MonoBehaviour
    {
        [SerializeField] public Transform FollowTransform;
        public CameraSwitcher CameraSwitcher;

        protected CinemachineVirtualCamera _cmVirtualCamera;
        public bool IsActive { get; private set; }

        protected abstract void SetupInputActions();

        public abstract void OnActivation();

        public abstract void OnDeactivation();

        public void SetPriority(int priority)
        {
            _cmVirtualCamera.Priority = priority;
        }

        public void SetFollowTransform(Transform aimTarget)
        {
            FollowTransform = aimTarget.transform;
        }
        
        public void SetActive(bool isActive)
        {
            if (IsActive == isActive) return;
            IsActive = isActive;
            if (isActive) OnActivation();
            else OnDeactivation();
        }
    }
}