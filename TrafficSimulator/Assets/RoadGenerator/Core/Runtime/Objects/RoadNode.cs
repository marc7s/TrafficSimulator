using UnityEngine;
using System.Linq;

namespace RoadGenerator
{
    public enum RoadNodeType 
    {
        Default = 0,
        ThreeWayIntersection,
        FourWayIntersection,
        Roundabout,
        JunctionEdge,
        End
    }

    /// <summary>Represents a single node in a road</summary>
	public class RoadNode : Node<RoadNode>
	{
        private RoadNodeType _type;
        // A list of all intersection types
        private static RoadNodeType[] _intersectionTypes = new RoadNodeType[]{ RoadNodeType.ThreeWayIntersection, RoadNodeType.FourWayIntersection, RoadNodeType.Roundabout };
        
        public RoadNode(Vector3 position, RoadNodeType type, float distanceToPrevNode) : this(position, type, null, null, distanceToPrevNode){}
        public RoadNode(Vector3 position, RoadNodeType type, RoadNode prev, RoadNode next, float distanceToPrevNode)
        {
            _position = position;
            _type = type;
            _prev = prev;
            _next = next;
            _distanceToPrevNode = distanceToPrevNode;
        }

        /// <summary>Returns `true` if this node is an intersection</summary>
        public bool IsIntersection() => _intersectionTypes.Contains(_type);

        public RoadNodeType Type
        {
            get => _type;
        }
    }
}