using UnityEngine;
using System;

namespace RoadGenerator
{
    /// <summary>The side of the lane on the road. The primary side is the driving side of the road system</summary>
    public enum LaneSide
    {
        Primary,
        Secondary
    }
    
    /// <summary>Represents the type of lane. Contains information on the side and index of the lane</summary>
    public class LaneType
    {
        public readonly LaneSide Side;
        public readonly int Index;
        public LaneType(LaneSide side, int index) => (this.Side, this.Index) = (side, Math.Abs(index));
    }
	
    public class Lane
	{
        private LaneNode _start;
        private Road _road;
        private LaneType _type;
        private float _length;

        /// <summary>Creates a lane along the supplied path</summary>
        /// <param name="road">The road that the lane is on</param>
        /// <param name="startNode">The start node of the linked list of LaneNodes making up the lane</param>
        /// <param name="type">The type of the lane</param>
        public Lane(Road road, LaneNode startNode, LaneType type)
        {
            this._road = road;
            this._start = startNode;
            this._type = type;

            // Set lane length
            _length = GetLaneLength();
        }

        /// <summary>Get the first lane node of the lane</summary>
        public LaneNode StartNode
        {
            get => _start;
        }

        /// <summary>Get the road that the lane is on</summary>
        public Road Road
        {
            get => _road;
        }
        
        /// <summary>Get the type of the lane</summary>
        public LaneType Type
        {
            get => _type;
        }

        /// <summary>Get the length of the lane</summary>
        public float Length
        {
            get => _length;
        }

        /// <summary>Get the length of the lane</summary>
        private float GetLaneLength()
        {
            float length = 0;
            LaneNode curr = _start;
            while(curr != null)
            {
                length += curr.DistanceToPrevNode;
                curr = curr.Next;
            }
            return length;
        }

        /// <summary>Get the length of the lane without intersections</summary>
        public float GetLaneLengthNoIntersections()
        {
            float length = 0;
            LaneNode curr = _start;
            while(curr != null)
            {
                if(!curr.RoadNode.IsIntersection())
                    length += curr.DistanceToPrevNode;
                while(curr.RoadNode.IsIntersection())
                {
                    curr = curr.Next;
                    if(curr.RoadNode.Type == RoadNodeType.JunctionEdge)
                        break;
                }
                curr = curr.Next;
            }
            return length;
        }
    }
}
