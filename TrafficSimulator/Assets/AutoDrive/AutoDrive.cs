using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using System;
using EVP;
using RoadGenerator;
using CustomProperties;
using DataModel;
using Simulation;
using POIs;

namespace VehicleBrain 
{
    public enum DrivingMode 
    {
        Performance,
        Quality
    }
    public enum VehicleType
    {
        Car,
        Bus,
        Tram,
        Unknown
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
    public enum VehicleActivity 
    {
        Driving,
        Parked,
        Waiting
    }
    
    public class AutoDrive : MonoBehaviour
    {
        private enum Status
        {
            Driving,
            RepositioningInitiated,
            Repositioning
        }

        // Used for road registration
        public delegate void RoadChangedDelegate(Road newRoad);
        public RoadChangedDelegate OnRoadChanged;
        
        private struct SpanNodes
        {
            public HashSet<LaneNode> ForwardClaimNodes;
            public HashSet<LaneNode> ForwardReleaseNodes;
            public HashSet<LaneNode> BackwardClaimNodes;
            public HashSet<LaneNode> BackwardReleaseNodes;
            public SpanNodes(HashSet<LaneNode> forwardClaimNodes, HashSet<LaneNode> forwardReleaseNodes, HashSet<LaneNode> backwardClaimNodes, HashSet<LaneNode> backwardReleaseNodes)
            {
                ForwardClaimNodes = forwardClaimNodes;
                ForwardReleaseNodes = forwardReleaseNodes;
                BackwardClaimNodes = backwardClaimNodes;
                BackwardReleaseNodes = backwardReleaseNodes;
            }
        }

        [Header("Connections")]
        [SerializeField] private Road _road;
        
        public GameObject NavigationTargetMarker;
        public Material NavigationPathMaterial;
        public int LaneIndex = 0;
        [SerializeField] private GameObject _mesh;

        [Header("Settings")]
        public DrivingMode Mode = DrivingMode.Quality;
        public RoadEndBehaviour EndBehaviour = RoadEndBehaviour.Loop;
        public bool ShowNavigationPath = false;
        public NavigationMode OriginalNavigationMode = NavigationMode.Disabled;

        [Header("Quality mode settings")]
        [Tooltip("How far from the stopping point the vehicle will come to a full stop at")] public float BrakeOffset = 5f;
        public float MaxRepositioningSpeed = 5f;
        public float MaxReverseDistance = 20f;
        [Range(0, 20f)] [Tooltip("The distance the vehicle will look ahead to find the next target. This value will be multiplied by the current speed to increase the lookahead distance when the vehicle is going faster")] public float BaseTLD = 10f;
        [Tooltip("This constant is used to divide the speed multiplier when calculating the new TLD. Higher value = shorter TLD. Lower value = longer TLD")] public int TLDSpeedDivider = 20;
        [Tooltip("This constant determines the offset to extend the bounds the vehicle uses to occupy nodes")] public float VehicleOccupancyOffset = 3f;

        [Header("Performance mode settings")]
        [Range(1f, 100f)] public float Speed = 10f;
        [Range(2f, 10f)] public float Acceleration = 7f;

        [Header("Statistics")]
        [ReadOnly] public float TotalDistance = 0;

        [Header("Debug Settings")]
        public ShowTargetLines ShowTargetLines = ShowTargetLines.None;
        public bool LogRepositioningInformation = true;
        public bool LogNavigationErrors = false;
        public bool LogBrakeReason = false;
        [SerializeField] private bool _logParkingFull = false;
        
        // Public variables
        [HideInInspector] public LaneNode CustomStartNode = null;

        // Private variables
        private float _vehicleLength;
        private AutoDriveAgent _agent;
        private BrakeController _brakeController;
        private NavigationController _navigationController;
        private BrakeLightController _brakeLightController;
        
        private float _originalMaxSpeedForward;
        private float _originalMaxSpeedReverse;
        private List<LaneNode> _occupiedNodes = new List<LaneNode>();
        
        private LaneNode _prevTarget;
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
        private IndicatorController _indicatorController;

        private bool _isSetup = false;

        // Performance variables
        private float _lerpSpeed = 0;
        private float _targetLerpSpeed = 0;
        private float _timeElapsedSinceLastTarget = 0;
        private float _lastLerpTime = 0;

        public Road Road
        {
            get => _road;
            set => SetRoad(value);
        }

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
            if (Mode == DrivingMode.Quality)
            {
                _originalMaxSpeedForward = _vehicleController.maxSpeedForward;
                _originalMaxSpeedReverse = _vehicleController.maxSpeedReverse;
            }

