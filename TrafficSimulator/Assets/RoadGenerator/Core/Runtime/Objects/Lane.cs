using UnityEngine;
using System;

namespace RoadGenerator
{
    /// <summary>The side of the lane on the road. The primary side is the driving side of the road system</summary>
    public enum LaneSide
    {
        PRIMARY,
        SECONDARY
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
<<<<<<< HEAD
=======
            this._path = path;

            // Create the start node for the lane
            _start = new LaneNode(path.GetPoint(0), GetNodeRotation(0, _road.EndOfPathInstruction), roadNode, 0);
            
            // Create a previous and current node that will be used when creating the linked list
            LaneNode prev = null;
            LaneNode curr = _start;
            
            // The road node for this lane node
            RoadNode currRoadNode = roadNode.Next == null ? roadNode : roadNode.Next;

            // Go through each point in the path of the lane
            for(int i = 1; i < path.NumPoints; i++)
            {
                // Update the prev and create a new current node
                prev = curr;
                curr = new LaneNode(path.GetPoint(i), GetNodeRotation(path.cumulativeLengthAtEachVertex[i], _road.EndOfPathInstruction), currRoadNode, prev, null, this, path.DistanceBetweenPoints(i - 1, i));

                // Set the next pointer for the previous node
                prev.Next = curr;

                // Move the current road node forward one step
                if(currRoadNode.Next != null)
                {
                    currRoadNode = currRoadNode.Next;
                }
            }
>>>>>>> Fixed bugs

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
    }
}
