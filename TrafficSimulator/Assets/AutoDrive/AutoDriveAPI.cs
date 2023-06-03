using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;
using DataModel;
using EVP;
using RoadGenerator;
using POIs;

namespace VehicleBrain
{
    public enum DrivingState
    {
        Accelerating,
        Driving,
        Braking,
        Stopped
    }
    public class AutoDriveAgent
    {
        private AutoDriveSetting _setting;
        private AutoDriveContext _context;
        private List<POI> _allParkings = new List<POI>();
        private List<POI> _allBusStops = new List<POI>();

        public ref AutoDriveSetting Setting => ref _setting;
        public ref AutoDriveContext Context => ref _context;

        private Dictionary<string, (LaneNode, LaneNode, TurnDirection)> _intersectionNodeTransitions = new Dictionary<string, (LaneNode, LaneNode, TurnDirection)>();

        private class ForcePath
        {
            public Stack<NavigationNodeEdge> ForcedNavigationPath;
            public List<(POI, NavigationNode, LaneSide)> ForcedTargets;
            public ForcePath(Stack<NavigationNodeEdge> forcedNavigationPath, List<(POI, NavigationNode, LaneSide)> forcedTargets)
            {
                ForcedNavigationPath = forcedNavigationPath;
                ForcedTargets = forcedTargets;
            }
        }

        public AutoDriveAgent(AutoDriveSetting setting, AutoDriveContext context)
        {
            _setting = setting;
            _context = context;

            SetAllParkings();
            SetAllBusStops();
        }

        public void SetIntersectionTransition(Intersection intersection, LaneNode entry, LaneNode guideStart, TurnDirection turnDirection)
        {
            _intersectionNodeTransitions[GetIntersectionTransitionKey(intersection, entry)] = (entry, guideStart, turnDirection);
        }

        public void UnsetIntersectionTransition(Intersection intersection, LaneNode entry)
        {
            if(intersection != null)
                _intersectionNodeTransitions.Remove(GetIntersectionTransitionKey(intersection, entry));
        }

        public void ClearIntersectionTransitions()
        {
            _intersectionNodeTransitions.Clear();
        }

        public void UpdateNavigationPath(LaneNode node)
        {
            switch(Context.NavigationMode)
            {
                case NavigationMode.RandomNavigationPath:
                    UpdateRandomPath(node, Context.ShowNavigationPath);
                    break;
                case NavigationMode.Path:
                    GeneratePath(node, Context.ShowNavigationPath);
                    break;
            }
        }

        private LaneNode UpdateAndGetGuideNode(LaneNode node)
        {
            if (Context.NavigationMode == NavigationMode.Disabled)
                return node.Next;

            bool isNonIntersectionNavigationNode = node.RoadNode.IsNavigationNode && !node.IsIntersection();
            bool currentTargetNodeNotChecked = node.RoadNode != Context.PrevTarget?.RoadNode;

            // On navigation nodes that are not intersections, we pop the path
            // Special case for roadConnections, there is always two road connection nodes in a row, we only pop the path on the first one
            if (isNonIntersectionNavigationNode && Context.NavigationPath.Count > 0 && currentTargetNodeNotChecked && node.Prev?.Type != RoadNodeType.RoadConnection)
                Context.NavigationPath.Pop();

            if (Context.NavigationPathEndNode != null && Context.NavigationPathEndNode.RoadNode == node.RoadNode && Context.NavigationPath.Count == 0)
                UpdateNavigationPath(node);

            if (node.Type == RoadNodeType.JunctionEdge && currentTargetNodeNotChecked)
            {
                LaneNode entryNode = node;

                // Only check the intersection if the vehicle has not already just checked it
                bool intersectionNotChecked = node.Intersection?.ID != Context.PrevIntersection?.ID;
                if (intersectionNotChecked)
                {
                    LaneNode loopNode = null;
                    if (Context.NavigationMode == NavigationMode.RandomNavigationPath || Context.NavigationMode == NavigationMode.Path)
                    {
                        (loopNode, node) = node.Intersection.GetNewLaneNode(Context.NavigationPath.Pop(), node, ref Context.TurnDirection);
                        
                        // If the intersection does not have a lane node that matches the navigation path, unexpected behaviour has occurred, switch to random navigation
                        if (node == null)
                            Context.NavigationMode = NavigationMode.Random;
                    }
                    else if (Context.NavigationMode == NavigationMode.Random)
                    {
                        (loopNode, node) = node.Intersection.GetRandomLaneNode(node, ref Context.TurnDirection);
                    }
                    
                    SetIntersectionTransition(entryNode.Intersection, entryNode, node, Context.TurnDirection);
                    Context.SetLoopNode(loopNode);
                }
            }
            
            Context.BrakeTarget = node;
            Context.PrevTarget = node;
            return node.Next;
        }

