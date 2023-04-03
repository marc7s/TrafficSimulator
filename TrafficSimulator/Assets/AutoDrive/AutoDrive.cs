using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using EVP;
using RoadGenerator;
using CustomProperties;
using DataModel;
using Simulation;


namespace Car {
    public enum DrivingMode 
    {
        Performance,
        Quality
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
    public enum Activity 
    {
        Driving,
        Parked
    }
    
    public class AutoDrive : MonoBehaviour
    {
        private enum Status
        {
            Driving,
            RepositioningInitiated,
            Repositioning
        }

        [Header("Connections")]
        public Road Road;
        public GameObject NavigationTargetMarker;
        public Material NavigationPathMaterial;
        public int LaneIndex = 0;
        [SerializeField] private GameObject _mesh;

        [Header("Settings")]
        public DrivingMode Mode = DrivingMode.Quality;
        public Activity Active = Activity.Driving;
        public RoadEndBehaviour EndBehaviour = RoadEndBehaviour.Loop;
        public bool ShowNavigationPath = false;
        public NavigationMode OriginalNavigationMode = NavigationMode.Disabled;
        public bool LogRepositioningInformation = true;

        [Header("Quality mode settings")]
        public ShowTargetLines ShowTargetLines = ShowTargetLines.None;
        [Tooltip("How far from the stopping point the vehicle will come to a full stop at")] public float BrakeOffset = 5f;
        public float MaxRepositioningSpeed = 5f;
        public float MaxReverseDistance = 20f;
        [Range(0, 20f)] [Tooltip("The distance the vehicle will look ahead to find the next target. This value will be multiplied by the current speed to increase the lookahead distance when the vehicle is going faster")] public float BaseTLD = 10f;
        [Tooltip("This constant is used to divide the speed multiplier when calculating the new TLD. Higher value = shorter TLD. Lower value = longer TLD")] public int TLDSpeedDivider = 20;
        [Tooltip("This constant determines the offset to extend the bounds the vehicle uses to occupy nodes")] public float VehicleOccupancyOffset = 3f;

        [Header("Performance mode settings")]
        [Range(0, 100f)] public float Speed = 20f;
        [Range(1f, 10f)] public float Acceleration = 7f;
        [Range(2f, 20f)] public float RotationSpeed = 5f;

        [Header("Statistics")]
        [ReadOnly] public float TotalDistance = 0;
        
        // Public variables
        public LaneNode CustomStartNode = null;

        // Private variables
        private float _vehicleLength;
        private Bounds _vehicleBounds;
        private AutoDriveAgent _agent;
        private BrakeController _brakeController;
        private NavigationController _navigationController;
        
        private float _originalMaxSpeedForward;
        private float _originalMaxSpeedReverse;
        private HashSet<LaneNode> _occupiedNodes = new HashSet<LaneNode>();
        private float _lerpSpeed;
        private LaneNode _target;
        private LineRenderer _targetLineRenderer;
        private Rigidbody _rigidbody;

        // Quality variables
        private const int _repositioningOffset = 1;
        private Status _status = Status.Driving;
        private float _targetLookaheadDistance = 0;
        private const float _intersectionLookaheadDistance = 7f;
        private const float _intersectionMaxSpeed = 4f;
        private LaneNode _repositioningTarget;
        private VehicleController _vehicleController;

        private bool _isSetup = false;

        void Start()
        {
            if(!_isSetup)
                Setup();
        }

