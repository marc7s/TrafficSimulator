using System.Numerics;
using Vector3 = UnityEngine.Vector3;

namespace Cam
{
    public class FirstPersonDriverCamera : CameraState
    {
        private void Start()
        {
            VirtualCamera.Priority = 100;
            VirtualCamera.Follow = FollowTransform;
        }

        public override void Move(Vector3 movement)
        {
            transform.position = FollowTransform.position;
        }
    }
}