        public void UpdateRandomPath(LaneNode node, bool showNavigationPath)
        {
            Context.NavigationPathTargets.Clear();
            
            // Get a random path from the navigation graph
            Context.NavigationPath = Navigation.GetRandomPath(Context.CurrentRoad.RoadSystem, node.GetNavigationEdge(), out Context.NavigationPathEndNode);

            // Switch to Random mode if no path could be found
            if (Context.NavigationPath.Count == 0)
                Context.NavigationMode = NavigationMode.Random;

            // If a path was found we are still in RandomNavigationPath mode, so map the navigation path
            if (Context.NavigationMode == NavigationMode.RandomNavigationPath)
            {
                RoadNode targetRoadNode = Context.NavigationPathEndNode.RoadNode;
                if(targetRoadNode.HasPOI())
                {
                    POI randomPOI = targetRoadNode.POIs.GetRandomElement();
                    Context.NavigationPathTargets.Push((randomPOI, targetRoadNode, randomPOI.LaneSide));
                }
                
                MapNavigationPath(node, showNavigationPath);
            }
        }

        private void SetAllBusStops()
        {
            List<POI> busStops = new List<POI>();
            
            foreach(DefaultRoad road in Context.CurrentRoad.RoadSystem.DefaultRoads)
            {
                foreach(POI busStop in road.POIs.FindAll(x => x is BusStop))
                {
                    if(!busStops.Contains(busStop))
                        busStops.Add(busStop);
                }
            }
            
            busStops.Sort((x, y) => x.name.CompareTo(y.name));
            _allBusStops = busStops;
        }

        private void SetAllParkings()
        {
            List<POI> parkings = new List<POI>();
            
            foreach(DefaultRoad road in Context.CurrentRoad.RoadSystem.DefaultRoads)
            {
                foreach(POI parking in road.POIs.FindAll(x => x is Parking))
                {
                    if(!parkings.Contains(parking))
                        parkings.Add(parking);
                }
            }
            
            parkings.Sort((x, y) => x.name.CompareTo(y.name));
            _allParkings = parkings;
        }

        private NavigationNode GetPOINavigationNode(POI poi)
        {
            return Context.CurrentRoad.RoadSystem.RoadSystemGraph.Find(x => x == (poi.LaneSide == LaneSide.Primary ? x.RoadNode.PrimaryNavigationNode : x.RoadNode.SecondaryNavigationNode) && x.RoadNode == poi.RoadNode);
        }