        public void Setup()
        {
            Road.RoadSystem.Setup();

            _vehicleController = GetComponent<VehicleController>();
            _vehicleLength = _mesh.GetComponent<MeshRenderer>().bounds.size.z;
            _originalMaxSpeedForward = _vehicleController.maxSpeedForward;
            _originalMaxSpeedReverse = _vehicleController.maxSpeedReverse;
            
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
            LaneNode currentNode = CustomStartNode == null ? lane.StartNode : CustomStartNode;
            _target = currentNode;

            _rigidbody = GetComponent<Rigidbody>();

            GetComponent<Vehicle>().CurrentSpeedFunction = GetCurrentSpeed;
            
            // Setup target line renderer
            float targetLineWidth = 0.3f;
            _targetLineRenderer = GetComponent<LineRenderer>();
            _targetLineRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            _targetLineRenderer.sharedMaterial.SetColor("_Color", Color.green);
            _targetLineRenderer.startWidth = targetLineWidth;
            _targetLineRenderer.endWidth = targetLineWidth;

            _agent = new AutoDriveAgent(
                new AutoDriveSetting(GetComponent<Vehicle>(), Mode, Active, EndBehaviour, _vehicleController, BrakeOffset, Speed, Acceleration, NavigationTargetMarker, NavigationPathMaterial),
                new AutoDriveContext(Road, currentNode, transform.position, OriginalNavigationMode)
            );

            _agent.Context.BrakeTarget = _agent.Context.CurrentNode;

            // Teleport the vehicle to the start of the lane
            ResetToNode(_agent.Context.CurrentNode);
            
            if (Mode == DrivingMode.Quality)
            {
                _repositioningTarget = _agent.Context.CurrentNode;
            }
            else if (Mode == DrivingMode.Performance)
            {
                // In performance mode the vehicle should not be affected by physics or gravity
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = false;
                _target = _agent.Context.CurrentNode;
                _lerpSpeed = Speed;
            }

            // Setup the controller that handles the braking
            _brakeController = new BrakeController();

            // Setup the controller that handles callbacks for intersection entry and exit
            _navigationController = new NavigationController();

            _navigationController.OnIntersectionEntry += IntersectionEntryHandler;
            _navigationController.OnIntersectionExit += IntersectionExitHandler;

            _isSetup = true;

            UpdateOccupiedNodes();
        }

        void Update()
        {
            UpdateContext();
            UpdateOccupiedNodes();
            SetActivity();

            if (ShowTargetLines != ShowTargetLines.None)
                DrawTargetLines();
        }

        private void UpdateContext()
        {
            _agent.Context.VehiclePosition = transform.position;
        }

        private void IntersectionEntryHandler(Intersection intersection)
        {
           _agent.Context.PrevIntersection = intersection;
        }

        private void IntersectionExitHandler(Intersection intersection)
        {
            _vehicleController.maxSpeedForward = _originalMaxSpeedForward;
            _agent.UnsetIntersectionTransition(intersection);
        }

        private void SetActivity()
        {
            switch (Active)
            {
                case Activity.Driving:
                    if (Mode == DrivingMode.Quality)
                    {
                        Q_Brake();
                        Q_SteerTowardsTarget();
                        Q_UpdateTarget();
                        Q_UpdateCurrent();
                    }
                    else if (Mode == DrivingMode.Performance)
                    {
                        // Brake if needed
                        P_UpdateTargetAndCurrent();
                    }
                    break;
                case Activity.Parked:
                    break;
                default:
                    break;
            }
        }

        private void ResetToNode(LaneNode node)
        {
            _agent.Context.CurrentNode = node;
            _target = node;
            _agent.Context.BrakeTarget = node;
            _repositioningTarget = node;
            _agent.Context.IsEnteringNetwork = true;

            if(_agent.Setting.Mode == DrivingMode.Quality)
                Q_TeleportToLane();
            else
                P_TeleportToLane();
            transform.rotation = node.Rotation;
            _agent.Context.NavigationMode = OriginalNavigationMode;
            SetInitialPrevIntersection();

            _agent.ClearIntersectionTransitions();
            
            if (_agent.Context.NavigationMode == NavigationMode.RandomNavigationPath)
                _agent.UpdateRandomPath(node, ShowNavigationPath);
        }

        public float GetCurrentSpeed()
        {
            return _rigidbody.velocity.magnitude;
        }

        // Update the list of nodes that the vehicle is currently occupying
        private void UpdateOccupiedNodes()
        {
            foreach(LaneNode node in _occupiedNodes)
                node.UnsetVehicle(_agent.Setting.Vehicle);

            (HashSet<LaneNode> forwardSpanNodes, HashSet<LaneNode> backwardSpanNodes) = GetVehicleSpanNodes();
            _occupiedNodes.Clear();
            
            // Start adding the nodes behind the car
            AddSpanNodes(backwardSpanNodes);
            
            // Since we want them in order and the backward nodes are added from the car outwards, reverse the list
            _occupiedNodes.Reverse();

            // Add the nodes in front of the car
            AddSpanNodes(forwardSpanNodes);
        }

