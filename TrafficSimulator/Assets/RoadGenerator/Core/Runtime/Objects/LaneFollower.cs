using UnityEngine;

namespace RoadGenerator
{
    // Moves along a lane at constant speed.
    // Depending on the end of path instruction, will either loop, reverse, or stop at the end of the path.
    [RequireComponent(typeof(Renderer))]
    public class LaneFollower : MonoBehaviour
    {
        [Header("Lane selection")]
        [SerializeField] private Road _road;
        [SerializeField] private int _laneIndex;
        
        [Header("Follower settings")]
        [SerializeField] private EndOfPathInstruction _endOfPathInstruction;
        [SerializeField] private float _speed = 15;
        private float _distanceTravelled;

        private Lane _lane;
        private float _height = 0;

        void Start() {
            if (_road != null)
            {
                // If the road has not updated yet there will be no lanes, so update them first
                if(_road.Lanes.Count == 0)
                {
                    _road.OnChange();
                }
                
                // Check that the provided lane index is valid
                if(_laneIndex < 0 || _laneIndex >= _road.Lanes.Count)
                {
                    Debug.LogError("Lane index out of range");
                    return;
                }
                
                // Get the height of the object to offset it so it follows on top of the lane
                _height = GetComponent<Renderer>().bounds.size.y;
                
                // Get the lane from the road
                _lane = _road.Lanes[_laneIndex];
            }
        }

        void Update()
        {
            if (_lane != null)
            {
                _distanceTravelled += _speed * Time.deltaTime;
                transform.position = _lane.GetPositionAtDistance(_distanceTravelled, _endOfPathInstruction) + _height / 2 * Vector3.up;
                transform.rotation = _lane.GetRotationAtDistance(_distanceTravelled, _endOfPathInstruction);
            }
        }
    }
}