        public void GeneratePath(LaneNode node, bool showNavigationPath)
        {
            List<(POI, NavigationNode, LaneSide)> targets = new List<(POI, NavigationNode, LaneSide)>();
            List<NavigationNode> navigationNodes = Context.CurrentRoad.RoadSystem.RoadSystemGraph;
            ForcePath forcePath = null;

            // Generate a path to visit different POIs depending on the vehicle type
            List<POI> pois = new List<POI>();
            switch(Setting.VehicleType)
            {
                // Cars will go to a random parking
                case VehicleType.Car:
                    List<POI> parkings = new List<POI>(_allParkings);
                    if(parkings.Count > 0)
                    {
                        parkings.Shuffle();

                        while(parkings.Count > 0)
                        {
                            POI parking = parkings[0];
                            parkings.RemoveAt(0);
                            NavigationNode pathEnd = null;
                            Stack<NavigationNodeEdge> forcedNavigationPath = Navigation.GetPath(node.GetNavigationEdge(), new List<NavigationNode>{ GetPOINavigationNode(parking) }, false, out pathEnd);
                            
                            // Break if a path was found
                            if(pathEnd != null && forcedNavigationPath.Count > 0)
                            {
                                pois.Add(parking);
                                NavigationNode poiNavigationNode = GetPOINavigationNode(parking);
                
                                forcePath = new ForcePath(forcedNavigationPath, new List<(POI, NavigationNode, LaneSide)>{ (parking, poiNavigationNode, parking.LaneSide) });
                                Context.NavigationPathEndNode = pathEnd;
                                break;
                            }
                        }
                    }
                        
                    break;
                // Buses will follow their bus route
                case VehicleType.Bus:
                    pois = (Setting.Vehicle as Bus).BusRoute.ConvertAll(x => (POI)x);
                    break;
                case VehicleType.Tram:
                    break;
            }
            
            if(forcePath == null)
            {
                foreach(POI poi in pois)
                {
                    NavigationNode poiNavigationNode = GetPOINavigationNode(poi);
                    
                    (POI, NavigationNode, LaneSide) target = (poi, poiNavigationNode, poi.LaneSide);
                    if(poiNavigationNode != null && !targets.Contains(target))
                        targets.Add(target);
                }
            }
            else
            {
                targets = forcePath.ForcedTargets;
            }
            
            List<NavigationNode> targetList = targets.ConvertAll(x => x.Item2);
            
            // The target list needs to be reversed as it is converted to a stack
            targets.Reverse();
            
            // Save the targets
            Context.NavigationPathTargets = new Stack<(POI, RoadNode, LaneSide)>(targets.ConvertAll(x => (x.Item1, x.Item2.RoadNode, x.Item3)));
            
            // Get a random path from the navigation graph
            Context.NavigationPath = forcePath != null ? forcePath.ForcedNavigationPath : Navigation.GetPath(node.GetNavigationEdge(), targetList, Context.LogNavigationErrors, out Context.NavigationPathEndNode);

            // Switch to Random mode if no path could be found
            if (Context.NavigationPath.Count == 0)
                Context.NavigationMode = NavigationMode.Random;

            // If a path was found we are still in Path mode, so map the navigation path
            if (Context.NavigationMode == NavigationMode.Path)
                MapNavigationPath(node, showNavigationPath);
        }

        private string GetIntersectionTransitionKey(Intersection intersection, LaneNode entry)
        {
            return $"{intersection.ID}_{entry.ID}";
        }

        private void MapNavigationPath(LaneNode node, bool showNavigationPath)
        {
            if (Context.NavigationPathEndNode == null)
            {
                Debug.LogError("Navigation path end node is null");
                return;
            }

            // Save the previous intersection and reset to it after the path has been mapped
            Intersection prevIntersection = _context.PrevIntersection;
            LaneNode current = node;

            // If the start node is navigation node we use the next node as the start node to avoid popping the first node from the navigation path
            if (node.RoadNode.IsNavigationNode)
                current = node.Next;

            _context.NavigationPathPositions.Clear();
            while(current != null)
            {
                // Only add steering targets to the navigation path
                if(current.IsSteeringTarget)
                    _context.NavigationPathPositions.Add(current.Position);
                
                if (current.RoadNode == _context.NavigationPathEndNode.RoadNode && _context.NavigationPath.Count == 0)
                    break;

                if((current.Type == RoadNodeType.JunctionEdge && current.Next?.IsIntersection() == true && current.Intersection != null && !_intersectionNodeTransitions.ContainsKey(GetIntersectionTransitionKey(current.Intersection, current))) || current.RoadNode.IsNavigationNode)
                    current = UpdateAndGetGuideNode(current);
                else
                    current = Next(current, RoadEndBehaviour.Stop);
            }

            Context.PrevIntersection = prevIntersection;

            Context.DisplayNavigationPathLine();

            _context.CurrentActivity = VehicleActivity.DrivingToTarget;
            _context.TurnDirection = TurnDirection.Straight;
        }

