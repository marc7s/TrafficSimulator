using UnityEngine;
using System.Collections.Generic;
using DataModel;
using EVP;
using RoadGenerator;

namespace Car
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

        public ref AutoDriveSetting Setting => ref _setting;
        public ref AutoDriveContext Context => ref _context;

        private Dictionary<string, (LaneNode, LaneNode)> _intersectionNodeTransitions = new Dictionary<string, (LaneNode, LaneNode)>();

        public AutoDriveAgent(AutoDriveSetting setting, AutoDriveContext context)
        {
            _setting = setting;
            _context = context;
        }

        public void SetIntersectionTransition(Intersection intersection, LaneNode entry, LaneNode guideStart)
        {
            _intersectionNodeTransitions[intersection.ID] = (entry, guideStart);
        }

        public void UnsetIntersectionTransition(Intersection intersection)
        {
            if(intersection != null)
                _intersectionNodeTransitions.Remove(intersection.ID);
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
                UpdateRandomPath(node, showNavigationPath);

            if (node.Type == RoadNodeType.JunctionEdge && currentTargetNodeNotChecked)
            {
                LaneNode entryNode = node;

                // Only check the intersection if the vehicle has not already just checked it
                bool intersectionNotChecked = node.Intersection?.ID != Context.PrevIntersection?.ID;
                if (intersectionNotChecked)
                {
                    if (Context.NavigationMode == NavigationMode.RandomNavigationPath)
                    {
                        (Context.StartNode, Context.EndNode, node) = node.Intersection.GetNewLaneNode(Context.NavigationPath.Pop(), node, ref Context.TurnDirection);

                        // In performance mode, one currentNode will not be checked as it changes immediately, so we need to remove the oldest point from the navigation path
                        if (Setting.Mode == DrivingMode.Performance)
                            Context.NavigationPathPositions.RemoveAt(0);
                        
                        // If the intersection does not have a lane node that matches the navigation path, unexpected behaviour has occurred, switch to random navigation
                        if (node == null)
                            Context.NavigationMode = NavigationMode.Random;
                    }
                    else if (Context.NavigationMode == NavigationMode.Random)
                    {
                        (Context.StartNode, Context.EndNode, node) = node.Intersection.GetRandomLaneNode(node, ref Context.TurnDirection);
                    }
                    SetIntersectionTransition(entryNode.Intersection, entryNode, node);
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

            if (Context.NavigationPath.Count == 0)
                Context.NavigationMode = NavigationMode.Random;

            if (Context.NavigationMode == NavigationMode.RandomNavigationPath)
                MapNavigationPath(node, showNavigationPath);
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
            {
                _context.NavigationPathPositions.Add(current.Position);
                current = node.Next;
            }

            _context.NavigationPathPositions.Clear();
            while(current != null)
            {
                _context.NavigationPathPositions.Add(current.Position);
                if (current.RoadNode == Context.NavigationPathEndNode.RoadNode && Context.NavigationPath.Count == 0)
                    break;

                if( (current.Type == RoadNodeType.JunctionEdge && current.Intersection != null && !_intersectionNodeTransitions.ContainsKey(current.Intersection.ID)) || current.RoadNode.IsNavigationNode)
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
            if(node.Intersection != null && _intersectionNodeTransitions.ContainsKey(node.Intersection.ID))
            {
                (LaneNode entry, LaneNode guideStart) = _intersectionNodeTransitions[node.Intersection.ID];
                if(node == entry)
                    return guideStart;   
            }

            if(node.Next == null)
                return endBehaviour == RoadEndBehaviour.Loop ? _context.StartNode : null;
            else
                return node.Next;
        }

        public LaneNode Prev(LaneNode node, RoadEndBehaviour? overrideEndBehaviour = null)
        {
            RoadEndBehaviour endBehaviour = overrideEndBehaviour ?? _setting.EndBehaviour;
            if(node.Intersection != null && _intersectionNodeTransitions.ContainsKey(node.Intersection.ID))
            {
                (LaneNode entry, LaneNode guideStart) = _intersectionNodeTransitions[node.Intersection.ID];
                if(node == guideStart)
                    return entry;
            }

            if(node.Prev == null)
                return endBehaviour == RoadEndBehaviour.Loop ? _context.EndNode : null;
            else
                return node.Prev;
        }
    }
    
    public struct AutoDriveSetting
    {
        private Vehicle _vehicle;
        private DrivingMode _mode;
        private Activity _active;
        private RoadEndBehaviour _endBehaviour;
        private VehicleController _vehicleController;
        private float _brakeOffset;
        private float _speed;
        private float _acceleration;
        private GameObject _navigationTargetMarker;
        private Material _navigationPathMaterial;
        
        public Vehicle Vehicle => _vehicle;
        public DrivingMode Mode => _mode;
        public Activity Active => _active;
        public RoadEndBehaviour EndBehaviour => _endBehaviour;
        public VehicleController VehicleController => _vehicleController;
        public float BrakeOffset => _brakeOffset;
        public float Speed => _speed;
        public float Acceleration => _acceleration;
        public GameObject NavigationTargetMarker => _navigationTargetMarker;
        public Material NavigationPathMaterial => _navigationPathMaterial;
        
        public AutoDriveSetting(Vehicle vehicle, DrivingMode mode, Activity active, RoadEndBehaviour endBehaviour, VehicleController vehicleController, float brakeOffset, float speed, float acceleration, GameObject navigationTargetMarker, Material navigationPathMaterial)
        {
            _vehicle = vehicle;
            _mode = mode;
            _active = active;
            _endBehaviour = endBehaviour;
            _vehicleController = vehicleController;
            _brakeOffset = brakeOffset;
            _speed = speed;
            _acceleration = acceleration;
            _navigationTargetMarker = navigationTargetMarker;
            _navigationPathMaterial = navigationPathMaterial;
        }
    }

    public struct AutoDriveContext
    {
        public Road CurrentRoad;
        public LaneNode CurrentNode;
        public LaneNode StartNode;
        public LaneNode EndNode;
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
        public GameObject NavigationPathContainer;
        public List<Vector3> NavigationPathPositions;
        public List<Vector3> VisitedNavigationNodes;
        public TurnDirection TurnDirection;
        public bool IsBrakingOrStopped => CurrentAction == DrivingAction.Braking || CurrentAction == DrivingAction.Stopped;
        
        public AutoDriveContext(Road currentRoad, LaneNode initialNode, Vector3 vehiclePosition, NavigationMode navigationMode)
        {
            CurrentRoad = currentRoad;
            CurrentNode = initialNode;
            StartNode = initialNode.First;
            EndNode = initialNode.Last;
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
            NavigationPathContainer = new GameObject("Navigation Path");
            NavigationPathPositions = new List<Vector3>();
            VisitedNavigationNodes = new List<Vector3>();
            TurnDirection = TurnDirection.Straight;
        }
    }
}