        private void AddSpanNodes(HashSet<LaneNode> spanNodes)
        {
            foreach (LaneNode node in spanNodes)
            {
                // Add the span nodes we successfully acquire to the list of occupied nodes until we fail to acquire one, then break
                // This avoids the vehicle from occupying nodes with gaps between them, which could cause a lockup if the vehicle has acquired nodes in front of and behind another vehicle
                if(node.SetVehicle(_agent.Setting.Vehicle))
                    _occupiedNodes.Add(node);
                else
                    break;
            }
        }

        // Get the list of nodes that the vehicle is currently occupying by moving backwards from the current position until out of vehicle bounds
        private (HashSet<LaneNode>, HashSet<LaneNode>) GetVehicleSpanNodes()
        {
            HashSet<LaneNode> forwardNodes = new HashSet<LaneNode>(){ _agent.Context.CurrentNode };
            HashSet<LaneNode> backwardNodes = new HashSet<LaneNode>();
            LaneNode node = _agent.Prev(_agent.Context.CurrentNode);

            float distanceToCurrentNode = Vector3.Distance(transform.position, _agent.Context.CurrentNode.Position);

            float nodeDistance = _agent.Context.CurrentNode.DistanceToPrevNode;

            // Add all occupied nodes prior to and including the current node
            while (node != null && nodeDistance <= distanceToCurrentNode + _vehicleLength / 2 + VehicleOccupancyOffset)
            {
                backwardNodes.Add(node);
                nodeDistance += node.DistanceToPrevNode;
                node = _agent.Prev(node, RoadEndBehaviour.Stop);
            }
            
            nodeDistance = 0;
            // Add all occupied nodes after and excluding the current node
            node = _agent.Next(_agent.Context.CurrentNode);
            while (node != null && nodeDistance <= distanceToCurrentNode + _vehicleLength / 2 + VehicleOccupancyOffset)
            {
                forwardNodes.Add(node);
                nodeDistance += node.DistanceToPrevNode;
                node = _agent.Next(node, RoadEndBehaviour.Stop);
            }
            
            return (forwardNodes, backwardNodes);
        }

