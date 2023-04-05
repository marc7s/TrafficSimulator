using UnityEngine;
using DataModel;
using System.Collections.Generic;

namespace RoadGenerator 
{
    /// <summary>Represents a single node in a lane</summary>
    public class LaneNode : Node<LaneNode>
    {
        protected RoadNode _roadNode;
        protected LaneSide _laneSide;
        protected Vehicle _vehicle;
        protected int _laneIndex;
        protected bool _isSteeringTarget;
        public List<(LaneNode, LaneNode)> YieldNodes = new List<(LaneNode, LaneNode)>();
        public List<LaneNode> YieldBlockingNodes = new List<LaneNode>();

        /// <summary>Creates a new isolated lane node without any previous or next nodes</summary>
        public LaneNode(Vector3 position, LaneSide laneSide, int laneIndex, RoadNode roadNode, float distanceToPrevNode, bool isSteeringTarget = true) : this(position, laneSide, laneIndex, roadNode, null, null, distanceToPrevNode, isSteeringTarget){}
        
        /// <summary>Creates a new lane node</summary>
        /// <param name="position">The position of the node</param>
        /// <param name="laneSide">The side of the lane this lane node belongs to</param>
        /// <param name="roadNode">The road node this lane node relates to</param>
        /// <param name="prev">The previous lane node. Pass `null` if there is no previous</param>
        /// <param name="next">The next lane node. Pass `null` if there is no next</param>
        /// <param name="distanceToPrevNode">The distance to the previous (current end node)</param>
        public LaneNode(Vector3 position, LaneSide laneSide, int laneIndex, RoadNode roadNode, LaneNode prev, LaneNode next, float distanceToPrevNode, bool isSteeringTarget = true)
        {
            _position = position;
            _laneSide = laneSide;
            _laneIndex = laneIndex;
            _roadNode = roadNode;
            _prev = prev;
            _next = next;
            _distanceToPrevNode = distanceToPrevNode;
            _isSteeringTarget = isSteeringTarget;
            _id = System.Guid.NewGuid().ToString();

            _rotation = laneSide == LaneSide.Primary ? roadNode.Rotation : roadNode.Rotation * Quaternion.Euler(0, 180f, 0);
        }

        public bool IsIntersection() => _roadNode.IsIntersection();

        public NavigationNodeEdge GetNavigationEdge()
        {
            return _laneSide == LaneSide.Primary ? _roadNode.PrimaryNavigationNodeEdge : _roadNode.SecondaryNavigationNodeEdge;
        }
        public int Index => _laneIndex;
        public LaneSide LaneSide => _laneSide;
        public virtual RoadNode RoadNode => _roadNode;
        public RoadNodeType Type => _roadNode.Type;
        public TrafficLight TrafficLight => _roadNode.TrafficLight;
        public virtual Intersection Intersection => _roadNode.Intersection;
        public virtual Vehicle Vehicle => _vehicle;
        public virtual bool IsSteeringTarget => _isSteeringTarget;
        public override LaneNode Copy()
        {
            return new LaneNode(_position, _laneSide, _laneIndex, _roadNode, _prev, _next, _distanceToPrevNode);
        }
        
        /// <summary>Tries to assign a vehicle to this node. Returns `true` if it succeded, `false` if there is already a vehicle assigned</summary>
        public virtual bool SetVehicle(Vehicle vehicle)
        {
            if(_vehicle == null || _vehicle == vehicle)
            {
                _vehicle = vehicle;
                return true;
            }
            return false;
        }

        /// <summary>Tries to unset a vehicle from this node. Returns `true` if it succeded, `false` if either no vehicle is assigned, or a different vehicle is assigned</summary>
        public virtual bool UnsetVehicle(Vehicle vehicle)
        {
            if(_vehicle == vehicle)
            {
                _vehicle = null;
                return true;
            }
            Debug.LogError("Trying to unset a different vehicle");
            return false;
        }

        public virtual bool HasVehicle()
        {
            return Vehicle != null;
        }
    }
}