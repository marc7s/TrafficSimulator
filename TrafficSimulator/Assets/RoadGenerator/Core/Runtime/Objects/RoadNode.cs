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
        public Intersection Intersection;
        public NavigationNodeEdge NavigationNodeEdge;
        private Vector3 _tangent;
        private Vector3 _normal;
        private RoadNodeType _type;
        private float _time;

        // A list of all intersection types
        private static RoadNodeType[] _intersectionTypes = new RoadNodeType[]{ RoadNodeType.ThreeWayIntersection, RoadNodeType.FourWayIntersection, RoadNodeType.Roundabout };
        
        public RoadNode(Vector3 position, Vector3 tangent, Vector3 normal, RoadNodeType type, float distanceToPrevNode, float time) : this(position, tangent, normal, type, null, null, distanceToPrevNode, time){}
        public RoadNode(Vector3 position, Vector3 tangent, Vector3 normal, RoadNodeType type, RoadNode prev, RoadNode next, float distanceToPrevNode, float time, Intersection intersection = null)
        {
            _position = position;
            _tangent = tangent;
            _normal = normal;
            _type = type;
            _prev = prev;
            _next = next;
            _distanceToPrevNode = distanceToPrevNode;
            _time = time;
            this.Intersection = intersection;
        }

        public override RoadNode Copy()
        {
            RoadNode copy = new RoadNode(_position, _tangent, _normal, _type, _prev, _next, _distanceToPrevNode, _time);
            copy.NavigationNodeEdge = NavigationNodeEdge;
            return copy;
        }

        public int CountNonIntersections
        {
            get
            {
                int count = 1;
                RoadNode curr = this;
                
                while(curr.Next != null)
                {
                    if(!(curr.IsIntersection() || curr.Type == RoadNodeType.JunctionEdge))
                        count++;
                    curr = curr.Next;
                }
                return count;
            }
        }
        public void ReverseEdges() 
        {
            RoadNode curr = this;
            while (curr != null) 
            {
                if (curr.NavigationNodeEdge == null)
                {
                    curr = curr.Next;
                    continue;
                }
                foreach (NavigationNodeEdge edge in curr.NavigationNodeEdge.EndNavigationNode.Edges)
                {
                    if (edge.EndNavigationNode == curr.NavigationNodeEdge.StartNavigationNode)
                    {

                        curr.NavigationNodeEdge = edge;
                    }
                }
                curr = curr.Next;
                if (curr == null)
                    break;
            }
        }

        /// <summary>Returns `true` if this node is an intersection</summary>
        public bool IsIntersection() => _intersectionTypes.Contains(_type);

        public RoadNodeType Type
        {
            get => _type;
        }

        public float Time
        {
            get => _time;
        }

        public Vector3 Tangent
        {
            get => _tangent;
        }
        public Vector3 Normal
        {
            get => _normal;
        }
    }
}