        private void Q_TeleportToLane()
        {
            // Rotate it to face the current position
            _vehicleController.cachedRigidbody.MoveRotation(_agent.Context.CurrentNode.Rotation);
            
            // Move it to the current position, offset in the opposite direction of the lane
            _vehicleController.cachedRigidbody.position = _agent.Context.CurrentNode.Position - (2 * (_agent.Context.CurrentNode.Rotation * Vector3.forward).normalized);
            transform.position = _vehicleController.cachedRigidbody.position;

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
            _targetLookaheadDistance = target.RoadNode.Intersection != null ? _intersectionLookaheadDistance : Math.Max(BaseTLD * _vehicleController.speed / TLDSpeedDivider, BaseTLD);

            // If the vehicle is driving and the target is behind us and too far away
            if (_status == Status.Driving && dot < 0 && direction.magnitude > _targetLookaheadDistance + 1f)
            {
                if(LogRepositioningInformation)
                    Debug.Log("Repositioning started, slowing down...");
                _status = Status.RepositioningInitiated;

                // Reposition to the point prior to the one we missed
                _repositioningTarget = GetNextLaneNode(_target, -_repositioningOffset - 1, false);

                // Slow down and limit the max speed to the repositioning speed or the max speed, whichever is lower
                _vehicleController.brakeInput = 1f;
                _vehicleController.maxSpeedForward = Math.Min(_vehicleController.maxSpeedForward, MaxRepositioningSpeed);
                _vehicleController.maxSpeedReverse = Math.Min(_vehicleController.maxSpeedReverse, MaxRepositioningSpeed);
            }
            // If the vehicle has started repositioning and slowed down enough
            else if (_status == Status.RepositioningInitiated && _vehicleController.speed <= _vehicleController.maxSpeedForward)
            {
                if(LogRepositioningInformation)
                    Debug.Log("Slowed down, releasing brakes and repositioning...");
                _status = Status.Repositioning;

                _vehicleController.brakeInput = 0f;
            }
            // If the vehicle is currently repositioning
            else if (_status == Status.Repositioning) 
            {
                // Allow the vehicle to accelerate and reverse. It will only reverse if the target is behind it, and within reversing distance
                _vehicleController.throttleInput = Vector3.Distance(transform.position, target.Position) < MaxReverseDistance && dot < 0 ? -1 : 1;
                
                // If the target is in front of us and we are close enough we have successfully repositioned
                if (dot > 0 && direction.magnitude <= _targetLookaheadDistance - 1f) 
                {
                    if(LogRepositioningInformation)
                        Debug.Log("Repositioned, speeding back up...");
                    _status = Status.Driving;

                    // Assume that the car travelled straight to the repositioning target
                    TotalDistance += Vector3.Distance(_agent.Context.CurrentNode.Position, _target.Position);
                    
                    // Update the current node
                    _agent.Context.CurrentNode = _target;
                    
                    // Set the target to the one after the target we missed
                    _target = GetNextLaneNode(_repositioningTarget, _repositioningOffset, false);
                    
                    // Reset the max speed to the original, and set the acceleration to the max again
                    _vehicleController.maxSpeedForward = _originalMaxSpeedForward;
                    _vehicleController.maxSpeedReverse = _originalMaxSpeedReverse;
                    _vehicleController.throttleInput = 1f;
                    _vehicleController.brakeInput = 0f;
                }
            }
            // If the vehicle is driving and the target is in front of us and we are close enough
            else if (_status == Status.Driving && dot > 0 && direction.magnitude <= _targetLookaheadDistance)
            {
                bool trafficLightShouldStop = _agent.Context.BrakeTarget.RoadNode.TrafficLight?.CurrentState == TrafficLightState.Red && _agent.Context.BrakeTarget.Intersection?.ID != _agent.Context.PrevIntersection?.ID;
                // when the target is the brake target and the traffic light is red, do not change the target
                if (trafficLightShouldStop && _target == _agent.Context.BrakeTarget)
                    return;
                
                if(_target.Intersection != null)
                    _vehicleController.maxSpeedForward = _intersectionMaxSpeed;

                // Set the target to the next point in the lane
                _target = GetNextLaneNode(_target, 0, false);
            }
        }

        private void Q_Brake()
        {
            if(_brakeController.ShouldAct(ref _agent))
            {
                _vehicleController.brakeInput = 0.2f;
                _vehicleController.throttleInput = 0f;
            }
            else
            {
                _vehicleController.brakeInput = 0f;
                _vehicleController.throttleInput = 0.5f;
            }
        }

        private LaneNode Q_GetNextCurrentNode()
        {
            return _agent.Next(_agent.Context.CurrentNode);
        }

