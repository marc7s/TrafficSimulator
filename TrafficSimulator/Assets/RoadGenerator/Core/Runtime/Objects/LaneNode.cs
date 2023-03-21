using UnityEngine;
using DataModel;

namespace RoadGenerator 
{
    /// <summary>Represents a single node in a lane</summary>
    public class LaneNode : Node<LaneNode>
    {
        protected RoadNode _roadNode;
        protected LaneSide _laneSide;
        protected Vehicle _vehicle;
        protected int _laneIndex;

        /// <summary>Creates a new isolated lane node without any previous or next nodes</summary>
        public LaneNode(Vector3 position, LaneSide laneSide, int laneIndex, RoadNode roadNode, float distanceToPrevNode) : this(position, laneSide, laneIndex, roadNode, null, null, distanceToPrevNode){}
        
        /// <summary>Creates a new lane node</summary>
        /// <param name="position">The position of the node</param>
        /// <param name="laneSide">The side of the lane this lane node belongs to</param>
        /// <param name="roadNode">The road node this lane node relates to</param>
        /// <param name="prev">The previous lane node. Pass `null` if there is no previous</param>
        /// <param name="next">The next lane node. Pass `null` if there is no next</param>
        /// <param name="distanceToPrevNode">The distance to the previous (current end node)</param>
        public LaneNode(Vector3 position, LaneSide laneSide, int laneIndex, RoadNode roadNode, LaneNode prev, LaneNode next, float distanceToPrevNode)
        {
            _position = position;
            _laneSide = laneSide;
            _laneIndex = laneIndex;
            _roadNode = roadNode;
            _prev = prev;
            _next = next;
            _distanceToPrevNode = distanceToPrevNode;
            _id = System.Guid.NewGuid().ToString();

            _rotation = laneSide == LaneSide.Primary ? roadNode.Rotation : roadNode.Rotation * Quaternion.Euler(0, 180f, 0);
        }

        /// <summary> Calculates the distance from one node to another. 
        /// Returns true if the node is found, the distance is passed to the out parameter and is 0 if the node is not found 
        /// The sign of the distance is along the node path, so a positive distance means the target is ahead of the current node.
        /// With `roadEndBehaviour` set to Loop, the distance will always be positive if found since all nodes will be checked in the forward direction with looping activated </summary>
        public bool DistanceToNode(LaneNode targetNode, out float distance, RoadEndBehaviour roadEndBehaviour = RoadEndBehaviour.Stop, bool onlyLookAhead = false)
        {
            // Return if the target node is the current node
            if(targetNode == this)
            {
                distance = 0;
                return true;
            }

            float dst = 0;
            LaneNode curr = this.Next;

            // Look forwards
            while (curr != null && curr != this)
            {
                dst += curr.DistanceToPrevNode;
                
                if(curr == targetNode)
                {
                    distance = dst;
                    return true;
                }

                curr = curr.Next;

                if(curr == null && roadEndBehaviour == RoadEndBehaviour.Loop)
                    curr = First;
            }

            // Reset the current node and distance before looking for the target node backwards
            curr = this;
            dst = 0;
            
            // Look backwards if we have not already checked all nodes with the loop behaviour
            while (roadEndBehaviour != RoadEndBehaviour.Loop && !onlyLookAhead && curr != null)
            {
                // We need to change the pointer first, otherwise we will count the distance to the node after the target
                curr = curr.Prev;
                
                dst -= curr.DistanceToPrevNode;
                if(curr == targetNode)
                {
                    distance = dst;
                    return true;
                }
            }
            
            // The target was not found, so set the distance to 0 and return false
            distance = 0;
            return false;
        }
        public bool IsIntersection() => _roadNode.IsIntersection();

        public NavigationNodeEdge GetNavigationEdge()
        {
            return _laneSide == LaneSide.Primary ? _roadNode.PrimaryNavigationNodeEdge : _roadNode.SecondaryNavigationNodeEdge;
        }
        public int Index
        {
            get => _laneIndex;
        }

        public LaneSide LaneSide
        {
            get => _laneSide;
        }
        
        public virtual RoadNode RoadNode
        {
            get => _roadNode;
        }
        public RoadNodeType Type
        {
            get => _roadNode.Type;
        }
        public virtual Vehicle Vehicle
        {
            get => _vehicle;
        }
        public override LaneNode Copy()
        {
            return new LaneNode(_position, _laneSide, _laneIndex, _roadNode, _prev, _next, _distanceToPrevNode);
        }
        
        /// <summary>Tries to assign a vehicle to this node. Returns `true` if it succeded, `false` if there is already a vehicle assigned</summary>
        public virtual bool SetVehicle(Vehicle vehicle)
        {
            if(_vehicle == null)
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

        public bool HasVehicle()
        {
            return _vehicle != null;
        }
    }
}