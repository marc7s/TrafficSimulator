using UnityEngine;
using DataModel;

namespace RoadGenerator 
{
    /// <summary>Represents a single node in a lane</summary>
    public class LaneNode : Node<LaneNode>
    {
        private RoadNode _roadNode;
        private Vehicle _vehicle;
        /// <summary>Creates a new isolated lane node without any previous or next nodes</summary>
        public LaneNode(Vector3 position, Quaternion rotation, RoadNode roadNode, float distanceToPrevNode) : this(position, rotation, roadNode, null, null, distanceToPrevNode){}
        
        /// <summary>Creates a new lane node</summary>
        /// <param name="position">The position of the node</param>
        /// <param name="rotation">The rotation of the node</param>
        /// <param name="roadNode">The road node this lane node relates to</param>
        /// <param name="prev">The previous lane node. Pass `null` if there is no previous</param>
        /// <param name="next">The next lane node. Pass `null` if there is no next</param>
        public LaneNode(Vector3 position, Quaternion rotation, RoadNode roadNode, LaneNode prev, LaneNode next, float distanceToPrevNode)
        {
            _position = position;
            _rotation = rotation;
            _roadNode = roadNode;
            _prev = prev;
            _next = next;
            _distanceToPrevNode = distanceToPrevNode;
        }
        public bool IsIntersection() => _roadNode.IsIntersection();

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
    }
}