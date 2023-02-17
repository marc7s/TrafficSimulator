using UnityEngine;

namespace RoadGenerator
{
    // Moves along a lane at constant speed.
    // Depending on the end of path instruction, will either loop, reverse, or stop at the end of the path.
    [RequireComponent(typeof(Renderer))]
    public class LaneFollower : MonoBehaviour
    {
        [Header("Lane selection")]
        public Road Road;
        public int LaneIndex;
        
        [Header("Follower settings")]
        public EndOfPathInstruction endOfPathInstruction;
        public float speed = 15;
        float distanceTravelled;

        private Lane _lane;
        private float _height = 0;

        void Start() {
            if (Road != null)
            {
                // If the road has not updated yet there will be no lanes, so update them first
                if(Road.Lanes.Count == 0)
                {
                    Road.OnChange();
                }
                
                // Check that the provided lane index is valid
                if(LaneIndex < 0 || LaneIndex >= Road.Lanes.Count)
                {
                    Debug.LogError("Lane index out of range");
                    return;
                }
                
                // Get the height of the object to offset it so it follows on top of the lane
                _height = GetComponent<Renderer>().bounds.size.y;
                
                // Get the lane from the road
                _lane = Road.Lanes[LaneIndex];
            }
        }

        void Update()
        {
            if (_lane != null)
            {
                distanceTravelled += speed * Time.deltaTime;
                transform.position = _lane.GetPositionAtDistance(distanceTravelled, endOfPathInstruction) + _height / 2 * Vector3.up;
                transform.rotation = _lane.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);
            }
        }
    }
}