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
        [SerializeField] private float _rotationSpeed = 15;

        private Lane _lane;
        private float _height = 0;
        private LaneNode _start;
        private LaneNode _end;
        private LaneNode _target;

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

                _start = _lane.StartNode;
                _end = _start.Last;
                _target = _lane.StartNode;
                TeleportToFirstPosition();
            }
        }

        void Update()
        {
            Vector3 targetPosition = Vector3.MoveTowards(transform.position, _target.Position, _speed * Time.deltaTime);
            Quaternion targetRotation = Quaternion.RotateTowards(transform.rotation, _target.Rotation, _rotationSpeed * _speed * Time.deltaTime);
            
            if(transform.position == targetPosition && !(_endOfPathInstruction == EndOfPathInstruction.Stop && _target == _end)) 
            {
                _target = _target.Next != null ? _target.Next : _start;

                if(_target == _start && _endOfPathInstruction == EndOfPathInstruction.Loop) 
                {
                    TeleportToFirstPosition();
                    return;
                }
            }
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        void TeleportToFirstPosition()
        {
            transform.position = _start.Position;
            transform.rotation = _start.Rotation;
        }
    }
}