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
        private VertexPath _path;
        private LaneNode _start;
        private Road _road;
        private LaneType _type;

        /// <summary>Creates a lane along the supplied path</summary>
        /// <param name="road">The road that the lane is on</param>
        /// <param name="roadNode">The road node that the lane starts at</param>
        /// <param name="type">The type of the lane</param>
        /// <param name="path">The path that the lane should follow</param>
        public Lane(Road road, RoadNode roadNode, LaneType type, VertexPath path)
        {
            this._road = road;
            this._type = type;
            this._path = path;

            // Create the start node for the lane
            _start = new LaneNode(path.GetPoint(0), path.GetRotation(0, _road.EndOfPathInstruction), roadNode);
            
            // Create a previous and current node that will be used when creating the linked list
            LaneNode prev = null;
            LaneNode curr = _start;
            
            // The road node for this lane node
            RoadNode currRoadNode = roadNode;

            // Go through each point in the path of the lane
            for(int i = 1; i < path.NumPoints; i++)
            {
                // Update the prev and create a new current node
                prev = curr;
                curr = new LaneNode(path.GetPoint(i), path.GetRotationAtDistance(path.cumulativeLengthAtEachVertex[i], _road.EndOfPathInstruction), currRoadNode, prev, null);

                // Set the next pointer for the previous node
                prev.Next = curr;

                // Move the current road node forward one step
                if(currRoadNode.Next != null)
                {
                    currRoadNode = currRoadNode.Next;
                }
            }
        }

        /// <summary>Get the first lane node of the lane</summary>
        public LaneNode Start
        {
            get => _start;
        }
        
        /// <summary>Get the type of the lane</summary>
        public LaneType Type
        {
            get => _type;
        }
        
        /// <summary>Get the position at a distance from the start of the path</summary>
        public Vector3 GetPositionAtDistance(float distance, EndOfPathInstruction? endOfPathInstruction = null)
        {
            EndOfPathInstruction eopi = endOfPathInstruction == null ? _road.EndOfPathInstruction : (EndOfPathInstruction)endOfPathInstruction;
            return _path.GetPointAtDistance(distance, eopi);
        }
        
        /// <summary>Get the rotation at a distance from the start of the path</summary>
        public Quaternion GetRotationAtDistance(float distance, EndOfPathInstruction? endOfPathInstruction = null)
        {
            EndOfPathInstruction eopi = endOfPathInstruction == null ? _road.EndOfPathInstruction : (EndOfPathInstruction)endOfPathInstruction;
            return _path.GetRotationAtDistance(distance, eopi);
        }
    }
}