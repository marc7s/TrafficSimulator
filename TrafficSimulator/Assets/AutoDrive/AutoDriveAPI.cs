using UnityEngine;
using System.Collections.Generic;
using Extensions;
using DataModel;
using EVP;
using RoadGenerator;
using POIs;

namespace VehicleBrain
{
    public enum DrivingAction
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

        private Dictionary<string, (LaneNode, LaneNode)> _intersectionNodeTransitions = new Dictionary<string, (LaneNode, LaneNode)>();

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

        public void SetIntersectionTransition(Intersection intersection, LaneNode entry, LaneNode guideStart)
        {
            _intersectionNodeTransitions[GetIntersectionTransitionKey(intersection, entry)] = (entry, guideStart);
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

        private LaneNode UpdateAndGetGuideNode(LaneNode node, bool showNavigationPath)
        {
            if (Context.NavigationMode == NavigationMode.Disabled)
                return node.Next;

            bool isNonIntersectionNavigationNode = node.RoadNode.IsNavigationNode && !node.IsIntersection();
            bool currentTargetNodeNotChecked = node.RoadNode != Context.PrevTarget?.RoadNode;

            if (isNonIntersectionNavigationNode && Context.NavigationPath.Count > 0 && currentTargetNodeNotChecked && !Context.VisitedNavigationNodes.Contains(node.RoadNode.Position))
            {
                Context.VisitedNavigationNodes.Add(node.RoadNode.Position);
                Context.NavigationPath.Pop();
            }

            if (Context.NavigationPathEndNode != null && Context.NavigationPathEndNode.RoadNode == node.RoadNode && Context.NavigationPath.Count == 0)
            {
                switch(Context.NavigationMode)
                {
                    case NavigationMode.RandomNavigationPath:
                        UpdateRandomPath(node, showNavigationPath);
                        break;
                    case NavigationMode.Path:
                        GeneratePath(node, showNavigationPath);
                        break;
                }
            }

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

                        // In performance mode, one currentNode will not be checked as it changes immediately, so we need to remove the oldest point from the navigation path
                        if (Setting.Mode == DrivingMode.Performance)
                            Context.NavigationPathPositions.RemoveAt(0);
                        
                        // If the intersection does not have a lane node that matches the navigation path, unexpected behaviour has occurred, switch to random navigation
                        if (node == null)
                            Context.NavigationMode = NavigationMode.Random;
                    }
                    else if (Context.NavigationMode == NavigationMode.Random)
                    {
                        (loopNode, node) = node.Intersection.GetRandomLaneNode(node, ref Context.TurnDirection);
                    }
                    
                    SetIntersectionTransition(entryNode.Intersection, entryNode, node);
                    Context.SetLoopNode(loopNode);
                }
            }
            Context.BrakeTarget = node;
            Context.PrevTarget = node;
            return node.Next;
        }

        public void UpdateRandomPath(LaneNode node, bool showNavigationPath)
        {
            Context.VisitedNavigationNodes.Clear();
            
            // Get a random path from the navigation graph
            Context.NavigationPath = Navigation.GetRandomPath(Context.CurrentRoad.RoadSystem, node.GetNavigationEdge(), out Context.NavigationPathEndNode);

            // Switch to Random mode if no path could be found
            if (Context.NavigationPath.Count == 0)
                Context.NavigationMode = NavigationMode.Random;

            // If a path was found we are still in RandomNavigationPath mode, so map the navigation path
            if (Context.NavigationMode == NavigationMode.RandomNavigationPath)
                MapNavigationPath(node, showNavigationPath);
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
            Context.VisitedNavigationNodes.Clear();

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
                            Stack<NavigationNodeEdge> forcedNavigationPath = Navigation.GetPath(Context.CurrentRoad.RoadSystem, node.GetNavigationEdge(), new List<NavigationNode>{ GetPOINavigationNode(parking) }, false, out pathEnd);
                            
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

            // Since we are converting the list to a stack and we want the targets to be popped in the same order as the sorted list, we need to reverse the list
            targets.Reverse();
            
            List<NavigationNode> targetList = targets.ConvertAll(x => x.Item2);
            
            // Save the targets
            Context.NavigationPathTargets = new Stack<(POI, RoadNode, LaneSide)>(targets.ConvertAll(x => (x.Item1, x.Item2.RoadNode, x.Item3)));
            
            // Get a random path from the navigation graph
            Context.NavigationPath = forcePath != null ? forcePath.ForcedNavigationPath : Navigation.GetPath(Context.CurrentRoad.RoadSystem, node.GetNavigationEdge(), targetList, Context.LogNavigationErrors, out Context.NavigationPathEndNode);

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
                
                if (current.RoadNode == Context.NavigationPathEndNode.RoadNode && Context.NavigationPath.Count == 0)
                    break;

                if((current.Type == RoadNodeType.JunctionEdge && current.Next?.IsIntersection() == true && current.Intersection != null && !_intersectionNodeTransitions.ContainsKey(GetIntersectionTransitionKey(current.Intersection, current))) || current.RoadNode.IsNavigationNode)
                    current = UpdateAndGetGuideNode(current, true);
                else
                    current = Next(current, RoadEndBehaviour.Stop);
            }

            _context.PrevIntersection = prevIntersection;

            if (showNavigationPath)
                Navigation.DrawNewNavigationPath(_context.NavigationPathPositions, Context.NavigationPathEndNode, Context.NavigationPathContainer, Setting.NavigationPathMaterial , Setting.NavigationTargetMarker);
        }

        public LaneNode Next(LaneNode node, RoadEndBehaviour? overrideEndBehaviour = null)
        {
            RoadEndBehaviour endBehaviour = overrideEndBehaviour ?? _setting.EndBehaviour;
            if(node.Intersection != null && _intersectionNodeTransitions.ContainsKey(GetIntersectionTransitionKey(node.Intersection, node)))
            {
                (LaneNode entry, LaneNode guideStart) = _intersectionNodeTransitions[GetIntersectionTransitionKey(node.Intersection, node)];
                if(node == entry)
                    return guideStart;
            }
            
            if (Context.NavigationMode == NavigationMode.Random && node.Type == RoadNodeType.JunctionEdge && node.Intersection != null)
                return UpdateAndGetGuideNode(node, true);

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
                (LaneNode entry, LaneNode guideStart) = _intersectionNodeTransitions[GetIntersectionTransitionKey(node.Intersection, node)];
                
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
        private VehicleController _vehicleController;
        private float _brakeOffset;
        private float _speed;
        private float _acceleration;
        private GameObject _navigationTargetMarker;
        private Material _navigationPathMaterial;
        
        public Vehicle Vehicle => _vehicle;
        public VehicleType VehicleType => _vehicleType;
        public DrivingMode Mode => _mode;
        public RoadEndBehaviour EndBehaviour => _endBehaviour;
        public VehicleController VehicleController => _vehicleController;
        public float BrakeOffset => _brakeOffset;
        public float Speed => _speed;
        public float Acceleration => _acceleration;
        public GameObject NavigationTargetMarker => _navigationTargetMarker;
        public Material NavigationPathMaterial => _navigationPathMaterial;
        
        public AutoDriveSetting(Vehicle vehicle, VehicleType vehicleType, DrivingMode mode, RoadEndBehaviour endBehaviour, VehicleController vehicleController, float brakeOffset, float speed, float acceleration, GameObject navigationTargetMarker, Material navigationPathMaterial)
        {
            _vehicle = vehicle;
            _vehicleType = vehicleType;
            _mode = mode;
            _endBehaviour = endBehaviour;
            _vehicleController = vehicleController;
            _brakeOffset = brakeOffset;
            _speed = speed;
            _acceleration = acceleration;
            _navigationTargetMarker = navigationTargetMarker;
            _navigationPathMaterial = navigationPathMaterial;
        }
    }

    public class AutoDriveContext
    {
        public LaneNode CurrentNode;
        public LaneNode EndPrevNode;
        public LaneNode EndNextNode;
        public Vector3 VehiclePosition;
        public bool IsEnteringNetwork;
        public Intersection PrevIntersection;
        public LaneNode PrevTarget;
        public NavigationMode NavigationMode;
        public LaneNode BrakeTarget;
        public float CurrentBrakeInput;
        public float CurrentThrottleInput;
        public DrivingAction CurrentAction;
        public NavigationNode NavigationPathEndNode;
        public Stack<NavigationNodeEdge> NavigationPath;
        public Stack<(POI, RoadNode, LaneSide)> NavigationPathTargets;
        public GameObject NavigationPathContainer;
        public List<Vector3> NavigationPathPositions;
        public List<Vector3> VisitedNavigationNodes;
        public TurnDirection TurnDirection;
        public VehicleActivity Activity;
        public Parking CurrentParking;
        public Dictionary<Intersection, LaneNode> PrevEntryNodes;
        public bool IsInsideIntersection;
        public bool LogNavigationErrors;
        public bool LogBrakeReason;
        public float BrakeUndershoot;
        public bool IsBrakingOrStopped => CurrentAction == DrivingAction.Braking || CurrentAction == DrivingAction.Stopped;
        public Road CurrentRoad => CurrentNode.RoadNode.Road;
        
        public AutoDriveContext(LaneNode initialNode, Vector3 vehiclePosition, NavigationMode navigationMode, bool logNavigationErrors, bool logBrakeReason)
        {
            CurrentNode = initialNode;
            VehiclePosition = vehiclePosition;

            IsEnteringNetwork = true;
            PrevIntersection = null;
            PrevTarget = null;
            NavigationMode = navigationMode;
            BrakeTarget = null;
            CurrentBrakeInput = 0;
            CurrentThrottleInput = 0;
            CurrentAction = DrivingAction.Stopped;
            NavigationPathEndNode = null;
            NavigationPath = new Stack<NavigationNodeEdge>();
            NavigationPathTargets = new Stack<(POI, RoadNode, LaneSide)>();
            NavigationPathContainer = new GameObject("Navigation Path");
            NavigationPathContainer.AddComponent<LineRenderer>();
            NavigationPathPositions = new List<Vector3>();
            VisitedNavigationNodes = new List<Vector3>();
            TurnDirection = TurnDirection.Straight;
            BrakeUndershoot = 0;
            Activity = VehicleActivity.Driving;
            CurrentParking = null;
            PrevEntryNodes = new Dictionary<Intersection, LaneNode>();
            IsInsideIntersection = false;
            LogNavigationErrors = logNavigationErrors;
            LogBrakeReason = logBrakeReason;

            SetLoopNode(initialNode);
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
            Road randomRoad = twoWayRoads[Random.Range(0, twoWayRoads.Count)];

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
    }
}