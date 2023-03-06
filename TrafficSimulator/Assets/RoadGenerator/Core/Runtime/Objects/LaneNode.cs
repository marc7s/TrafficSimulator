using UnityEngine;
using DataModel;

namespace RoadGenerator 
{
    /// <summary>Represents a single node in a lane</summary>
    public class LaneNode : Node<LaneNode>
    {
        private RoadNode _roadNode;
        private LaneSide _laneSide;
        private Vehicle _vehicle;
        private string _id;
        /// <summary>Creates a new isolated lane node without any previous or next nodes</summary>
        public LaneNode(Vector3 position, LaneSide laneSide, RoadNode roadNode, float distanceToPrevNode) : this(position, laneSide, roadNode, null, null, distanceToPrevNode){}
        
        /// <summary>Creates a new lane node</summary>
        /// <param name="position">The position of the node</param>
        /// <param name="laneSide">The side of the lane this lane node belongs to</param>
        /// <param name="roadNode">The road node this lane node relates to</param>
        /// <param name="prev">The previous lane node. Pass `null` if there is no previous</param>
        /// <param name="next">The next lane node. Pass `null` if there is no next</param>
        public LaneNode(Vector3 position, LaneSide laneSide, RoadNode roadNode, LaneNode prev, LaneNode next, float distanceToPrevNode)
        {
            _position = position;
            _laneSide = laneSide;
            _roadNode = roadNode;
            _prev = prev;
            _next = next;
            _distanceToPrevNode = distanceToPrevNode;
            _id = System.Guid.NewGuid().ToString();

            _rotation = laneSide == LaneSide.Primary ? roadNode.Rotation : roadNode.Rotation * Quaternion.Euler(0, 180f, 0);
        }

        // Calculates the distance from one node to another
        public float DistanceToNode(LaneNode targetNode)
        {
            float distance = 0;
            LaneNode startNode = this;
            while (startNode.Id != targetNode.Id && startNode.Next != null)
            {
                startNode = startNode.Next;
                distance += startNode.DistanceToPrevNode;
            }
            return distance;
        }
        public bool IsIntersection() => _roadNode.IsIntersection();

        public NavigationNodeEdge GetNavigationEdge()
        {
            return _laneSide == LaneSide.Primary ? _roadNode.PrimaryNavigationNodeEdge : _roadNode.SecondaryNavigationNodeEdge;
        }
        
        public RoadNode RoadNode
        {
            get => _roadNode;
        }
        public RoadNodeType Type
        {
            get => _roadNode.Type;
        }
        public Vehicle Vehicle
        {
            get => _vehicle;
        }
        public string Id
        {
            get => _id;
        }
        public override LaneNode Copy()
        {
            return new LaneNode(_position, _laneSide, _roadNode, _prev, _next, _distanceToPrevNode);
        }
        
        /// <summary>Tries to assign a vehicle to this node. Returns `true` if it succeded, `false` if there is already a vehicle assigned</summary>
        public bool SetVehicle(Vehicle vehicle)
        {
            if(_vehicle == null)
            {
                _vehicle = vehicle;
                return true;
            }
            return false;
        }

        /// <summary>Tries to unset a vehicle from this node. Returns `true` if it succeded, `false` if either no vehicle is assigned, or a different vehicle is assigned</summary>
        public bool UnsetVehicle(Vehicle vehicle)
        {
            if(_vehicle == vehicle)
            {
                _vehicle = null;
                return true;
            }
            Debug.LogError("Trying to unset a different vehicle");
            return false;
        }

        public bool HasVehicle()
        {
            return _vehicle != null;
        }
    }
}