            _brakeLightController = GetComponent<BrakeLightController>();
            _indicatorController = GetComponent<IndicatorController>();
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
            _prevTarget = currentNode;

            _rigidbody = GetComponent<Rigidbody>();

            GetComponent<Vehicle>().CurrentSpeedFunction = GetCurrentSpeed;
            
            // Setup target line renderer
            float targetLineWidth = 0.3f;
            _targetLineRenderer = GetComponent<LineRenderer>();
            _targetLineRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            _targetLineRenderer.sharedMaterial.SetColor("_Color", Color.green);
            _targetLineRenderer.startWidth = targetLineWidth;
            _targetLineRenderer.endWidth = targetLineWidth;


            VehicleType vehicleType = 
                GetComponent<Bus>() != null ? VehicleType.Bus
                : GetComponent<Tram>() != null ? VehicleType.Tram
                : GetComponent<Car>() != null ? VehicleType.Car
                : VehicleType.Unknown;
            
            if(vehicleType == VehicleType.Unknown)
            {
                Debug.LogError("Could not determine vehicle type");
                return;
            }

            _agent = new AutoDriveAgent(
                new AutoDriveSetting(GetComponent<Vehicle>(), vehicleType, Mode, EndBehaviour, _vehicleController, BrakeOffset, Speed, Acceleration, NavigationTargetMarker, NavigationPathMaterial),
                new AutoDriveContext(currentNode, transform.position, OriginalNavigationMode, LogNavigationErrors, LogBrakeReason)
            );

            _agent.Context.BrakeTarget = _agent.Context.CurrentNode;
            
            if (Mode == DrivingMode.Quality)
            {
                _repositioningTarget = _agent.Context.CurrentNode;
            }
            else if (Mode == DrivingMode.Performance)
            {
                // In performance mode the vehicle should not be affected by physics or gravity
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = false;
                _lerpSpeed = Speed;
                _targetLerpSpeed = Speed;
            }

            // Setup the controller that handles the braking
            _brakeController = new BrakeController(ref _agent);

            // Setup the controller that handles callbacks for intersection entry and exit
            _navigationController = new NavigationController();

            _navigationController.OnIntersectionEntry += IntersectionEntryHandler;
            _navigationController.OnIntersectionExit += IntersectionExitHandler;

            // Teleport the vehicle to the start of the lane
            ResetToNode(_agent.Context.CurrentNode);
            UpdateOccupiedNodes();
            _isSetup = true;
        }

        void Update()
        {
            HandleActivity();
            
            if (ShowTargetLines != ShowTargetLines.None)
                DrawTargetLines();
        }
        
        private void SetRoad(Road newRoad)
        {
            if (_road != newRoad)
            {
                _road = newRoad;
                OnRoadChanged?.Invoke(_road);
            }
        }

        private void UpdateIndicators()
        {
            _indicatorController.SetIndicator(IndicatorController.TurnDirectionToIndicatorState(_agent.Context.TurnDirection));
        }
        private void UpdateContext()
        {
            _agent.Context.VehiclePosition = transform.position;
            _agent.Context.CurrentAction = GetCurrentDrivingAction();
        }

        private void IntersectionEntryHandler(Intersection intersection)
        {
           _agent.Context.PrevIntersection = intersection;
           _agent.Context.IsInsideIntersection = true;
           _agent.Context.PrevEntryNodes[intersection] = _agent.Context.CurrentNode;
        }

        private void IntersectionExitHandler(Intersection intersection)
        {
            _vehicleController.maxSpeedForward = _originalMaxSpeedForward;
            _agent.UnsetIntersectionTransition(intersection, _agent.Context.PrevEntryNodes[intersection]);
            _agent.Context.TurnDirection = TurnDirection.Straight;
            _agent.Context.IsInsideIntersection = false;
        }

        private void HandleActivity()
        {
            switch (_agent.Context.Activity)
            {
                case VehicleActivity.Driving:
                    UpdateContext();
                    UpdateOccupiedNodes();

                    if (Mode == DrivingMode.Quality)
                    {
                        Q_Brake();
                        Q_SteerTowardsTarget();
                        Q_UpdateTarget();
                        Q_UpdateCurrent();
                    }
                    else if (Mode == DrivingMode.Performance)
                    {
                        _timeElapsedSinceLastTarget += Time.deltaTime;
                        // Brake if needed
                        P_UpdateTargetAndCurrent();
                    }

                    if(_agent.Context.Activity != VehicleActivity.Driving)
                        break;
                    
                    _brakeLightController.SetBrakeLights(_agent.Context.IsBrakingOrStopped ?  BrakeLightState.On : BrakeLightState.Off);
                    UpdateIndicators();
                    
                    break;
                case VehicleActivity.Parked:
                    break;
                case VehicleActivity.Waiting:
                    break;
                default:
                    break;
            }
        }

