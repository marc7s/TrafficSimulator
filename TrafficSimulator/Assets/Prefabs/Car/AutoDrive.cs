using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using EVP;
using RoadGenerator;
using CustomProperties;
using DataModel;


namespace Car {
    enum Status 
    {
        Driving,
        RepositioningInitiated,
        Repositioning
    }
    public enum DrivingMode 
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
        None,
        Target,
        BrakeTarget,
        CurrentPosition,
        OccupiedNodes,
        All
    }
    public class AutoDrive : MonoBehaviour
    {
        [Header("Connections")]
        public Road Road;
        public GameObject NavigationTargetMarker;
        public Material NavigationPathMaterial;
        public int LaneIndex = 0;
        [SerializeField] private GameObject _mesh;

        [Header("Settings")]
        [SerializeField] private DrivingMode _mode = DrivingMode.Quality;
        [SerializeField] private RoadEndBehaviour _roadEndBehaviour = RoadEndBehaviour.Loop;
        public bool ShowNavigationPath = false;
        [SerializeField][HideInInspector] private NavigationMode _navigationMode = NavigationMode.Disabled;
        [SerializeField] private NavigationMode _originalNavigationMode = NavigationMode.Disabled;
        [SerializeField] private bool _logRepositioningInformation = true;
        [SerializeField] [Tooltip("This constant determines the offset to extend the bounds the vehicle uses to occupy nodes")] private float _vehicleOccupancyOffset = 3f;

        [Header("Quality mode settings")]
        [SerializeField] private ShowTargetLines _showTargetLines = ShowTargetLines.None;
        [SerializeField] [Tooltip("How far from the stopping point the vehicle will come to a full stop at")] private float _brakeOffset = 5f;
        [SerializeField] private float _maxRepositioningSpeed = 5f;
        [SerializeField] private float _maxReverseDistance = 20f;
        [SerializeField] [Range(0, 20f)] [Tooltip("The distance the vehicle will look ahead to find the next target. This value will be multiplied by the current speed to increase the lookahead distance when the vehicle is going faster")] private float _baseTLD = 10f;
        [SerializeField] [Tooltip("This constant is used to divide the speed multiplier when calculating the new TLD. Higher value = shorter TLD. Lower value = longer TLD")] private int _TLDSpeedDivider = 20;

        [Header("Performance mode settings")]
        [SerializeField] [Range(0, 100f)] private float _speed = 20f;
        [SerializeField] [Range(2f, 20f)] private float _rotationSpeed = 5f;

        [Header("Statistics")]
        [SerializeField] [ReadOnly] private float _totalDistance = 0;
        
        // Public variables
        public LaneNode CustomStartNode = null;

        // Private variables
        private Vehicle _vehicle;
        private float _vehicleLength;
        private Bounds _vehicleBounds;
        private float _originalMaxSpeed;
        private Vector3? _prevIntersectionPosition;
        private Dictionary<string, LaneNode> _currentNodeTransitions = new Dictionary<string, LaneNode>();
        private NavigationNode _navigationPathEndNode;
        private Stack<NavigationNodeEdge> _navigationPath = new Stack<NavigationNodeEdge>();
        private List<LaneNode> _occupiedNodes = new List<LaneNode>();
        private float _lerpSpeed;
        private GameObject _navigationPathContainer;
        private bool _isEnteringNetwork = true;
        private LaneNode _target;
        private LineRenderer _targetLineRenderer;
        private LaneNode _startNode;
        private LaneNode _endNode;
        private LaneNode _currentNode;
        private LaneNode _brakeTarget;
        private float _brakeDistance = 0;

        // Quality variables
        private const int _repositioningOffset = 1;
        private Status _status = Status.Driving;
        private float _targetLookaheadDistance = 0;
        private const float _intersectionLookaheadDistance = 5f;
        private LaneNode _previousTarget;
        private LaneNode _repositioningTarget;
        private VehicleController _vehicleController;
        
        
        void Start()
        {
            Road.RoadSystem.Setup();
            _navigationPathContainer = new GameObject("Navigation Path");

            _vehicleController = GetComponent<VehicleController>();
            _vehicleLength = _mesh.GetComponent<MeshRenderer>().bounds.size.z;
            // TODO - use this to decide whether a node is occupied or not
            //_vehicleBounds = _mesh.GetComponent<MeshRenderer>().bounds;
            _vehicle = GetComponent<Vehicle>();
            _originalMaxSpeed = _vehicleController.maxSpeedForward;
            
            // If the road has not updated yet there will be no lanes, so update them first
            if(Road.Lanes.Count == 0)
                Road.OnChange();
            
            // Check that the provided lane index is valid
            if(LaneIndex < 0 || LaneIndex >= Road.Lanes.Count)
            {
                Debug.LogError("Lane index out of range");
                return;
            }
            
            Lane lane = Road.Lanes[LaneIndex];
            _endNode = lane.StartNode.Last;
            _startNode = lane.StartNode;
            _currentNode = CustomStartNode == null ? lane.StartNode : CustomStartNode;
            _target = _currentNode;

            // Setup target line renderer
            float targetLineWidth = 0.3f;
            _targetLineRenderer = GetComponent<LineRenderer>();
            _targetLineRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            _targetLineRenderer.sharedMaterial.SetColor("_Color", Color.green);
            _targetLineRenderer.startWidth = targetLineWidth;
            _targetLineRenderer.endWidth = targetLineWidth;
            
            if (_mode == DrivingMode.Quality)
            {
                // Teleport the vehicle to the start of the lane and set the acceleration to the max
                Q_TeleportToLane();
                
                _brakeTarget = _currentNode;
                _repositioningTarget = _currentNode;
                
                _vehicleController.throttleInput = 1f;
            }
            else if (_mode == DrivingMode.Performance)
            {
                // In performance mode the vehicle should not be affected by physics or gravity
                Rigidbody rigidbody = GetComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                _brakeTarget = _currentNode;
                _target = _currentNode;
                _lerpSpeed = _speed;
                P_TeleportToFirstPosition();
            }
            
            if (_navigationMode == NavigationMode.RandomNavigationPath)
            {
                UpdateRandomPath();
                SetInitialPrevIntersection();
            }
            _navigationMode = _originalNavigationMode;
        }

        void Update()
        {
            UpdateOccupiedNodes();
            UpdateBrakeDistance();
            UpdateBrakeTarget();
            if (_mode == DrivingMode.Quality)
            {
                // Brake if needed
                Q_Brake();
                
                // Steer towards the target and update to next target
                Q_SteerTowardsTarget();
                Q_UpdateTarget();
                Q_UpdateCurrent();
            }
            else if (_mode == DrivingMode.Performance)
            {
                // Brake if needed
                P_Brake();
                P_UpdateTargetAndCurrent();
            }
            if (_showTargetLines != ShowTargetLines.None)
                    DrawTargetLines();
        }

        private void Q_ResetToNode(LaneNode node)
        {
            _currentNode = node;
            _target = node;
            _brakeTarget = node;
            _repositioningTarget = node;
            _isEnteringNetwork = true;

            Q_TeleportToLane();
            _navigationMode = _originalNavigationMode;
            if (_navigationMode == NavigationMode.RandomNavigationPath)
                UpdateRandomPath();
            
            SetInitialPrevIntersection();
        }

        // Update the list of nodes that the vehicle is currently occupying
        private void UpdateOccupiedNodes()
        {
            foreach (LaneNode node in _occupiedNodes)
            {
                node.UnsetVehicle(_vehicle);
            }
            List<LaneNode> spanNodes = GetVehicleSpanNodes();
            _occupiedNodes.Clear();
            foreach (LaneNode node in spanNodes)
            {
                // Only add the span nodes we successfully acquire to the list of occupied nodes
                if(node.SetVehicle(_vehicle))
                    _occupiedNodes.Add(node);
            }
        }

        // Get the list of nodes that the vehicle is currently occupying by moving backwards from the current position until out of vehicle bounds
        private List<LaneNode> GetVehicleSpanNodes()
        {
            List<LaneNode> nodes = new List<LaneNode>(){ _currentNode };
            LaneNode node = _currentNode.Prev;

            float nodeDistance = _currentNode.DistanceToPrevNode;

            // Add all occupied nodes prior to and including the current node
            while (node != null && nodeDistance <= _vehicleLength / 2 + _vehicleOccupancyOffset)
            {
                nodes.Add(node);
                nodeDistance += node.DistanceToPrevNode;
                node = node.Prev;
            }
            
            nodeDistance = 0;
            // Add all occupied nodes after and excluding the current node
            node = _currentNode.Next;
            while (node != null && nodeDistance <= _vehicleLength / 2 + _vehicleOccupancyOffset)
            {
                nodes.Add(node);
                nodeDistance += node.DistanceToPrevNode;
                node = node.Next;
            }
            return nodes;
        }

        private void Q_TeleportToLane()
        {
            // Rotate it to face the current position
            _vehicleController.cachedRigidbody.MoveRotation(_currentNode.Rotation);
            
            // Move it to the current position, offset in the opposite direction of the lane
            _vehicleController.cachedRigidbody.MovePosition(_currentNode.Position - (2 * (_currentNode.Rotation * Vector3.forward).normalized));
            
            // Reset velocity and angular velocity
            _vehicleController.cachedRigidbody.velocity = Vector3.zero;
            _vehicleController.cachedRigidbody.angularVelocity = Vector3.zero;
        }

        private void Q_SteerTowardsTarget()
        {
            // Calculate the direction, which is the vector from our current position to the target
            Vector3 direction = Q_GetTarget().Position - transform.position;

            // Calculate the dot product between our forward vector and the direction. If the target is in front of us, the dot product will be positive. If it's behind us, it will be negative
            float dot = Vector3.Dot(transform.forward, direction.normalized);

            // Calculate the desired steering angle as the angle between our forward vector and the direction to the target, divided by 90 to get a value between -1 and 1 if it is in front of us.
            // If it is behind us, the value will be between (-1, -2) or (1, 2) respectively which will be clamped to +-1 by the SetTurnAnglePercent method
            float steeringAngle = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up) / 90;

            // If the target is directly behind, steer slightly to the right to avoid continuing away from the target
            if(steeringAngle == 0 && dot < 0)
                steeringAngle = 0.1f;

            // Steer smoothly from the current steering angle to the desired
            _vehicleController.steerInput = Vector3.MoveTowards(new Vector3(_vehicleController.steerInput, 0, 0), new Vector3(steeringAngle, 0, 0), Time.deltaTime).x;
        }
        private void Q_UpdateTarget()
        {   
            LaneNode target = Q_GetTarget();
            
            // Calculate the direction, which is the vector from our current position to the target
            Vector3 direction = target.Position - transform.position;

            // Calculate the dot product between our forward vector and the direction. If the target is in front of us, the dot product will be positive. If it's behind us, it will be negative
            float dot = Vector3.Dot(transform.forward, direction.normalized);

            // Set new taget look ahead distance based on the current speed. if the speed is very low, use the base look ahead distance (multiplied by 1)
            // If the vehicle is driving in an intersection, instead use a constant small lookahead distance for precise steering
            _targetLookaheadDistance = target.RoadNode.Intersection != null ? _intersectionLookaheadDistance : Math.Max(_baseTLD * _vehicleController.speed / _TLDSpeedDivider, _baseTLD);

            // If the vehicle is driving and the target is behind us and too far away
            if (_status == Status.Driving && dot < 0 && direction.magnitude > _targetLookaheadDistance + 1f)
            {
                if(_logRepositioningInformation)
                    Debug.Log("Repositioning started, slowing down...");
                _status = Status.RepositioningInitiated;

                // Reposition to the point prior to the one we missed
                _repositioningTarget = GetNextLaneNode(_target, -_repositioningOffset - 1, false);

                // Slow down and limit the max speed to the repositioning speed or the max speed, whichever is lower
                _vehicleController.brakeInput = 1f;
                _vehicleController.maxSpeedForward = Math.Min(_vehicleController.maxSpeedForward, _maxRepositioningSpeed);
            }
            // If the vehicle has started repositioning and slowed down enough
            else if (_status == Status.RepositioningInitiated && _vehicleController.speed <= _vehicleController.maxSpeedForward)
            {
                if(_logRepositioningInformation)
                    Debug.Log("Slowed down, releasing brakes and repositioning...");
                _status = Status.Repositioning;

                _vehicleController.brakeInput = 0f;
            }
            // If the vehicle is currently repositioning
            else if (_status == Status.Repositioning) 
            {
                // Allow the vehicle to accelerate and reverse. It will only reverse if the target is behind it, and within reversing distance
                _vehicleController.throttleInput = Vector3.Distance(transform.position, Q_GetTarget().Position) < _maxReverseDistance && dot < 0 ? -1 : 1;
                
                // If the target is in front of us and we are close enough we have successfully repositioned
                if (dot > 0 && direction.magnitude <= _targetLookaheadDistance - 1f) 
                {
                    if(_logRepositioningInformation)
                        Debug.Log("Repositioned, speeding back up...");
                    _status = Status.Driving;

                    // Assume that the car travelled straight to the repositioning target
                    _totalDistance += Vector3.Distance(_currentNode.Position, _target.Position);
                    
                    // Update the current node
                    _currentNode = _target;
                    
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
                _target = GetNextLaneNode(_target, 0, false);
            }
        }

        private void Q_Brake()
        {
            float distanceToBrakeTarget;
            bool brakeTargetFound = _currentNode.DistanceToNode(_brakeTarget, out distanceToBrakeTarget, _roadEndBehaviour == RoadEndBehaviour.Loop, true);
            distanceToBrakeTarget += Vector3.Distance(_currentNode.Position, transform.position);
            
            // If the brake target is not found or the vehicle is further away from the target than the brake distance, accelerate
            if (!brakeTargetFound || distanceToBrakeTarget > _brakeDistance + 1)
            {
                _vehicleController.brakeInput = 0f;
                _vehicleController.throttleInput = 1f;
            }
            // If the vehicle is closer to the target than the brake distance, brake
            else if (distanceToBrakeTarget <= _brakeDistance)
            {
                _vehicleController.brakeInput = Mathf.Lerp(_vehicleController.brakeInput, 1f, Time.deltaTime * 1.5f);
                _vehicleController.throttleInput = 0f;
            }
            
           UpdateTargetFromNavigation();
        }

        private void UpdateBrakeTarget()
        {
            // Set the brake target point to the point closest to the target that is at least _brakeDistance points away
            // If the road end behaviour is set to stop and the brake target is the end node, do not update the brake target
            // If the next node has a vehicle, do not update the brake target
            if (_mode == DrivingMode.Quality)
            {
                while(Q_ShouldAdvanceBrakeTarget())
                    _brakeTarget = GetNextLaneNode(_brakeTarget, 0, true);
            }
            // Performance mode is
            else if (_mode == DrivingMode.Performance)
            {
                LaneNode nextBrakeTarget = _currentNode;
                while (P_ShouldAdvanceBrakeTarget(nextBrakeTarget))
                {
                    nextBrakeTarget = GetNextLaneNode(nextBrakeTarget, 0, true);
                }
                _brakeTarget = nextBrakeTarget;
            }
        }

        private bool Q_ShouldAdvanceBrakeTarget()
        {
            float distanceToBrakeTarget;
            bool brakeTargetFound = _currentNode.DistanceToNode(_brakeTarget, out distanceToBrakeTarget, _roadEndBehaviour == RoadEndBehaviour.Loop, true);
            
            // Return if the brake target was not found
            if(!brakeTargetFound)
                return false;
            
            Vehicle nextNodeVehicle = GetNextLaneNode(_brakeTarget, 0, true).Vehicle;
            bool _nextNodeHasVehicle = nextNodeVehicle != null && nextNodeVehicle != _currentNode.Vehicle;
            bool _brakeTargetIsEndNode = _brakeTarget.Type == RoadNodeType.End && _brakeTarget != _startNode;
            bool _brakeTargetIsEndNodeAndLoop = _brakeTargetIsEndNode && _roadEndBehaviour == RoadEndBehaviour.Loop;
            bool _brakeDistanceIsGreaterThanBrakeTargetDistance = distanceToBrakeTarget < _brakeDistance;
            
            return _brakeDistanceIsGreaterThanBrakeTargetDistance && !_nextNodeHasVehicle && (!_brakeTargetIsEndNode || _brakeTargetIsEndNodeAndLoop);
        }

        private void UpdateBrakeDistance()
        {
            if (_mode == DrivingMode.Quality)
            {
                // Calculate the distance it will take to stop
                _brakeDistance = _brakeOffset + (_vehicleController.speed / 2) + _vehicleController.speed * _vehicleController.speed / (_vehicleController.tireFriction * 9.81f);
            }
            else if (_mode == DrivingMode.Performance)
            {
                // Set the distance to the brake offset + the speed divided by 5
                _brakeDistance = _brakeOffset + _speed / 5f;
            }
            
        }

        private LaneNode Q_GetNextCurrentNode()
        {
            bool transitionFound = _currentNodeTransitions.ContainsKey(_currentNode.ID);

            return transitionFound ? _currentNodeTransitions[_currentNode.ID] : GetNextLaneNode(_currentNode, 0, false);
        }

        private void Q_UpdateCurrent()
        {
            LaneNode nextNode = Q_GetNextCurrentNode();
            LaneNode nextNextNode = GetNextLaneNode(nextNode, 0, false);
            bool reachedEnd = !_isEnteringNetwork && _currentNode.Type == RoadNodeType.End;

            // Move the current node forward while we are closer to the next node than the current. Also check the node after the next as the next may be further away in intersections where the current road is switched
            // Note: only updates while driving. During repositioning the vehicle will be closer to the next node (the repositioning target) halfway through the repositioning
            // This would cause our current position to skip ahead so repositioning is handled separately
            while(!reachedEnd && _status == Status.Driving && (Vector3.Distance(transform.position, nextNode.Position) <= Vector3.Distance(transform.position, _currentNode.Position) || Vector3.Distance(transform.position, nextNextNode.Position) <= Vector3.Distance(transform.position, _currentNode.Position)))
            {
                if(_currentNodeTransitions.ContainsKey(_currentNode.ID))
                    _currentNodeTransitions.Remove(_currentNode.ID);
                
                _totalDistance += _currentNode.DistanceToPrevNode;
                _currentNode = nextNode;
                // When the current node is updated, it needs to redraw the navigation path
                if (ShowNavigationPath)
                    Navigation.DrawPathRemoveOldestPoint(_navigationPathContainer);
                nextNode = Q_GetNextCurrentNode();
                nextNextNode = GetNextLaneNode(nextNode, 0, false);
                reachedEnd = reachedEnd || (!_isEnteringNetwork && _currentNode.Type == RoadNodeType.End);
            }

            // If the road ended but we are looping, teleport to the first position
            if(reachedEnd && _roadEndBehaviour == RoadEndBehaviour.Loop)
            {
                Q_ResetToNode(_startNode);
            }

            // After the first increment of the current node, we are no longer entering the network
            if(_isEnteringNetwork && _currentNode.Type != RoadNodeType.End)
                _isEnteringNetwork = false;
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
        private void DrawTargetLines()
        {
            switch (_showTargetLines)
            {
                case ShowTargetLines.Target:
                    _targetLineRenderer.positionCount = 2;
                    _targetLineRenderer.SetPositions(new Vector3[]{ Q_GetTarget().Position, transform.position });
                    break;
                case ShowTargetLines.BrakeTarget:
                    _targetLineRenderer.positionCount = 2;
                    _targetLineRenderer.SetPositions(new Vector3[]{ _brakeTarget.Position, transform.position });
                    break;
                case ShowTargetLines.CurrentPosition:
                    _targetLineRenderer.positionCount = 2;
                    _targetLineRenderer.SetPositions(new Vector3[]{ _currentNode.Position, transform.position });
                    break;
                case ShowTargetLines.OccupiedNodes:
                    _targetLineRenderer.positionCount = _occupiedNodes.Count;
                    _targetLineRenderer.SetPositions(_occupiedNodes.Select(x => x.Position).ToArray());
                    break;
                case ShowTargetLines.All:
                    _targetLineRenderer.positionCount = 3 + _occupiedNodes.Count;
                    _targetLineRenderer.SetPositions(new Vector3[]{ _brakeTarget.Position, transform.position, _currentNode.Position, transform.position}.Concat(_occupiedNodes.Select(x => x.Position)).ToArray());
                    break;
            }  
        }

        private void SetInitialPrevIntersection()
        {
            _prevIntersectionPosition = null;
            // If the starting node is an intersection, the previous intersection is set 
            if (_target.RoadNode.Intersection != null)
                _prevIntersectionPosition = _target.RoadNode.Intersection.IntersectionPosition;
                
            // If the starting node is at a three way intersection, the target will be an EndNode but the next will be an intersection node, so we need to set the previous intersection
            if (_target.RoadNode.Next != null && _target.RoadNode.Next.Intersection != null && _target.RoadNode.Position == _target.RoadNode.Next.Position)
                _prevIntersectionPosition = _target.RoadNode.Next.Intersection.IntersectionPosition;

            // If the starting node is a junction edge, the previous intersection is set
            if (_target.RoadNode.Prev != null && _target.RoadNode.Prev.Intersection != null && _target.RoadNode.Position == _target.RoadNode.Prev.Position)
                _prevIntersectionPosition = _target.RoadNode.Prev.Intersection.IntersectionPosition;
        }

        private void P_TeleportToFirstPosition()
        {
            if (_navigationMode == NavigationMode.RandomNavigationPath)
            {
                UpdateRandomPath();
                SetInitialPrevIntersection();
            }

            // Move to the first position of the lane
            transform.position = _currentNode.Position;
            transform.rotation = _currentNode.Rotation;
            P_MoveToTargetNode();
        }

        // Performance methods

        // Move the vehicle to the target node
        private void P_MoveToTargetNode()
        {
            Vector3 targetPosition = P_GetLerpPosition(_target.Position);
            Quaternion targetRotation = P_GetLerpQuaternion(_target.Rotation);

            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        private void P_UpdateTargetAndCurrent()
        {
            LaneNode nextTarget = GetNextLaneNode(_target, 0, _roadEndBehaviour == RoadEndBehaviour.Loop);
            // If the car is at the target, set the target to the next node and update current node
            float distanceToBrakeTarget;
            bool brakeTargetFound = nextTarget.DistanceToNode(_brakeTarget, out distanceToBrakeTarget, _roadEndBehaviour == RoadEndBehaviour.Loop, true);
            
            if (transform.position == _target.Position && distanceToBrakeTarget > _vehicleLength / 2)
            {
                _currentNode = _target;
                // When the currentNode is changed, the navigation path needs to be updated
                if (ShowNavigationPath)
                    Navigation.DrawPathRemoveOldestPoint(_navigationPathContainer);
                
                _totalDistance += _currentNode.DistanceToPrevNode;
                _target = GetNextLaneNode(_target, 0, _roadEndBehaviour == RoadEndBehaviour.Loop);
            }
            // Move the vehicle to the target node
            P_MoveToTargetNode();
            UpdateTargetFromNavigation();
        }

        private bool P_ShouldAdvanceBrakeTarget(LaneNode tempBrakeTarget)
        {
            float distanceToBrakeTarget;
            bool brakeTargetFound = _currentNode.DistanceToNode(tempBrakeTarget, out distanceToBrakeTarget, _roadEndBehaviour == RoadEndBehaviour.Loop, true);
            
            // Return if the brake target was not found
            if(!brakeTargetFound)
            {
                Debug.Log("Brake target not found");
                return false;
            }
            
            Vehicle nextNodeVehicle = GetNextLaneNode(tempBrakeTarget, 0, true).Vehicle;
            bool _nextNodeHasVehicle = nextNodeVehicle != null && nextNodeVehicle != _currentNode.Vehicle;
            
            bool _brakeTargetIsEndNode = tempBrakeTarget.Type == RoadNodeType.End && tempBrakeTarget == _endNode;
            bool _brakeTargetIsEndNodeAndLoop = _brakeTargetIsEndNode && _roadEndBehaviour == RoadEndBehaviour.Loop;
            bool _brakeDistanceIsGreaterThanBrakeTargetDistance = distanceToBrakeTarget < _brakeDistance;
            
            return _brakeDistanceIsGreaterThanBrakeTargetDistance && !_nextNodeHasVehicle && (!_brakeTargetIsEndNode || _brakeTargetIsEndNodeAndLoop);
        }

        private void P_Brake()
        {
            float distanceToBrakeTarget;
            bool brakeTargetFound = _currentNode.DistanceToNode(_brakeTarget, out distanceToBrakeTarget, _roadEndBehaviour == RoadEndBehaviour.Loop, true);

            if(!brakeTargetFound)
                return;
            
            float distanceToCurrentNode = Vector3.Distance(transform.position, _currentNode.Position);
            
            // Determine the distance from the transform to the current node, along the current direction
            Vector3 currentDirection = _currentNode.Rotation * Vector3.forward;
            Vector3 transformToCurrentNode = _currentNode.Position - transform.position;
            Vector3 transformAlongDirection = Vector3.Project(transformToCurrentNode, currentDirection);
            
            // Add the distance along the path from the transform to the current node to the brake target
            distanceToBrakeTarget += transformAlongDirection.magnitude;

            float maxDelta = Time.deltaTime * 0.07f;
            
            // If the vehicle is closer to the target than the brake distance, start decelerating
            if (distanceToBrakeTarget <= _brakeDistance)
            {
                _currentNode = _target;
                float minLerpSpeed = 3f;
                _lerpSpeed = Mathf.MoveTowards(_lerpSpeed, minLerpSpeed, maxDelta);
            }
            
            if (distanceToBrakeTarget > _brakeDistance)
                _lerpSpeed = Mathf.MoveTowards(_lerpSpeed, _speed, maxDelta);
        }
        private bool P_HasReachedTarget()
        {
            // Since the target position will be lifted in performance mode, we need to compare the XZ coordinates
            Vector3 targetPosition = _target.Position;
            targetPosition.y = transform.position.y;
            
            return transform.position == targetPosition;
        }

        private void UpdateTargetFromNavigation()
        {
            if (_navigationMode == NavigationMode.Disabled)
                return;
            
            bool isNonIntersectionNavigationNode = _target.RoadNode.IsNavigationNode && !_target.IsIntersection();
            bool currentTargetNodeNotChecked = _previousTarget != null && _target.RoadNode != _previousTarget.RoadNode;
            if (isNonIntersectionNavigationNode &&_navigationPath.Count != 0 && currentTargetNodeNotChecked)
            {
                _navigationPath.Pop();
                _prevIntersectionPosition = Vector3.zero; 
            }
            if (_navigationPathEndNode != null && _navigationPathEndNode.RoadNode == _target.RoadNode && _navigationPath.Count == 0)
                UpdateRandomPath();
            
            if (_target.Type == RoadNodeType.JunctionEdge && currentTargetNodeNotChecked)
            {
                LaneNode entryNode = _target;
                // Only check the intersection if the vehicle hasn't already just checked it
                bool intersectionNotChecked = _target.RoadNode.Intersection.IntersectionPosition != _prevIntersectionPosition;
                if (intersectionNotChecked)
                {
                    if (_navigationMode == NavigationMode.RandomNavigationPath)
                    {
                        (_startNode, _endNode, _target) = _target.RoadNode.Intersection.GetNewLaneNode(_navigationPath.Pop(), _target);
                        // In performance mode, one currentNode will not be checked as it changes immediately, so we need to remove the oldest point from the navigation path
                        if (_mode == DrivingMode.Performance)
                            Navigation.DrawPathRemoveOldestPoint(_navigationPathContainer);
                        // If the intersection does not have a lane node that matches the navigation path, unexpected behaviour has occurred, switch to random navigation
                        if (_target == null)
                            _navigationMode = NavigationMode.Random;
                    }
                    else if (_navigationMode == NavigationMode.Random)
                    {
                        (_startNode, _endNode, _target) = _target.RoadNode.Intersection.GetRandomLaneNode(_target);
                    }
                    
                    if(_currentNodeTransitions.ContainsKey(entryNode.ID))
                        _currentNodeTransitions.Remove(entryNode.ID);

                    _currentNodeTransitions.Add(entryNode.ID, _target);
                    _brakeTarget = _target;
                    _prevIntersectionPosition = _target.RoadNode.Intersection.IntersectionPosition;
                }
            }
            _previousTarget = _target;
        }

        private void UpdateRandomPath()
        {
            // Get a random path from the navigation graph
            _navigationPath = Navigation.GetRandomPath(Road.RoadSystem, _target.GetNavigationEdge(), out _navigationPathEndNode);
            if (ShowNavigationPath)
                    Navigation.DrawNavigationPath(_navigationPathEndNode, _navigationPath, _currentNode, _navigationPathContainer, NavigationPathMaterial, _prevIntersectionPosition, NavigationTargetMarker);
            if (_navigationPath.Count == 0)
            {
                _navigationMode = NavigationMode.Random;
            }
        }

        private Vector3 P_GetLerpPosition(Vector3 target)
        {
            return Vector3.MoveTowards(transform.position, target, _lerpSpeed * Time.deltaTime);
        }
        private Quaternion P_GetLerpQuaternion(Quaternion target)
        {
            return Quaternion.RotateTowards(transform.rotation, target, _rotationSpeed * _lerpSpeed * Time.deltaTime);
        }
        
        public LaneNode CurrentNode
        {
            get => _currentNode;
        }

        public float VehicleLength
        {
            get => _vehicleLength;
        }

        public double TotalDistance
        {
            get => _totalDistance;
        }
    }
}