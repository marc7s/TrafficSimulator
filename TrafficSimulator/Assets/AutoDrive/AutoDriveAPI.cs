using UnityEngine;
using System.Collections.Generic;
using DataModel;
using EVP;
using RoadGenerator;

namespace Car
{
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

        public bool HasIntersectionNavigation(Intersection intersection, LaneNode entry)
        {
            return intersection != null && _intersectionNodeTransitions.ContainsKey(intersection.ID) && _intersectionNodeTransitions[intersection.ID].Item1 == entry;
        }

        private LaneNode UpdateAndGetGuideNode(LaneNode node, bool showNavigationPath)
        {
            if (Context.NavigationMode == NavigationMode.Disabled)
                return node.Next;
            
            bool isNonIntersectionNavigationNode = node.RoadNode.IsNavigationNode && !node.IsIntersection();
            bool currentTargetNodeNotChecked = node.RoadNode != Context.PrevTarget?.RoadNode;
            if (isNonIntersectionNavigationNode && Context.NavigationPath.Count != 0 && currentTargetNodeNotChecked)
            {
                Context.NavigationPath.Pop();
                Context.PrevIntersection = null;
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
                        (Context.StartNode, Context.EndNode, node) = node.Intersection.GetNewLaneNode(Context.NavigationPath.Pop(), node);
                        
                        // In performance mode, one currentNode will not be checked as it changes immediately, so we need to remove the oldest point from the navigation path
                        if (Setting.Mode == DrivingMode.Performance)
                            Navigation.DrawPathRemoveOldestPoint(ref Context.NavigationPathPositions, Context.NavigationPathContainer);
                        
                        // If the intersection does not have a lane node that matches the navigation path, unexpected behaviour has occurred, switch to random navigation
                        if (node == null)
                            Context.NavigationMode = NavigationMode.Random;
                    }
                    else if (Context.NavigationMode == NavigationMode.Random)
                    {
                        (Context.StartNode, Context.EndNode, node) = node.Intersection.GetRandomLaneNode(node);
                    }
                    
                    SetIntersectionTransition(entryNode.Intersection, entryNode, node);

                    Context.BrakeTarget = node;
                    Context.PrevTarget = node;
                }
            }

            return node.Next;
        }

        public void UpdateRandomPath(LaneNode node, bool showNavigationPath)
        {
            // Get a random path from the navigation graph
            Context.NavigationPath = Navigation.GetRandomPath(Context.CurrentRoad.RoadSystem, node.GetNavigationEdge(), out Context.NavigationPathEndNode);
            
            if (showNavigationPath)
                Navigation.DrawNavigationPath(out Context.NavigationPathPositions, Context.NavigationPathEndNode, Context.NavigationPath, Context.CurrentNode, Context.NavigationPathContainer, Setting.NavigationPathMaterial, Context.PrevIntersection, Setting.NavigationTargetMarker);
            
            if (Context.NavigationPath.Count == 0)
                Context.NavigationMode = NavigationMode.Random;
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
 
            if(node.Type == RoadNodeType.JunctionEdge && node.Intersection != null)
                return UpdateAndGetGuideNode(node, true);

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
        private RoadEndBehaviour _endBehaviour;
        private VehicleController _vehicleController;
        private float _brakeOffset;
        private float _speed;
        private GameObject _navigationTargetMarker;
        private Material _navigationPathMaterial;
        
        public Vehicle Vehicle => _vehicle;
        public DrivingMode Mode => _mode;
        public RoadEndBehaviour EndBehaviour => _endBehaviour;
        public VehicleController VehicleController => _vehicleController;
        public float BrakeOffset => _brakeOffset;
        public float Speed => _speed;
        public GameObject NavigationTargetMarker => _navigationTargetMarker;
        public Material NavigationPathMaterial => _navigationPathMaterial;
        
        public AutoDriveSetting(Vehicle vehicle, DrivingMode mode, RoadEndBehaviour endBehaviour, VehicleController vehicleController, float brakeOffset, float speed, GameObject navigationTargetMarker, Material navigationPathMaterial)
        {
            _vehicle = vehicle;
            _mode = mode;
            _endBehaviour = endBehaviour;
            _vehicleController = vehicleController;
            _brakeOffset = brakeOffset;
            _speed = speed;
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
        public NavigationNode NavigationPathEndNode;
        
        public Stack<NavigationNodeEdge> NavigationPath;
        public GameObject NavigationPathContainer;
        public List<Vector3> NavigationPathPositions;
        
        public AutoDriveContext(Road currentRoad, LaneNode current, LaneNode start, LaneNode end, Vector3 vehiclePosition, bool isEnteringNetwork, Intersection prevIntersection, LaneNode prevTarget, NavigationMode navigationMode, LaneNode brakeTarget, NavigationNode navigationPathEndNode, Stack<NavigationNodeEdge> navigationPath, GameObject navigationPathContainer, List<Vector3> navigationPathPositions)
        {
            CurrentRoad = currentRoad;
            CurrentNode = current;
            StartNode = start;
            EndNode = end;
            VehiclePosition = vehiclePosition;

            IsEnteringNetwork = isEnteringNetwork;
            PrevIntersection = prevIntersection;
            PrevTarget = prevTarget;
            NavigationMode = navigationMode;
            BrakeTarget = brakeTarget;
            NavigationPathEndNode = navigationPathEndNode;
            NavigationPath = navigationPath;
            NavigationPathContainer = navigationPathContainer;
            NavigationPathPositions = navigationPathPositions;
        }
    }
}