        public LaneNode Next(LaneNode node, RoadEndBehaviour? overrideEndBehaviour = null)
        {
            RoadEndBehaviour endBehaviour = overrideEndBehaviour ?? _setting.EndBehaviour;
            if(node.Intersection != null && _intersectionNodeTransitions.ContainsKey(GetIntersectionTransitionKey(node.Intersection, node)))
            {
                (LaneNode entry, LaneNode guideStart, TurnDirection turnDirection) = _intersectionNodeTransitions[GetIntersectionTransitionKey(node.Intersection, node)];
                Context.TurnDirection = turnDirection;
                
                if(node == entry)
                    return guideStart;
            }
            
            if (Context.NavigationMode == NavigationMode.Random && node.Type == RoadNodeType.JunctionEdge && node.Next?.IsIntersection() == true && node.Intersection != null)
                return UpdateAndGetGuideNode(node);

            if(node.Next == null)
                return endBehaviour == RoadEndBehaviour.Loop ? _context.EndNextNode : null;
            else
                return node.Next;
        }

        public LaneNode Prev(LaneNode node, RoadEndBehaviour? overrideEndBehaviour = null)
        {
            RoadEndBehaviour endBehaviour = overrideEndBehaviour ?? _setting.EndBehaviour;
            if(node.Intersection != null && _intersectionNodeTransitions.ContainsKey(GetIntersectionTransitionKey(node.Intersection, node)))
            {
                (LaneNode entry, LaneNode guideStart, _) = _intersectionNodeTransitions[GetIntersectionTransitionKey(node.Intersection, node)];
                
                if(node == guideStart)
                    return entry;
            }

            if(node.Prev == null)
                return endBehaviour == RoadEndBehaviour.Loop ? _context.EndPrevNode : null;
            else
                return node.Prev;
        }
    }
    
    public struct AutoDriveSetting
    {
        private Vehicle _vehicle;
        private VehicleType _vehicleType;
        private DrivingMode _mode;
        private RoadEndBehaviour _endBehaviour;
        private NavigationMode _originalNavigationMode;
        private VehicleController _vehicleController;
        private float _brakeOffset;
        private float _speed;
        private float _acceleration;
        
        public Vehicle Vehicle => _vehicle;
        public VehicleType VehicleType => _vehicleType;
        public DrivingMode Mode => _mode;
        public RoadEndBehaviour EndBehaviour => _endBehaviour;
        public NavigationMode OriginalNavigationMode => _originalNavigationMode;
        public VehicleController VehicleController => _vehicleController;
        public float BrakeOffset => _brakeOffset;
        public float Speed => _speed;
        public float Acceleration => _acceleration;
        
        public AutoDriveSetting(Vehicle vehicle, VehicleType vehicleType, DrivingMode mode, RoadEndBehaviour endBehaviour, NavigationMode originalNavigationMode, VehicleController vehicleController, float brakeOffset, float speed, float acceleration)
        {
            _vehicle = vehicle;
            _vehicleType = vehicleType;
            _mode = mode;
            _endBehaviour = endBehaviour;
            _originalNavigationMode = originalNavigationMode;
            _vehicleController = vehicleController;
            _brakeOffset = brakeOffset;
            _speed = speed;
            _acceleration = acceleration;

            // Register all ragdolls to the user select manager
            if(_mode == DrivingMode.Performance)
                User.UserSelectManager.Instance.AddRagdollVehicle(_vehicle);
        }
    }

    public class AutoDriveContext
    {
        public Action OnActivityChanged;
        public Action OnRoadChanged;
        private LaneNode _currentNode;
        public LaneNode EndPrevNode;
        public LaneNode EndNextNode;
        public Vector3 VehiclePosition;
        public Vector3 VehicleDirection;
        public bool IsEnteringNetwork;
        public Intersection PrevIntersection;
        public LaneNode PrevTarget;
        public NavigationMode NavigationMode;
        public LaneNode BrakeTarget;
        public float CurrentBrakeInput;
        public float CurrentThrottleInput;
        public NavigationNode NavigationPathEndNode;
        public Stack<NavigationNodeEdge> NavigationPath;
        public Stack<(POI, RoadNode, LaneSide)> NavigationPathTargets;
        public GameObject NavigationPathContainer;
        public GameObject NavigationTargetMarker;
        public Material NavigationPathMaterial;
        public List<Vector3> NavigationPathPositions;
        public TurnDirection TurnDirection;
        public Parking CurrentParking;
        public POI CurrentPOI;
        public Dictionary<Intersection, LaneNode> PrevEntryNodes;
        public bool IsInsideIntersection;
        public bool LogNavigationErrors;
        public bool LogBrakeReason;
        public float BrakeUndershoot;
        public bool IsBrakingOrStopped => CurrentDrivingState == DrivingState.Braking || CurrentDrivingState == DrivingState.Stopped;
        public Road CurrentRoad => CurrentNode?.RoadNode.Road;

