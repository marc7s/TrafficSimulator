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
        
        public RoadNode(Vector3 position, RoadNodeType type, float distanceToNextNode) : this(position, type, null, null, distanceToNextNode){}
        public RoadNode(Vector3 position, RoadNodeType type, RoadNode prev, RoadNode next, float distanceToNextNode)
        {
            _position = position;
            _type = type;
            _prev = prev;
            _next = next;
            _distanceToNextNode = distanceToNextNode;
        }

        /// <summary>Returns `true` if this node is an intersection</summary>
        public bool IsIntersection() => _intersectionTypes.Contains(_type);

        public RoadNodeType Type
        {
            get => _type;
        }
    }
}