        private void Q_UpdateCurrent()
        {
            LaneNode nextNode = Q_GetNextCurrentNode();
            LaneNode nextNextNode = GetNextLaneNode(nextNode, 0, false);
            bool reachedEnd = !_agent.Context.IsEnteringNetwork && _agent.Context.CurrentNode.Type == RoadNodeType.End;

            // Move the current node forward while we are closer to the next node than the current. Also check the node after the next as the next may be further away in intersections where the current road is switched
            // Note: only updates while driving. During repositioning the vehicle will be closer to the next node (the repositioning target) halfway through the repositioning
            // This would cause our current position to skip ahead so repositioning is handled separately
            
            while(!reachedEnd && _status == Status.Driving)
            {
                bool isCloserToNextThanCurrentNode = Vector3.Distance(transform.position, nextNode.Position) <= Vector3.Distance(transform.position, _agent.Context.CurrentNode.Position);
                bool isCloserToNextNextThanCurrentNode = Vector3.Distance(transform.position, nextNextNode.Position) <= Vector3.Distance(transform.position, _agent.Context.CurrentNode.Position);
                // If the next or next next node is further away than our current position, we should not update the current node
                if(!(isCloserToNextThanCurrentNode || isCloserToNextNextThanCurrentNode))
                    break;

                _agent.UnsetIntersectionTransition(_agent.Context.CurrentNode.Intersection);
                
                TotalDistance += _agent.Context.CurrentNode.DistanceToPrevNode;
                _agent.Context.CurrentNode = nextNode;
                
                // All logic for the navigation controller is handled through the actions, so we ignore the return value
                _navigationController.ShouldAct(ref _agent);
                
                // When the current node is updated, it needs to redraw the navigation path
                if (ShowNavigationPath)
                    Navigation.DrawPathRemoveOldestPoint(ref _agent.Context.NavigationPathPositions, _agent.Context.NavigationPathContainer);
                
                nextNode = Q_GetNextCurrentNode();
                nextNextNode = GetNextLaneNode(nextNode, 0, false);
                reachedEnd = reachedEnd || (!_agent.Context.IsEnteringNetwork && _agent.Context.CurrentNode.Type == RoadNodeType.End);
            }

            // If the road ended but we are looping, teleport to the first position
            if(reachedEnd && EndBehaviour == RoadEndBehaviour.Loop)
                ResetToNode(_agent.Context.StartNode);

            // After the first increment of the current node, we are no longer entering the network
            if(_agent.Context.IsEnteringNetwork && _agent.Context.CurrentNode.Type != RoadNodeType.End)
                _agent.Context.IsEnteringNetwork = false;
        }

        private LaneNode Q_GetTarget()
        {
            return _status == Status.Driving ? _target : _repositioningTarget;
        }

        private LaneNode GetNextLaneNode(LaneNode currentNode, int offset = 0, bool wrapAround = true)
        {
            LaneNode node = currentNode;
            RoadEndBehaviour endBehaviour = wrapAround ? RoadEndBehaviour.Loop : RoadEndBehaviour.Stop;
            for(int i = 0; i < Math.Abs(offset) + 1; i++)
            {
                LaneNode nextNode = offset >= 0 ? _agent.Next(node, endBehaviour) : _agent.Prev(node, endBehaviour);

                if(nextNode == null)
                    return node;
                
                node = nextNode;
            }
            return node;
        }
        
        // Draw lines towards steering and braking target
        private void DrawTargetLines()
        {
            switch (ShowTargetLines)
            {
                case ShowTargetLines.Target:
                    _targetLineRenderer.positionCount = 2;
                    _targetLineRenderer.SetPositions(new Vector3[]{ Q_GetTarget().Position, transform.position });
                    break;
                case ShowTargetLines.BrakeTarget:
                    _targetLineRenderer.positionCount = 2;
                    _targetLineRenderer.SetPositions(new Vector3[]{ _agent.Context.BrakeTarget.Position, transform.position });
                    break;
                case ShowTargetLines.CurrentPosition:
                    _targetLineRenderer.positionCount = 2;
                    _targetLineRenderer.SetPositions(new Vector3[]{ _agent.Context.CurrentNode.Position, transform.position });
                    break;
                case ShowTargetLines.OccupiedNodes:
                    _targetLineRenderer.positionCount = _occupiedNodes.Count;
                    _targetLineRenderer.SetPositions(_occupiedNodes.Select(x => x.Position).ToArray());
                    break;
                case ShowTargetLines.All:
                    _targetLineRenderer.positionCount = 3 + _occupiedNodes.Count;
                    _targetLineRenderer.SetPositions(new Vector3[]{ _agent.Context.BrakeTarget.Position, transform.position, _agent.Context.CurrentNode.Position, transform.position}.Concat(_occupiedNodes.Select(x => x.Position)).ToArray());
                    break;
            }  
        }