        private VehicleActivity _currentActivity;
        private VehicleAction _currentAction;
        private bool _showNavigationPath;
        private LineRenderer _navigationPathLineRenderer;
        private DrivingState _currentDrivingState;
        private float _brakeTime = 0;
        private float _startDrivingDelay = 0;

        public DrivingState CurrentDrivingState
        {
            get => _currentDrivingState;
            set
            {
                if(value == DrivingState.Stopped)
                {
                    bool wasStopped = _currentDrivingState != value;
                    
                    if(wasStopped)
                    {
                        ResetBrakeTime();
                        _startDrivingDelay = UnityEngine.Random.Range(0, 10) / 10f * 3f;
                    }
                    
                    _currentDrivingState = value;
                }
            }
        }

        public void ResetBrakeTime()
        {
            _brakeTime = Time.time;
        }

        public bool CanStartDrivingAgain => Time.time - _brakeTime > _startDrivingDelay;
        

        public LaneNode CurrentNode
        {
            get => _currentNode;
            set
            {
                bool roadChanged = _currentNode?.RoadNode.Road != value?.RoadNode.Road;
                _currentNode = value;
                
                if(roadChanged)
                    OnRoadChanged?.Invoke();
            }
        }

        public bool ShowNavigationPath
        {
            get => _showNavigationPath;
            set
            {
                _showNavigationPath = value;
                DisplayNavigationPathLine();
            }
        }

        public VehicleActivity CurrentActivity
        {
            get => _currentActivity;
            set
            {
                _currentActivity = value;
                OnActivityChanged?.Invoke();
            }
        }

        public VehicleAction CurrentAction
        {
            get => _currentAction;
            set
            {
                _currentAction = value;
                CurrentActivity = GetVehicleActivity(_currentAction, CurrentActivity);
            }
        }
        
        public AutoDriveContext(LaneNode initialNode, Vector3 vehiclePosition, Vector3 vehicleDirection, NavigationMode navigationMode, bool showNavigationPath, bool logNavigationErrors, bool logBrakeReason, GameObject navigationTargetMarker, Material navigationPathMaterial)
        {
            _currentNode = initialNode;
            VehiclePosition = vehiclePosition;
            VehicleDirection = vehicleDirection;

            NavigationTargetMarker = navigationTargetMarker;
            NavigationPathMaterial = navigationPathMaterial;

            IsEnteringNetwork = true;
            PrevIntersection = null;
            PrevTarget = null;
            NavigationMode = navigationMode;
            BrakeTarget = null;
            CurrentBrakeInput = 0;
            CurrentThrottleInput = 0;
            CurrentDrivingState = DrivingState.Stopped;
            CurrentActivity = VehicleActivity.DrivingRandomly;
            CurrentAction = VehicleAction.Driving;
            NavigationPathEndNode = null;
            NavigationPath = new Stack<NavigationNodeEdge>();
            NavigationPathTargets = new Stack<(POI, RoadNode, LaneSide)>();
            
            NavigationPathContainer = new GameObject("Navigation Path");
            _navigationPathLineRenderer = NavigationPathContainer.AddComponent<LineRenderer>();
            _navigationPathLineRenderer.startWidth = 1f;
            _navigationPathLineRenderer.endWidth = 1f;
            _navigationPathLineRenderer.material = NavigationPathMaterial;
            _navigationPathLineRenderer.positionCount = 0;
            
            NavigationPathPositions = new List<Vector3>();
            TurnDirection = TurnDirection.Straight;
            BrakeUndershoot = 0;
            CurrentParking = null;
            CurrentPOI = null;
            PrevEntryNodes = new Dictionary<Intersection, LaneNode>();
            IsInsideIntersection = false;
            LogNavigationErrors = logNavigationErrors;
            LogBrakeReason = logBrakeReason;

            SetLoopNode(initialNode);

            ShowNavigationPath = showNavigationPath;
        }

