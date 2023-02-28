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
        public bool IsNavigationNode = false;
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
            copy.Intersection = Intersection;
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
        public void AddNavigationEdgeToRoadNodes(NavigationNode startNavigationNode, NavigationNode endNavigationNode, bool isClosed)
        {
            RoadNode curr = this;
            NavigationNode prevNavigationNode = startNavigationNode;
            NavigationNodeEdge navigationEdge = startNavigationNode.Edges[0];
            // Using a sliding window with the start being a navigational node and the end begin the edge pointing in the road direction
            while(curr != null) 
            {
                if (curr.IsIntersection())
                {
                    // We do not want to skip the first node of the road
                    if (curr.Position == startNavigationNode.RoadNode.Position)
                    {
                        curr = curr.Next;
                        continue;
                    }
                    // If the intersection only have one edge, then it is a three way intersection the last node of the road
                    if (navigationEdge.EndNavigationNode.Edges.Count < 2)
                    {
                        return;
                    }
                    if (navigationEdge.EndNavigationNode.Edges[0].EndNavigationNode.RoadNode.Position == prevNavigationNode.RoadNode.Position)
                    {
                        prevNavigationNode = navigationEdge.EndNavigationNode;
                        navigationEdge = navigationEdge.EndNavigationNode.Edges[1];
                    }
                    else
                    {
                        prevNavigationNode = navigationEdge.EndNavigationNode;
                        navigationEdge = navigationEdge.EndNavigationNode.Edges[0];
                    }
                    curr = curr.Next;
                    continue;
                }
                curr.NavigationNodeEdge = navigationEdge;

                curr = curr.Next;
            }

        }

        public void UpdateIntersectionJunctionEdgeNavigation(Road road)
        {
            RoadNode curr = this;
            while(curr != null)
            {
                if (curr.Type == RoadNodeType.JunctionEdge)
                {
                    //Debug.Log(curr.NavigationNodeEdge);
                    foreach (Intersection intersection in road.Intersections)
                    {
                        if (curr.Position == intersection.Road1AnchorPoint1)
                        {
                            if(curr.NavigationNodeEdge.EndNavigationNode.RoadNode.Position != intersection.IntersectionPosition)
                            {
                                intersection.Road1AnchorPoint1NavigationEdge = curr.NavigationNodeEdge;
                            }
                        }
                        if (curr.Position == intersection.Road1AnchorPoint2)
                        {
                            if(curr.NavigationNodeEdge.EndNavigationNode.RoadNode.Position != intersection.IntersectionPosition)
                            {
                                intersection.Road1AnchorPoint2NavigationEdge = curr.NavigationNodeEdge;
                            }
                        }
                        if (curr.Position == intersection.Road2AnchorPoint1)
                        {
                            if(curr.NavigationNodeEdge.EndNavigationNode.RoadNode.Position != intersection.IntersectionPosition)
                            {
                                intersection.Road2AnchorPoint1NavigationEdge = curr.NavigationNodeEdge;
                            }
                        }
                        if (curr.Position == intersection.Road2AnchorPoint2)
                        {
                            if(curr.NavigationNodeEdge.EndNavigationNode.RoadNode.Position != intersection.IntersectionPosition)
                            {
                                intersection.Road2AnchorPoint2NavigationEdge = curr.NavigationNodeEdge;
                            }
                        }
                    }

                }
                curr = curr.Next;
            }
        }
        private static void UpdateAnchorPointNodeEdge(NavigationNode nextNavigationNode, NavigationNode prevNavigationNode, Intersection intersection, out NavigationNodeEdge anchorPoint1NavigationEdgeToUpdate)
        {
            bool isNextNodeIntersection = nextNavigationNode.RoadNode.Position == intersection.IntersectionPosition;
            NavigationNode edgeNode = isNextNodeIntersection ? prevNavigationNode : nextNavigationNode;
            NavigationNode intersectionNode = isNextNodeIntersection ? nextNavigationNode : prevNavigationNode;

            if (intersectionNode.Edges[0].EndNavigationNode == edgeNode)
            {
                anchorPoint1NavigationEdgeToUpdate = intersectionNode.Edges[0];
            }
            else
            {
                anchorPoint1NavigationEdgeToUpdate = intersectionNode.Edges[1];
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
        /// <summary>Reverses the roadNode linked list</summary>
        public override RoadNode Reverse()
        {
            RoadNode reversedNode = base.Reverse();
            reversedNode.ReverseEdges();
            return reversedNode;
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