        private void ResetToNode(LaneNode node)
        {
            ResetNodeParameters(node);
            TeleportToNode(node);
            PostTeleportCleanup(node);
            
            if(_agent.Context.Activity == VehicleActivity.Driving)
            {
                switch(_agent.Context.NavigationMode)
                {
                    case NavigationMode.RandomNavigationPath:
                        // Generate a new random path when resetting to a node
                        _agent.UpdateRandomPath(node, ShowNavigationPath);
                        break;
                    case NavigationMode.Path:
                        // Generate a new path if the current one is empty since there might be targets left
                        if(_agent.Context.NavigationPathTargets.Count < 1)
                            _agent.GeneratePath(node, ShowNavigationPath);
                        break;
                }
            }
            
            UpdateOccupiedNodes();
        }

        private void ParkAtNode(POINode node, ParkingLineup parkingLineup)
        {
            _agent.Context.Activity = VehicleActivity.Parked;
            LaneNode parkingEntry = _agent.Context.CurrentNode;
            ResetNodeParameters(null);
            TeleportToNode(node, false);
            PostTeleportCleanup(node, parkingLineup == ParkingLineup.Random);
            SetVehicleMovement(false);
            
            // Unset all occupied nodes
            for(int i = _occupiedNodes.Count - 1; i >= 0; i--)
            {
                _occupiedNodes[i].UnsetVehicle(_agent.Setting.Vehicle);
                _occupiedNodes.RemoveAt(i);
            }
            
            _agent.Context.IsEnteringNetwork = false;
            
            // Use prev target to store the entry node to the parking
            _prevTarget = parkingEntry;
        }

        /// <summary> Unparks the vehicle from the current parking once it is free to do so </summary>
        private IEnumerator Unpark()
        {
            // Wait until the exit not is not occupied before unparking
            yield return new WaitUntil(() => !(_prevTarget.HasVehicle() && _prevTarget.Vehicle != _agent.Setting.Vehicle));
            
            _agent.Context.CurrentParking.Unpark(_agent.Setting.Vehicle);
            _agent.Context.CurrentParking = null;
            _agent.Context.Activity = VehicleActivity.Driving;
            SetVehicleMovement(true);
            
            // The entry node to the parking was stored in prev target, so teleport back to that node
            ResetToNode(_prevTarget);
        }

        private void SetVehicleMovement(bool enabled)
        {
            RigidbodyPause rigidbodyPause = _vehicleController.GetComponent<RigidbodyPause>();
            rigidbodyPause.pause = !enabled;
        }

        private void ResetNodeParameters(LaneNode node)
        {
            _agent.Context.CurrentNode = node;
            _target = node;
            _prevTarget = node;
            _agent.Context.PrevTarget = null;
            _lastLerpTime = 0;
            _agent.Context.BrakeTarget = node;
            _repositioningTarget = node;
            _agent.Context.IsEnteringNetwork = true;
        }

        private void PostTeleportCleanup(LaneNode node)
        {
            transform.rotation = node.Rotation;
            PostTeleportNavigationClear();
        }

        private void PostTeleportCleanup(POINode node, bool randomRotation = false)
        {
            transform.rotation = node.Rotation * (randomRotation && UnityEngine.Random.Range(0, 2) == 0 ? Quaternion.Euler(Vector3.up * 180) : Quaternion.identity);
            PostTeleportNavigationClear();
        }

        private void PostTeleportNavigationClear()
        {
            _agent.Context.NavigationMode = OriginalNavigationMode;
            SetInitialPrevIntersection();
        }

        public float GetCurrentSpeed()
        {
            return _agent.Setting.Mode == DrivingMode.Quality ? _rigidbody.velocity.magnitude : _lerpSpeed;
        }

        // Update the list of nodes that the vehicle is currently occupying
        private void UpdateOccupiedNodes()
        {
            SpanNodes spanNodes = GetVehicleSpanNodes();

            ClearSpanNodes(spanNodes.ForwardReleaseNodes, spanNodes.BackwardReleaseNodes);
            
            // Start adding the nodes behind the car
            AddSpanNodes(spanNodes.BackwardClaimNodes);

            // Add the nodes in front of the car
            AddSpanNodes(spanNodes.ForwardClaimNodes);

            _occupiedNodes.Sort((x, y) => x.Index.CompareTo(y.Index));
        }

