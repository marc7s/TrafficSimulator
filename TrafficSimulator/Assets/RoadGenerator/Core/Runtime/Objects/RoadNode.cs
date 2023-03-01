using UnityEngine;
using System.Linq;
using System.Collections.Generic;

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
        public NavigationNodeEdge PrimaryNavigationNodeEdge;
        public NavigationNodeEdge SecondaryNavigationNodeEdge;
        private Vector3 _tangent;
        private Vector3 _normal;
        private RoadNodeType _type;
        public bool IsNavigationNode = false;
        private float _time;
        
        public string ID;

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
            ID = System.Guid.NewGuid().ToString();
            _rotation = Quaternion.LookRotation(_tangent, Vector3.up);
        }

        public override RoadNode Copy()
        {
            RoadNode copy = new RoadNode(_position, _tangent, _normal, _type, _prev, _next, _distanceToPrevNode, _time);
            copy.PrimaryNavigationNodeEdge = PrimaryNavigationNodeEdge;
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
            NavigationNode nextNavigationNode = startNavigationNode.Edges[0].EndNavigationNode;

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
                    if (nextNavigationNode.Edges.Count < 2)
                    {
                        curr.SecondaryNavigationNodeEdge = nextNavigationNode.SecondaryDirectionEdge;
                        curr = curr.Next;
                        continue;
                    }
                    curr.PrimaryNavigationNodeEdge = nextNavigationNode.PrimaryDirectionEdge;
                    curr.SecondaryNavigationNodeEdge = nextNavigationNode.SecondaryDirectionEdge;
                    
                    if (nextNavigationNode.PrimaryDirectionEdge == null)
                    {
                        curr = curr.Next;
                        continue;
                    }
                    prevNavigationNode = nextNavigationNode;    
                    nextNavigationNode = nextNavigationNode.PrimaryDirectionEdge.EndNavigationNode;
            
                    curr = curr.Next;
                    continue;
                }

                if (curr.IsNavigationNode && !curr.IsIntersection() && isClosed && curr.Next == null)
                {
                    curr.PrimaryNavigationNodeEdge = nextNavigationNode.PrimaryDirectionEdge;
                    curr.SecondaryNavigationNodeEdge = nextNavigationNode.SecondaryDirectionEdge;
                    return;
                }


                curr.PrimaryNavigationNodeEdge = prevNavigationNode.PrimaryDirectionEdge;
                curr.SecondaryNavigationNodeEdge = nextNavigationNode.SecondaryDirectionEdge;
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
                    foreach (Intersection intersection in road.Intersections)
                    {
                        if (curr.Position == intersection.Road1AnchorPoint1)
                        {
                            if(curr.PrimaryNavigationNodeEdge.EndNavigationNode.RoadNode.Position != intersection.IntersectionPosition)
                            {
                                intersection.Road1AnchorPoint1NavigationEdge = curr.PrimaryNavigationNodeEdge;
                            }
                            else
                            {
                                intersection.Road1AnchorPoint1NavigationEdge = curr.SecondaryNavigationNodeEdge;
                            }
                        }
                        if (curr.Position == intersection.Road1AnchorPoint2)
                        {
                            if(curr.PrimaryNavigationNodeEdge.EndNavigationNode.RoadNode.Position != intersection.IntersectionPosition)
                            {
                                intersection.Road1AnchorPoint2NavigationEdge = curr.PrimaryNavigationNodeEdge;
                            }
                            else
                            {
                                intersection.Road1AnchorPoint2NavigationEdge = curr.SecondaryNavigationNodeEdge;
                            }

                        }
                        if (curr.Position == intersection.Road2AnchorPoint1)
                        {
                            if(curr.PrimaryNavigationNodeEdge.EndNavigationNode.RoadNode.Position != intersection.IntersectionPosition)
                            {
                                intersection.Road2AnchorPoint1NavigationEdge = curr.PrimaryNavigationNodeEdge;
                            }
                            else
                            {
                                intersection.Road2AnchorPoint1NavigationEdge = curr.SecondaryNavigationNodeEdge;
                            }
                        }
                        if (curr.Position == intersection.Road2AnchorPoint2)
                        {
                            if(curr.PrimaryNavigationNodeEdge.EndNavigationNode.RoadNode.Position != intersection.IntersectionPosition)
                            {
                                intersection.Road2AnchorPoint2NavigationEdge = curr.PrimaryNavigationNodeEdge;
                            }
                            else
                            {
                                intersection.Road2AnchorPoint2NavigationEdge = curr.SecondaryNavigationNodeEdge;
                            }
                        }
                    }

                }
                curr = curr.Next;
            }
        }

        public void ReverseEdges() 
        {
            RoadNode curr = this;
            while (curr != null) 
            {
                if (curr.PrimaryNavigationNodeEdge == null)
                {
                    curr = curr.Next;
                    continue;
                }
                foreach (NavigationNodeEdge edge in curr.PrimaryNavigationNodeEdge.EndNavigationNode.Edges)
                {
                    if (edge.EndNavigationNode == curr.PrimaryNavigationNodeEdge.StartNavigationNode)
                    {

                        curr.PrimaryNavigationNodeEdge = edge;
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