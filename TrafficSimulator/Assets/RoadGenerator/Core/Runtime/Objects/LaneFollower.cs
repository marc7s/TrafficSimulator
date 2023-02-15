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

        private VertexPath _path;
        private float _height = 0;

        void Start() {
            if (Road != null)
            {
                // If the road has not updated yet there will be no lanes, so update them first
                if(Road.Lanes.Count == 0)
                {
                    Road.UpdateLanes();
                }
                
                // Check that the provided lane index is valid
                if(LaneIndex < 0 || LaneIndex >= Road.Lanes.Count)
                {
                    Debug.LogError("Lane index out of range");
                    return;
                }
                
                // Get the height of the object to offset it so it follows on top of the lane
                Bounds bounds = GetComponent<Renderer>().bounds;
                _height = bounds.size.y;
                
                _path = Road.Lanes[LaneIndex].Path;
            }
        }

        void Update()
        {
            if (_path != null)
            {
                distanceTravelled += speed * Time.deltaTime;
                transform.position = _path.GetPointAtDistance(distanceTravelled, endOfPathInstruction) + _height / 2 * Vector3.up;
                transform.rotation = _path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);
            }
        }
    }
}