        private void SetInitialPrevIntersection()
        {
            _agent.Context.PrevIntersection = null;
            
            // If the starting node is an intersection, the previous intersection is set 
            if (_target.Intersection != null)
                _agent.Context.PrevIntersection = _target.Intersection;
                
            LaneNode nextNode = _agent.Next(_target);
            
            // If the starting node is at a three way intersection, the target will be an EndNode but the next will be an intersection node, so we need to set the previous intersection
            if (nextNode != null && nextNode.Intersection != null && nextNode.IsIntersection())
                _agent.Context.PrevIntersection = nextNode.Intersection;

            LaneNode prevNode = _agent.Prev(_target);
            
            // If the starting node is a junction edge, the previous intersection is set
            if (prevNode != null && prevNode.Intersection != null && prevNode.IsIntersection())
                _agent.Context.PrevIntersection = prevNode.Intersection;
        }

        private void P_TeleportToLane()
        {
            // Move to the first position of the lane
            transform.position = P_Lift(_agent.Context.CurrentNode.Position);
            transform.rotation = _agent.Context.CurrentNode.Rotation;
        }

        // Performance methods

        // Move the vehicle to the target node
        private void P_MoveTowardsTargetNode()
        {
            Vector3 targetPosition = P_GetLerpPosition(_target.Position);
            Quaternion targetRotation = P_GetLerpQuaternion(_target.Rotation);

            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        private void P_UpdateTargetAndCurrent()
        {
            _lerpSpeed = P_GetLerpSpeed(_brakeController.ShouldAct(ref _agent) ? 0f : Speed);
            
            // Update the target if we have reached the current target, and we do not need to brake
            if (P_HasReachedTarget(_target))
            {
                TotalDistance += _target.DistanceToPrevNode;
                _agent.Context.CurrentNode = _target;

                if(_target == _agent.Context.EndNode)
                {
                    ResetToNode(_agent.Context.StartNode);
                    _target = _agent.Context.StartNode;
                }

                // All logic for the navigation controller is handled through the actions, so we ignore the return value
                _navigationController.ShouldAct(ref _agent);

                // When the currentNode is changed, the navigation path needs to be updated
                if (ShowNavigationPath)
                    Navigation.DrawPathRemoveOldestPoint(ref _agent.Context.NavigationPathPositions, _agent.Context.NavigationPathContainer);
            
                _target = GetNextLaneNode(_target, 0, EndBehaviour == RoadEndBehaviour.Loop);
            }

            P_MoveTowardsTargetNode();
        }

        private bool P_HasReachedTarget(LaneNode target)
        {
            // Since the target position will be lifted in performance mode, we need to compare the XZ coordinates
            Vector3 targetPosition = target.Position;
            targetPosition.y = transform.position.y;
            
            return transform.position == targetPosition;
        }

        private Vector3 P_Lift(Vector3 position)
        {
            return position + Vector3.up * 0.1f;
        }

        public void SetNavigationPathVisibilty(bool visible)
        {
            if (_agent != null && _agent.Context.NavigationPathContainer != null)
            {
                if (_agent.Context.NavigationPathContainer.transform.childCount > 0)
                {
                    _agent.Context.NavigationPathContainer.transform.GetChild(0).gameObject.SetActive(visible);
                    _agent.Context.NavigationPathContainer.GetComponent<LineRenderer>().enabled = visible;
                }

                if(visible)
                    Navigation.DrawNavigationPath(out _agent.Context.NavigationPathPositions, _agent.Context.NavigationPathEndNode, _agent.Context.NavigationPath, _agent.Context.CurrentNode, _agent.Context.NavigationPathContainer, _agent.Setting.NavigationPathMaterial, _agent.Context.PrevIntersection, _agent.Setting.NavigationTargetMarker);
            }
        }

        private float P_GetLerpSpeed(float target)
        {
            return Mathf.MoveTowards(_lerpSpeed, target, Acceleration * Time.deltaTime);
        }
        private Vector3 P_GetLerpPosition(Vector3 target)
        {
            return Vector3.MoveTowards(transform.position, P_Lift(target), _lerpSpeed * Time.deltaTime);
        }
        private Quaternion P_GetLerpQuaternion(Quaternion target)
        {
            return Quaternion.RotateTowards(transform.rotation, target, RotationSpeed * _lerpSpeed * Time.deltaTime);
        }
    }
}