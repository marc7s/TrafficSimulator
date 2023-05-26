using UnityEngine;

namespace RoadGenerator
{
    // Moves along a path at constant speed.
    // Depending on the end of path instruction, will either loop, reverse, or stop at the end of the path.
    public class DollyCamera : MonoBehaviour
    {
        public PathCreator pathCreator;
        public EndOfPathInstruction endOfPathInstruction;
        [Range(0.5f, 20)] public float Speed = 5;
        [Range(1, 200)] public float RotationSpeed = 5;
        public float UpwardOffset = 0;
        public float DownwardRotation = 0;
        public float RightRotation = 0;
        float distanceTravelled = 0;

        void Start()
        {
            if (pathCreator != null)
            {
                // Subscribed to the pathUpdated event so that we're notified if the path changes during the game
                pathCreator.pathUpdated += OnPathChanged;
                transform.position = GetNewPosition();
                transform.rotation = GetNewRotation();
            }
        }

        void Update()
        {
            if (pathCreator != null)
            {
                distanceTravelled += Speed * Time.deltaTime;
                transform.position = GetNewPosition();

                Quaternion currentRotation = transform.rotation;
                Quaternion targetRotation = GetNewRotation();
                transform.rotation = Quaternion.RotateTowards(transform.rotation, GetNewRotation(), RotationSpeed * Time.deltaTime);
            }
        }

        // If the path changes during the game, update the distance travelled so that the follower's position on the new path
        // is as close as possible to its position on the old path
        private void OnPathChanged()
        {
            distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        }

        private Vector3 GetNewPosition()
        {
            return pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction) + GetUpwardDirection() * UpwardOffset;
        }

        private Quaternion GetNewRotation()
        {
            Quaternion rotation = pathCreator.path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);

            Quaternion downOffset = Quaternion.Euler(0, -DownwardRotation, 0);
            Quaternion rightOffset = Quaternion.Euler(RightRotation, 0, 0);
            
            return rotation * Quaternion.Inverse(downOffset * rightOffset);
        }

        private Vector3 GetNormal()
        {
            return pathCreator.path.GetNormalAtDistance(distanceTravelled, endOfPathInstruction);
        }

        private Vector3 GetTangent()
        {
            return pathCreator.path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
        }

        private Vector3 GetUpwardDirection()
        {
            return Vector3.Cross(GetTangent(), GetNormal());
        }
    }
}