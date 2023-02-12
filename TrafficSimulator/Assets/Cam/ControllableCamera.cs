using Cinemachine;
using UnityEngine;

namespace Cam
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public abstract class ControllableCamera : MonoBehaviour
    {
        [SerializeField] public Transform followTransform;

        protected CinemachineVirtualCamera _cmVirtualCamera;
        public bool IsActive { get; private set; }

        private void OnEnable()
        {
            OnActivation();
        }

        private void OnDisable()
        {
            OnDeactivation();
        }

        protected abstract void SetupInputActions();

        protected abstract void OnActivation();

        protected abstract void OnDeactivation();

        public void SetPriority(int priority)
        {
            _cmVirtualCamera.Priority = priority;
        }

        public void SetFollowTransform(Transform aimTarget)
        {
            followTransform = aimTarget.transform;
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