        private void ClearSpanNodes(HashSet<LaneNode> forwardNodes, HashSet<LaneNode> backwardNodes)
        {
            for (int i = _occupiedNodes.Count - 1; i >= 0; i--)
            {
                if(!forwardNodes.Contains(_occupiedNodes[i]) && !backwardNodes.Contains(_occupiedNodes[i]))
                {
                    _occupiedNodes[i].UnsetVehicle(_agent.Setting.Vehicle);
                    _occupiedNodes.Remove(_occupiedNodes[i]);
                }
            }
        }

        private void AddSpanNodes(HashSet<LaneNode> spanNodes)
        {
            foreach (LaneNode node in spanNodes)
            {
                if(_occupiedNodes.Contains(node))
                    continue;
                
                // Add the span nodes we successfully acquire to the list of occupied nodes until we fail to acquire one, then break
                // This avoids the vehicle from occupying nodes with gaps between them, which could cause a lockup if the vehicle has acquired nodes in front of and behind another vehicle
                if(node.SetVehicle(_agent.Setting.Vehicle))
                    _occupiedNodes.Add(node);
                else
                    break;
            }
        }

        /// <summary> Update the span distances and return whether the node distance is within that. 
        /// The release distance is greater than the claim distance when moving so that the flickering claim/release behaviour is removed </summary>
        private bool WithinSpanDistance(float nodeDistance, float maxDistance, float currentSpeed, ref bool isWithinClaimDistance, ref bool isWithinReleaseDistance)
        {
            isWithinClaimDistance = nodeDistance < maxDistance;
            isWithinReleaseDistance = nodeDistance < maxDistance + (currentSpeed > 0 ? 1f : 0);
            
            return isWithinClaimDistance || isWithinReleaseDistance;
        }

        // Get the list of nodes that the vehicle is currently occupying by moving backwards from the current position until out of vehicle bounds
        private SpanNodes GetVehicleSpanNodes()
        {
            HashSet<LaneNode> forwardClaimNodes = new HashSet<LaneNode>(){ _agent.Context.CurrentNode };
            HashSet<LaneNode> forwardReleaseNodes = new HashSet<LaneNode>(forwardClaimNodes);
            
            HashSet<LaneNode> backwardClaimNodes = new HashSet<LaneNode>();
            HashSet<LaneNode> backwardReleaseNodes = new HashSet<LaneNode>(backwardClaimNodes);
            
            LaneNode node = _agent.Prev(_agent.Context.CurrentNode, RoadEndBehaviour.Stop);
            LaneNode last = node;

            float distanceToCurrentNode = Vector3.Distance(transform.position, _agent.Context.CurrentNode.Position);
            
            float nodeDistance = _agent.Context.CurrentNode.DistanceToPrevNode;
            float currentSpeed = _agent.Setting.Vehicle.CurrentSpeed;

            // Occupy nodes further ahead in intersections
            // In the worst case, the nodes might be a quarter of an intersection away, which would be IntersectionLength / 4
            // Since we want some buffer to make sure they are reached, but half the IntersectionLength would be too far, we offset it by a third
            float forwardOccupancyOffset = _agent.Context.CurrentNode.Intersection != null && _agent.Setting.Vehicle.CurrentSpeed > 0 ? _agent.Context.CurrentNode.Intersection.IntersectionLength / 3 : 0;

            bool isWithinClaimDistance = false;
            bool isWithinReleaseDistance = false;

            // Add all occupied nodes prior to and excluding the current node
            while (node != null && WithinSpanDistance(nodeDistance, distanceToCurrentNode + _vehicleLength / 2 + VehicleOccupancyOffset, currentSpeed, ref isWithinClaimDistance, ref isWithinReleaseDistance))
            {
                if(isWithinClaimDistance)
                    backwardClaimNodes.Add(node);

                if(isWithinReleaseDistance)
                    backwardReleaseNodes.Add(node);
                
                if(node.IsSteeringTarget)
                    last = node;
                
                node = _agent.Prev(node, RoadEndBehaviour.Stop);
                nodeDistance += node != null && node.IsSteeringTarget ? Vector3.Distance(node.Position, last.Position) : 0;
            }

            nodeDistance = 0;
            
            // Add all occupied nodes after and including the current node
            node = _agent.Context.CurrentNode;
            if(!(node.TrafficLight != null && node.TrafficLight.CurrentState == TrafficLightState.Red && node.Intersection?.ID != _agent.Context.PrevIntersection?.ID))
            {
                last = node;
                node = _agent.Next(_agent.Context.CurrentNode, RoadEndBehaviour.Stop);
                nodeDistance += node != null && node.IsSteeringTarget ? Vector3.Distance(node.Position, last.Position) : 0;
                
                while (node != null && WithinSpanDistance(nodeDistance, _vehicleLength / 2 + distanceToCurrentNode + VehicleOccupancyOffset + forwardOccupancyOffset, currentSpeed, ref isWithinClaimDistance, ref isWithinReleaseDistance))
                {
                    // Do not occupy nodes in front of a red light
                    if(node.TrafficLight != null && node.TrafficLight.CurrentState == TrafficLightState.Red && node.Intersection?.ID != _agent.Context.PrevIntersection?.ID)
                        break;

                    // Do not occupy nodes in front of a yield sign
                    if(node.YieldSign != null)
                        break;

                    // Do not occupy nodes in front of a stop sign
                    if(node.StopSign != null)
                        break;

                    // Do not occupy nodes in front of the vehicle if it is stationary
                    if(currentSpeed < 0.1f)
                        break;
                    
                    if(isWithinClaimDistance)
                        forwardClaimNodes.Add(node);

                    if(isWithinReleaseDistance)
                        forwardReleaseNodes.Add(node);
                    
                    // Only update the last node if it was a steering target as steering targets are always occupied but do not contribute to the distance
                    if(node.IsSteeringTarget)
                        last = node;
                    
                    node = _agent.Next(node, RoadEndBehaviour.Stop);
                    nodeDistance += node != null && node.IsSteeringTarget ? Vector3.Distance(node.Position, last.Position) : 0;
                }
            }
            
            return new SpanNodes(forwardClaimNodes, forwardReleaseNodes, backwardClaimNodes, backwardReleaseNodes);
        }

