using System.Numerics;
using Vector3 = UnityEngine.Vector3;

namespace Cam
{
    public class FirstPersonDriverCamera : CameraState
    {

        public override void Move(Vector3 movement)
        {
            transform.position = FollowTransform.position;
            transform.rotation = FollowTransform.rotation;
        }
    }
}