using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using EVP;
using RoadGenerator;


namespace Car {
    enum Status 
    {
        Driving,
        RepositioningInitiated,
        Repositioning
    }
    public enum Mode 
    {
        Performance,
        Quality
    }
    public enum RoadEndBehaviour 
    {
        Loop,
        Stop
    }
    public enum ShowTargetLines 
    {
        None = 0,
        Target = 1,
        BrakeTarget = 2,
        Both = 3
    }

    public class AutoDrive : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private Road _road;
        [SerializeField] private int _laneIndex = 0;

        [Header("Settings")]
        [SerializeField] private Mode _mode = Mode.Quality;
        [SerializeField] private RoadEndBehaviour _roadEndBehaviour = RoadEndBehaviour.Loop;

        [Header("Quality mode settings")]
        [SerializeField] private ShowTargetLines _showTargetLines = ShowTargetLines.None;
        [SerializeField] [Tooltip("How far from the stopping point the vehicle will come to a full stop at")] private float _brakeOffset = 5f;
        [SerializeField] private float _maxRepositioningSpeed = 5f;
        [SerializeField] [Range(0, 20f)] [Tooltip("The distance the vehicle will look ahead to find the next target. This value will be multiplied by the current speed to increase the lookahead distance when the vehicle is going faster")] private float _baseTLD = 10f;
        [SerializeField] [Tooltip("This constant is used to divide the speed multiplier when calculating the new TLD. Higher value = shorter TLD. Lower value = longer TLD")] private int _TLDSpeedDivider = 20;

        [Header("Performance mode settings")]
        [SerializeField] [Range(0, 100f)] private float _speed = 20f;
        [SerializeField] [Range(2f, 20f)] private float _rotationSpeed = 5f;

        // Shared variables

        // Quality variables
        private float _targetLookaheadDistance = 0;
        private float _brakeDistance = 0;
        private LaneNode _target;
        private LaneNode _brakeTarget;
        private LaneNode _repositioningTarget;
        private LineRenderer _targetLineRenderer;
        private int _repositioningOffset = 1;
        private Status _status = Status.Driving;
        private float _originalMaxSpeed;
        private VehicleController _vehicleController;
        private LaneNode _startNode;
        private LaneNode _endNode;

        void Start()
        {
            _vehicleController = GetComponent<VehicleController>();
            _originalMaxSpeed = _vehicleController.maxSpeedForward;
            
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
            
            Lane lane = _road.Lanes[_laneIndex];
            _startNode = lane.StartNode;
            _endNode = lane.StartNode.Last;

            _target = lane.StartNode;
            
            if (_mode == Mode.Quality)
            {
                // Setup target line renderer
                float targetLineWidth = 0.3f;
                _targetLineRenderer = GetComponent<LineRenderer>();
                _targetLineRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                _targetLineRenderer.sharedMaterial.SetColor("_Color", Color.red);
                _targetLineRenderer.startWidth = targetLineWidth;
                _targetLineRenderer.endWidth = targetLineWidth;

                // Teleport the vehicle to the start of the lane and set the acceleration to the max
                Q_TeleportToLane();
                
                _brakeTarget = lane.StartNode;
                _repositioningTarget = lane.StartNode;
                
                _vehicleController.throttleInput = 1f;
            }
            else if (_mode == Mode.Performance)
            {
                P_MoveToFirstPosition();
            }
        }

        void Update()
        {
            if (_mode == Mode.Quality)
            {
                // Update brake distance and target
                Q_UpdateBrakeDistance();
                Q_UpdateBrakeTarget();
                
                // Steer towards the target and update to next target
                Q_SteerTowardsTarget();
                Q_UpdateTarget();
                
                if (_showTargetLines != ShowTargetLines.None)
                {
                    Q_DrawTargetLines();
                }
            }
            else if (_mode == Mode.Performance)
            {
                P_MoveToNextPosition();
            }
        }