        private void TeleportToNode<T>(Node<T> node, bool backwardOffset = true) where T : Node<T>
        {
            if(_agent.Setting.Mode == DrivingMode.Performance)
            {
                // Move to the first position of the lane
                transform.position = P_Lift(node.Position);
                transform.rotation = node.Rotation;
            }
            else
            {
                // Rotate it to face the current position
                _vehicleController.cachedRigidbody.MoveRotation(node.Rotation);
                
                // Move it to the current position, offset in the opposite direction of the lane
                _vehicleController.cachedRigidbody.position = backwardOffset ? node.Position - (node.Rotation * Vector3.forward).normalized : node.Position;
                transform.position = _vehicleController.cachedRigidbody.position;

                // Reset velocity and angular velocity
                _vehicleController.cachedRigidbody.velocity = Vector3.zero;
                _vehicleController.cachedRigidbody.angularVelocity = Vector3.zero;
            }
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
                Q_SetBrakeInput(1f);
                _vehicleController.maxSpeedForward = Math.Min(_vehicleController.maxSpeedForward, MaxRepositioningSpeed);
                _vehicleController.maxSpeedReverse = Math.Min(_vehicleController.maxSpeedReverse, MaxRepositioningSpeed);
            }
            // If the vehicle has started repositioning and slowed down enough
            else if (_status == Status.RepositioningInitiated && _vehicleController.speed <= _vehicleController.maxSpeedForward)
            {
                if(LogRepositioningInformation)
                    Debug.Log("Slowed down, releasing brakes and repositioning...");
                _status = Status.Repositioning;

                Q_SetBrakeInput(0f);
            }
            // If the vehicle is currently repositioning
            else if (_status == Status.Repositioning) 
            {
                // Allow the vehicle to accelerate and reverse. It will only reverse if the target is behind it, and within reversing distance
                Q_SetThrottleInput(Vector3.Distance(transform.position, target.Position) < MaxReverseDistance && dot < 0 ? -1 : 1);
                
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
                    SetTarget(GetNextLaneNode(_repositioningTarget, _repositioningOffset, false));
                    
                    // Reset the max speed to the original, and set the acceleration to the max again
                    _vehicleController.maxSpeedForward = _originalMaxSpeedForward;
                    _vehicleController.maxSpeedReverse = _originalMaxSpeedReverse;
                    Q_SetBrakeInput(0f);
                    Q_SetThrottleInput(1f);
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
                SetTarget(GetNextLaneNode(_target, 0, false));
            }
        }

        private void Park(Parking parking)
        {
            POINode parkNode = parking.Park(_agent.Setting.Vehicle);
            if(parkNode == null)
            {
                if(_logParkingFull)
                    Debug.Log("Parking full");
                return;
            }

            ParkAtNode(parkNode, parking.ParkingLineup);
            _agent.Context.CurrentParking = parking;
        }

