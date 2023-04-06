using UnityEngine;
using DataModel;

namespace RoadGenerator 
{
    /// <summary> A guide node used in intersections </summary>
    public class GuideNode : LaneNode
    {
        private LaneNode _laneNodeReference;

        /// <summary>Creates a new guide node</summary>
        /// <param name="position">The position of the node</param>
        /// <param name="reference">The LaneNode this guide node is related to</param>
        /// <param name="laneSide">The side of the lane this lane node belongs to</param>
        /// <param name="roadNode">The road node this lane node relates to</param>
        /// <param name="prev">The previous lane node. Pass `null` if there is no previous</param>
        /// <param name="next">The next lane node. Pass `null` if there is no next</param>
        /// <param name="distanceToPrevNode">The distance to the previous node</param>
        public GuideNode(Vector3 position, LaneNode laneNodeReference, LaneSide laneSide, int laneIndex, RoadNode roadNode, LaneNode prev, LaneNode next, float distanceToPrevNode) : 
            base(position, laneSide, laneIndex, roadNode, prev, next, distanceToPrevNode)
            {
                _laneNodeReference = laneNodeReference;
            }

        public override LaneNode Copy()
        {
            return new GuideNode(_position, _laneNodeReference, _laneSide, _laneIndex, _roadNode, _prev, _next, _distanceToPrevNode);
        }

        public override Intersection Intersection
        {
            get => _laneNodeReference.Intersection;
        }

        public override RoadNode RoadNode
        {
            get => _laneNodeReference.RoadNode;
        }

        // Since guide nodes are created on demand, they should not use their own vehicle variable but instead use the one from the lane node it is based on
        public override Vehicle Vehicle
        {
            get => _laneNodeReference.Vehicle;
        }

        // Since guide nodes are created on demand, setting a vehicle needs to be forwarded to the lane node it is based on
        // Otherwise guide nodes for different vehicles on the same position would not handle occupancy correctly
        public override bool SetVehicle(Vehicle vehicle)
        {
            return _laneNodeReference.SetVehicle(vehicle);
        }

        // Since guide nodes are created on demand, unsetting a vehicle needs to be forwarded to the lane node it is based on
        // Otherwise guide nodes for different vehicles on the same position would not handle occupancy correctly
        public override bool UnsetVehicle(Vehicle vehicle)
        {
            return _laneNodeReference.UnsetVehicle(vehicle);
        }

        public override bool HasVehicle()
        {
            return _laneNodeReference.HasVehicle();
        }

        public override bool IsSteeringTarget => _laneNodeReference.IsSteeringTarget;
    }
}