        private void Q_TeleportToLane()
        {
            // Move it to the first position of the lane, offset in the opposite direction of the lane
            transform.position = _startNode.Position - (2 * (_startNode.Next.Position - _startNode.Position));
            
            // Rotate it to face the first position of the lane
            transform.rotation = Quaternion.LookRotation(_startNode.Next.Position - _startNode.Position);
        }
        private void Q_SteerTowardsTarget()
        {
            // Calculate the direction, which is the vector from our current position to the target
            Vector3 direction = Q_GetTarget().Position - transform.position;

            // Calculate the desired steering angle as the angle between our forward vector and the direction to the target, divided by 90 to get a value between -1 and 1 if it is in front of us.
            // If it is behind us, the value will be between (-1, -2) or (1, 2) respectively which will be clamped to +-1 by the SetTurnAnglePercent method
            float steeringAngle = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up) / 90;

            // Steer smoothly from the current steering angle to the desired
            _vehicleController.steerInput = Vector3.MoveTowards(new Vector3(_vehicleController.steerInput, 0, 0), new Vector3(steeringAngle, 0, 0), Time.deltaTime).x;
        }
        private void Q_UpdateTarget()
        {   
            // Calculate the direction, which is the vector from our current position to the target
            Vector3 direction = Q_GetTarget().Position - transform.position;

            // Calculate the dot product between our forward vector and the direction. If the target is in front of us, the dot product will be positive. If it's behind us, it will be negative
            float dot = Vector3.Dot(transform.forward, direction.normalized);

            // Set new taget look ahead distance based on the current speed, if the speed is very low, use the base look ahead distance (multiplied by 1)
            _targetLookaheadDistance = Math.Max(_baseTLD * _vehicleController.speed / _TLDSpeedDivider, _baseTLD);

            // If the vehicle is driving and the target is behind us and too far away
            if (_status == Status.Driving && dot < 0 && direction.magnitude > _targetLookaheadDistance + 1f)
            {
                Debug.Log("Repositioning started, slowing down...");
                _status = Status.RepositioningInitiated;

                // Reposition to the point prior to the one we missed
                _repositioningTarget = GetNextLaneNode(_target, -_repositioningOffset - 1, false);

                // Slow down and limit the max speed to the repositioning speed or 30% of the max speed, whichever is lower
                _vehicleController.brakeInput = 1f;
                _vehicleController.maxSpeedForward = Math.Min(_vehicleController.maxSpeedForward * 0.3f, _maxRepositioningSpeed);
            }
            // If the vehicle has started repositioning and slowed down enough
            else if (_status == Status.RepositioningInitiated && _vehicleController.speed <= _vehicleController.maxSpeedForward)
            {
                Debug.Log("Slowed down, releasing brakes and repositioning...");
                _status = Status.Repositioning;

                _vehicleController.brakeInput = 0f;
            }
            // If the vehicle is currently repositioning
            else if (_status == Status.Repositioning) 
            {
                // Allow the vehicle to accelerate and reverse, whatever takes it to the target faster.
                // It will accelerate if the target is in front, and reverse if it's behind
                _vehicleController.throttleInput = dot > 0 ? 1 : -1;
                
                // If the target is in front of us and we are close enough we have successfully repositioned
                if (dot > 0 && direction.magnitude <= _targetLookaheadDistance - 1f) 
                {
                    Debug.Log("Repositioned, speeding back up...");
                    _status = Status.Driving;
                    
                    // Set the target to the one after the target we missed
                    _target = GetNextLaneNode(_repositioningTarget, _repositioningOffset, false);
                    
                    // Reset the max speed to the original, and set the acceleration to the max again
                    _vehicleController.maxSpeedForward = _originalMaxSpeed;
                    _vehicleController.throttleInput = 1f;
                    _vehicleController.brakeInput = 0f;
                }
            }
            // If the vehicle is driving and the target is in front of us and we are close enough
            else if (_status == Status.Driving && dot > 0 && direction.magnitude <= _targetLookaheadDistance)
            {
                // Set the target to the next point in the lane
                _target = GetNextLaneNode(_target, 0, _roadEndBehaviour == RoadEndBehaviour.Loop);
            }

            // If the vehicle is closer to the target than the brake distance, brake
            // Also, do not brake at the end node if we are looping
            if (Vector3.Distance(transform.position, _brakeTarget.Position) <= _brakeDistance && !(_roadEndBehaviour == RoadEndBehaviour.Loop && _brakeTarget == _endNode))
            {
                _vehicleController.brakeInput = Mathf.Lerp(_vehicleController.brakeInput, 1f, Time.deltaTime * 1.5f);
                _vehicleController.throttleInput = 0f;
            }
            // If the vehicle is further away from the target than the brake distance, accelerate
            else if (Vector3.Distance(transform.position, _brakeTarget.Position) > _brakeDistance + 1)
            {
                _vehicleController.brakeInput = 0f;
                _vehicleController.throttleInput = 1f;
            }
        }

        private void Q_UpdateBrakeTarget()
        {
            // Set the brake target point to the point closest to the target that is at least _brakeDistance points away
            while (Vector3.Distance(transform.position, _brakeTarget.Position) < _brakeDistance && _brakeTarget != _endNode)
            {
                _brakeTarget = GetNextLaneNode(_brakeTarget, 0, _roadEndBehaviour == RoadEndBehaviour.Stop);
            }
        }

        private void Q_UpdateBrakeDistance()
        {
            // If we should loop when the road has ended and our braking target is at the end, we do not want any brake offset.
            // That would cause the vehicle to stop at the end of the road, instead of looping around
            float brakeOffset = _roadEndBehaviour == RoadEndBehaviour.Loop && _brakeTarget == _endNode ? 0 : _brakeOffset;
            
            // Calculate the distance it will take to stop
            _brakeDistance = brakeOffset + (_vehicleController.speed / 2) + _vehicleController.speed * _vehicleController.speed / (_vehicleController.tireFriction * 9.81f);
        }

        private LaneNode Q_GetTarget()
        {
            return _status == Status.Driving ? _target : _repositioningTarget;
        }

        private LaneNode GetNextLaneNode(LaneNode currentNode, int offset = 0, bool wrapAround = true)
        {
            LaneNode node = currentNode;
            
            for(int i = 0; i < Math.Abs(offset) + 1; i++)
            {
                LaneNode nextNode = offset >= 0 ? node.Next : node.Prev;
                
                // If we are on the last node
                if(nextNode == null)
                {
                    // Either wrap around or return the last node
                    if(wrapAround)
                        node = offset >= 0 ? _startNode : _endNode;
                    else
                        return node;
                }
                else
                {
                    node = nextNode;
                }
            }
            return node;
        }
        
        // Draw lines towards steering and braking target
        private void Q_DrawTargetLines()
        {
            switch (_showTargetLines)
            {
                case ShowTargetLines.Target:
                    _targetLineRenderer.SetPositions(new Vector3[]{ Q_GetTarget().Position, transform.position });
                    _targetLineRenderer.positionCount = 2;
                    break;
                case ShowTargetLines.BrakeTarget:
                    _targetLineRenderer.SetPositions(new Vector3[]{ _brakeTarget.Position, transform.position });
                    _targetLineRenderer.positionCount = 2;
                    break;
                case ShowTargetLines.Both:
                    _targetLineRenderer.SetPositions(new Vector3[]{ _brakeTarget.Position, transform.position, Q_GetTarget().Position });
                    _targetLineRenderer.positionCount = 3;
                    break;
            }
        }

        // Performance methods
        private void P_MoveToFirstPosition()
        {
            // Move to the first position of the lane
            transform.position = _startNode.Position;
            transform.rotation = _startNode.Rotation;
        }

        private void P_MoveToNextPosition()
        {
            Vector3 targetPosition = P_GetLerpPosition(_target.Position);
            Quaternion targetRotation = P_GetLerpQuaternion(_target.Rotation);
            
            if(transform.position == targetPosition) 
            {
                _target = GetNextLaneNode(_target, 0, _roadEndBehaviour == RoadEndBehaviour.Loop);
                if(_target == _startNode && _roadEndBehaviour == RoadEndBehaviour.Loop) 
                {
                    P_MoveToFirstPosition();
                    return;
                }
            }
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        private Vector3 P_GetLerpPosition(Vector3 target)
        {
            return Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
        }
        private Quaternion P_GetLerpQuaternion(Quaternion target)
        {
            return Quaternion.RotateTowards(transform.rotation, target, _rotationSpeed * _speed * Time.deltaTime);
        }
    }
}