        private void WaitAtBusStop(BusStop busStop)
        {
            _agent.Context.Activity = VehicleActivity.Waiting;
            
            // Brake at the bus stop
            _vehicleController.brakeInput = 0.7f;
            _vehicleController.throttleInput = 0f;
            
            // Indicate to the side of the bus stop and turn on the brake lights
            _indicatorController.SetIndicator(_agent.Context.CurrentRoad.RoadSystem.DrivingSide == DrivingSide.Right ? IndicatorState.Right : IndicatorState.Left);
            _brakeLightController.SetBrakeLights(BrakeLightState.On);
            
            // Wait at the bus stop
            TimeManagerEvent unPauseEvent = new TimeManagerEvent(DateTime.Now.AddMilliseconds(7000));
            TimeManager.Instance.AddEvent(unPauseEvent);
            
            // Return to driving after the vehicle is done waiting at the bus stop
            unPauseEvent.OnEvent += () => _agent.Context.Activity = VehicleActivity.Driving;
        }

        private void SetTarget(LaneNode newTarget)
        {
            _prevTarget = _target;
            _target = newTarget;
            if(_agent.Setting.Mode == DrivingMode.Performance)
            {
                _timeElapsedSinceLastTarget = 0;
                _lastLerpTime = 0;
            }
        }

        private void Q_SetBrakeInput(float brakeInput)
        {
            _agent.Context.CurrentBrakeInput = Mathf.MoveTowards(_agent.Context.CurrentBrakeInput, brakeInput, Time.deltaTime * 0.5f);
            _vehicleController.brakeInput = _agent.Context.CurrentBrakeInput;
        }

        private void Q_SetThrottleInput(float throttleInput)
        {
            _agent.Context.CurrentThrottleInput = Mathf.MoveTowards(_agent.Context.CurrentThrottleInput, throttleInput, Time.deltaTime * 0.5f);
            _vehicleController.throttleInput = _agent.Context.CurrentThrottleInput;
        }