        private VehicleActivity GetVehicleActivity(VehicleAction activity, VehicleActivity currentAction)
        {
            switch(_currentAction)
            {
                case VehicleAction.Driving when NavigationMode == NavigationMode.Disabled:
                case VehicleAction.Driving when NavigationMode == NavigationMode.Random:
                    return VehicleActivity.DrivingRandomly;
                case VehicleAction.Driving when NavigationMode == NavigationMode.RandomNavigationPath:
                case VehicleAction.Driving when NavigationMode == NavigationMode.Path:
                    return VehicleActivity.DrivingToTarget;
                case VehicleAction.Parked:
                    return VehicleActivity.Parked;
                case VehicleAction.Waiting:
                    return currentAction;
                default:
                    return currentAction;
            }
        }

        public void Loop()
        {
            CurrentNode = EndNextNode;
            
            // Calculate the new loop node
            SetLoopNode(CurrentNode);
        }

        private void SetLoopNodeAtRandomRoad()
        {
            List<DefaultRoad> twoWayRoads = CurrentRoad.RoadSystem.DefaultRoads.FindAll(r => !r.IsOneWay && (r.StartRoadNode.Next?.IsIntersection() == false || r.EndRoadNode.Prev?.IsIntersection() == false));
            Road randomRoad = twoWayRoads.GetRandomElement();

            // if there is no intersection at the start of the road, spawn at the start
            if (randomRoad.StartRoadNode.Next?.IsIntersection() == false)
                EndNextNode = randomRoad.Lanes.Find(l => l.Type.Side == LaneSide.Primary)?.StartNode;
            else if (randomRoad.EndRoadNode.Prev?.IsIntersection() == false)
                EndNextNode = randomRoad.Lanes.Find(l => l.Type.Side == LaneSide.Secondary)?.StartNode;
            else
                Debug.LogError("Could not find a valid road to spawn at");
        }

        public void SetLoopNode(LaneNode node)
        {
            if(node == null)
            {
                EndPrevNode = null;
                EndNextNode = null;
                return;
            }

            EndPrevNode = node.Last;

            if (node.RoadNode.Road.IsOneWay)
                SetLoopNodeAtRandomRoad();
            else
                EndNextNode = node.RoadNode.Road.Lanes.Find(l => l.Type.Index == node.LaneIndex && l.Type.Side != node.LaneSide).StartNode;
        }

        public void UpdateNavigationPathLine()
        {
            if(NavigationPathPositions.Count < 1)
                return;
            
            while(NavigationPathPositions.Count > 0)
            {
                Vector3 nextPosition = NavigationPathPositions[0];
                Vector3 direction = nextPosition - VehiclePosition;
                float dot = Vector3.Dot(VehicleDirection, direction.normalized);

                // Continue removing as long as they are not in front, or too far away
                if(dot > 0 || direction.magnitude > 5f)
                    break;
                
                NavigationPathPositions.RemoveAt(0);
            }
        }

        public void DisplayNavigationPathLine()
        {
            if(ShowNavigationPath)
            {
                if(NavigationPathPositions.Count < 1)
                    return;
                
                _navigationPathLineRenderer.positionCount = NavigationPathPositions.Count;
                _navigationPathLineRenderer.SetPositions(NavigationPathPositions.ToArray());

                Vector3 position = NavigationPathEndNode.RoadNode.Position + Vector3.up * 10f;
                GameObject marker = UnityEngine.Object.Instantiate(NavigationTargetMarker, position, Quaternion.identity);
                marker.transform.parent = NavigationPathContainer.transform;
            }
            else
            {
                _navigationPathLineRenderer.positionCount = 0;
                _navigationPathLineRenderer.SetPositions(new Vector3[0]);
                
                foreach (Transform child in NavigationPathContainer.transform)
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}