        private void Q_Brake()
        {
            if(_brakeController.ShouldAct(ref _agent))
            {
                // The coefficient that determines the scaling for the undershoot to the brake input
                const float undershootCoef = 0.1f;
                
                // Try to target 30% braking, but if we are going to overshoot the target then brake harder, all the way up to 80% maximum
                Q_SetBrakeInput(Mathf.Clamp(0.3f + undershootCoef * _agent.Context.BrakeUndershoot, 0.3f, 0.8f));
                Q_SetThrottleInput(0f);
            }
            else
            {
                Q_SetBrakeInput(0f);
                Q_SetThrottleInput(0.5f);
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
            bool reachedTarget = false;

            // Move the current node forward while we are closer to the next node than the current. Also check the node after the next as the next may be further away in intersections where the current road is switched
            // Note: only updates while driving. During repositioning the vehicle will be closer to the next node (the repositioning target) halfway through the repositioning
            // This would cause our current position to skip ahead so repositioning is handled separately
            
            while(!reachedEnd && !reachedTarget && _status == Status.Driving)
            {
                bool isCloserToNextThanCurrentNode = Vector3.Distance(transform.position, nextNode.Position) <= Vector3.Distance(transform.position, _agent.Context.CurrentNode.Position);
                bool isCloserToNextNextThanCurrentNode = Vector3.Distance(transform.position, nextNextNode.Position) <= Vector3.Distance(transform.position, _agent.Context.CurrentNode.Position);
                
                // If the next or next next node is further away than our current position, we should not update the current node
                if(!(isCloserToNextThanCurrentNode || isCloserToNextNextThanCurrentNode))
                    break;
                
                TotalDistance += _agent.Context.CurrentNode.DistanceToPrevNode;
                _agent.Context.CurrentNode = nextNode;
                
                // All logic for the navigation controller is handled through the actions, so we ignore the return value
                _navigationController.ShouldAct(ref _agent);
                
                // When the current node is updated, it needs to redraw the navigation path
                if (_agent.Context.CurrentNode.IsSteeringTarget && _agent.Context.NavigationPathPositions.Count > 0)
                    _agent.Context.NavigationPathPositions.RemoveAt(0);

                if (ShowNavigationPath)
                    Navigation.DrawUpdatedNavigationPath(ref _agent.Context.NavigationPathPositions, _agent.Context.NavigationPathContainer);
                
                nextNode = Q_GetNextCurrentNode();
                nextNextNode = GetNextLaneNode(nextNode, 0, false);
                reachedEnd = reachedEnd || (!_agent.Context.IsEnteringNetwork && _agent.Context.CurrentNode.Type == RoadNodeType.End);
                reachedTarget = reachedTarget || HasReachedTarget();
            }

            bool waitWithTeleporting = false;
            if(reachedTarget && _agent.Context.NavigationMode == NavigationMode.Path)
            {
                (POI targetPOI, _, _) = _agent.Context.NavigationPathTargets.Pop();

                if(targetPOI != null)
                {
                    if(targetPOI is Parking)
                    {
                        Parking parking = targetPOI as Parking;
                        Park(parking);
                        waitWithTeleporting = _agent.Context.Activity == VehicleActivity.Parked;

                        // Queue an event to try unparking the vehicle after a random delay
                        TimeManagerEvent unParkEvent = new TimeManagerEvent(DateTime.Now.AddMilliseconds(UnityEngine.Random.Range(10, 60) * 1000));
                        TimeManager.Instance.AddEvent(unParkEvent);
                        unParkEvent.OnEvent += () => StartCoroutine(Unpark());
                    }
                    else if(targetPOI is BusStop)
                    {
                        WaitAtBusStop(targetPOI as BusStop);
                    }
                }
            }

            bool teleported = false;
            if(reachedEnd)
            {
                bool isLoopNodeNotOccupied = !(_agent.Context.EndNextNode.HasVehicle() && _agent.Context.EndNextNode.Vehicle != _agent.Setting.Vehicle);
                
                // If the road ended but we are looping, teleport to the first position
                if(!waitWithTeleporting && EndBehaviour == RoadEndBehaviour.Loop && !_agent.Context.CurrentNode.RoadNode.Road.IsClosed() && isLoopNodeNotOccupied)
                {
                    Q_EndOfRoadTeleport();
                    teleported = true;
                }
            }

            // After the first increment of the current node, we are no longer entering the network
            if(_agent.Context.Activity == VehicleActivity.Driving && !teleported && (_agent.Context.IsEnteringNetwork && _agent.Context.CurrentNode.Type != RoadNodeType.End))
                _agent.Context.IsEnteringNetwork = false;
        }

        private bool HasReachedTarget()
        {
            switch(_agent.Context.NavigationMode)
            {
                case NavigationMode.RandomNavigationPath:
                    return _agent.Context.CurrentNode.RoadNode == _agent.Context.NavigationPathEndNode.RoadNode;
                case NavigationMode.Path:
                    if(_agent.Context.NavigationPathTargets.Count < 1)
                        return false;
                    
                    bool hasTargetPOI = _agent.Context.CurrentNode.POIs.Contains(_agent.Context.NavigationPathTargets.Peek().Item1);
                    bool isOnRightSide = _agent.Context.NavigationPathTargets.Peek().Item3 == _agent.Context.CurrentNode.LaneSide;
                    return hasTargetPOI && isOnRightSide;
                default:
                    return false;
            }
        }

        private void Q_EndOfRoadTeleport()
        {
            _agent.Context.Loop();

            // Since there is an issue with the car spinning after teleporting, we pause the rigidbody for a second
            // This is a temporary fix until the real issue is resolved
            ResetToNode(_agent.Context.CurrentNode);
            SetVehicleMovement(false);
            TimeManagerEvent unPauseEvent = new TimeManagerEvent(DateTime.Now.AddMilliseconds(1000));
            TimeManager.Instance.AddEvent(unPauseEvent);
            unPauseEvent.OnEvent += () => SetVehicleMovement(true);
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
                
                // Ignore non-steering target nodes
                if(!nextNode.IsSteeringTarget)
                    i--;
                
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
                    _targetLineRenderer.SetPositions(new Vector3[]{ _agent.Context.BrakeTarget?.Position ?? transform.position, transform.position });
                    break;
                case ShowTargetLines.CurrentPosition:
                    _targetLineRenderer.positionCount = 2;
                    _targetLineRenderer.SetPositions(new Vector3[]{ _agent.Context.CurrentNode?.Position ?? transform.position, transform.position });
                    break;
                case ShowTargetLines.OccupiedNodes:
                    _targetLineRenderer.positionCount = _occupiedNodes.Count;
                    _targetLineRenderer.SetPositions(_occupiedNodes.Select(x => x.Position).ToArray());
                    break;
                case ShowTargetLines.All:
                    _targetLineRenderer.positionCount = 3 + _occupiedNodes.Count;
                    _targetLineRenderer.SetPositions(new Vector3[]{ _agent.Context.BrakeTarget?.Position ?? transform.position, transform.position, _agent.Context.CurrentNode?.Position ?? transform.position, transform.position}.Concat(_occupiedNodes.Select(x => x.Position)).ToArray());
                    break;
            }  
        }

        private void SetInitialPrevIntersection()
        {
            _agent.Context.PrevIntersection = null;
            _agent.Context.IsInsideIntersection = false;
            Intersection prevIntersection = null;
            
            if(_target == null)
                return;
            
            // If the starting node is an intersection, the previous intersection is set 
            if (_target.Intersection != null)
                prevIntersection = _target.Intersection;
                
            LaneNode nextNode = _agent.Next(_target);
            
            // If the starting node is at a three way intersection, the target will be an EndNode but the next will be an intersection node, so we need to set the previous intersection
            if (nextNode != null && nextNode.Intersection != null && nextNode.IsIntersection())
                prevIntersection = nextNode.Intersection;

            LaneNode prevNode = _agent.Prev(_target);
            
            // If the starting node is a junction edge, the previous intersection is set
            if (prevNode != null && prevNode.Intersection != null && prevNode.IsIntersection())
                prevIntersection = prevNode.Intersection;

            _agent.Context.PrevIntersection = prevIntersection;
            _agent.Context.IsInsideIntersection = prevIntersection != null;
        }

        // Move the vehicle to the target node
        private void P_MoveTowardsTargetNode()
        {
            float lerpTime = P_GetLerpTime();
            Vector3 targetPosition = P_GetLerpPosition(lerpTime);
            Quaternion targetRotation = P_GetLerpQuaternion(lerpTime);

            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        private void P_UpdateTargetAndCurrent()
        {
            bool shouldBrake = _brakeController.ShouldAct(ref _agent);
            _lerpSpeed = P_GetLerpSpeed(shouldBrake ? 0f : Speed);
            
            _agent.Context.CurrentBrakeInput = shouldBrake ? 1f : 0;
            _agent.Context.CurrentThrottleInput = 1f - _agent.Context.CurrentBrakeInput;
            
            
            // Update the target if we have reached the current target, and we do not need to brake
            if (P_HasReachedTarget(_target))
            {
                TotalDistance += _target.DistanceToPrevNode;
                _agent.Context.CurrentNode = _target;

                if(!_agent.Context.IsEnteringNetwork && _target.Type == RoadNodeType.End && !_target.RoadNode.Road.IsClosed())
                {
                    _agent.Context.Loop();
                    ResetToNode(_agent.Context.CurrentNode);
                }
                    

                // All logic for the navigation controller is handled through the actions, so we ignore the return value
                _navigationController.ShouldAct(ref _agent);

                // When the currentNode is changed, the navigation path needs to be updated
                if (_agent.Context.NavigationPathPositions.Count > 0)
                    _agent.Context.NavigationPathPositions.RemoveAt(0);

                if (ShowNavigationPath)
                    Navigation.DrawUpdatedNavigationPath(ref _agent.Context.NavigationPathPositions, _agent.Context.NavigationPathContainer);
            
                SetTarget(GetNextLaneNode(_target, 0, EndBehaviour == RoadEndBehaviour.Loop));
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
            }
        }

        private float P_GetLerpSpeed(float target)
        {
            // Move the speed by the acceleration
            float maxDiff = Acceleration * Time.deltaTime;
            return Mathf.MoveTowards(_lerpSpeed, target, maxDiff);
        }
        private float P_GetLerpTime()
        {
            float speed = _lerpSpeed;
            float s = Vector3.Distance(_prevTarget.Position, _target.Position);
            float t = s / speed;
            float newLerpTime = _lastLerpTime;
            
            // If the car has slowed down enough that this will be the last target node, then do not move all the way to it to avoid updating the current node
            // Otherwise, only update the lerp time if the speed is large enough to remove oscillations
            if(speed < 3f && _agent.Context.IsBrakingOrStopped)
                newLerpTime = Mathf.MoveTowards(_lastLerpTime, 0.9f, Time.deltaTime * 0.2f);
            else if(speed >= 3f)
                newLerpTime = _timeElapsedSinceLastTarget / t;

            // Only update the lerp time if it has increased to avoid the car reversing due to a shift in the time due to speed decrease (denominator less than one)
            _lastLerpTime = newLerpTime > _lastLerpTime ? newLerpTime : _lastLerpTime;
            return _lastLerpTime;
        }

        private DrivingAction GetCurrentDrivingAction()
        {
            if (GetCurrentSpeed() < 0.01f)
                return DrivingAction.Stopped;
            
            if (_agent.Context.CurrentBrakeInput > 0)
                return DrivingAction.Braking;
            
            if (_agent.Context.CurrentThrottleInput > 0)
                return DrivingAction.Accelerating;
            
            return DrivingAction.Driving;
        }

        private Vector3 P_GetLerpPosition(float t)
        {
            return P_Lift(Vector3.Lerp(_prevTarget.Position, _target.Position, t));
        }
        private Quaternion P_GetLerpQuaternion(float t)
        {
            return Quaternion.Lerp(_prevTarget.Rotation, _target.Rotation, t);
        }
    }
}