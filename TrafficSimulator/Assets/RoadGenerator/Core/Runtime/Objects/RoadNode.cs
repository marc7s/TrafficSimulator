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
        IntersectionGuide,
        RoadConnection,
        End
    }

    /// <summary>Represents a single node in a road</summary>
	public class RoadNode : Node<RoadNode>
	{
        public TrafficSignType? TrafficSignType;
        public Intersection Intersection;
        public NavigationNodeEdge PrimaryNavigationNodeEdge;
        public NavigationNodeEdge SecondaryNavigationNodeEdge;
        public bool IsNavigationNode = false;
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
            _tangent = tangent.normalized;
            _normal = normal.normalized;
            _type = type;
            _prev = prev;
            _next = next;
            _distanceToPrevNode = distanceToPrevNode;
            _time = time;
            Intersection = intersection;
            _id = System.Guid.NewGuid().ToString();
            _rotation = Quaternion.LookRotation(_tangent, Vector3.up);
        }
        public override RoadNode Copy()
        {
            return new RoadNode(_position, _tangent, _normal, _type, _prev, _next, _distanceToPrevNode, _time)
            {
                PrimaryNavigationNodeEdge = PrimaryNavigationNodeEdge,
                Intersection = Intersection
            };
        }

        public int CountNonIntersections
        {
            get
            {
                int count = 1;
                RoadNode curr = this;
                
                while(curr.Next != null)
                {
                    if(!curr.IsIntersection())
                        count++;
                    curr = curr.Next;
                }
                return count;
            }
        }
        public void AddNavigationEdgeToRoadNodes(NavigationNode startNavigationNode, bool isClosed)
        {
            RoadNode curr = this;
            NavigationNode prevNavigationNode = startNavigationNode;
            NavigationNode nextNavigationNode = startNavigationNode.Edges[0].EndNavigationNode;

            // Changing the prev and next navigation node to always be the nodes closest to the current node in each direction
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
                // If the end of the road and the road is closed
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
                if (curr.Type != RoadNodeType.JunctionEdge)
                {
                    curr = curr.Next;
                    continue;
                }
                
                foreach (Intersection intersection in road.Intersections)
                {
                    bool isPrimaryEdgePointingToIntersection = curr.PrimaryNavigationNodeEdge.EndNavigationNode.RoadNode.Position == intersection.IntersectionPosition;
                    NavigationNodeEdge edge = isPrimaryEdgePointingToIntersection ? curr.SecondaryNavigationNodeEdge : curr.PrimaryNavigationNodeEdge;
                    if (curr.Position == intersection.Road1AnchorPoint1)
                        intersection.Road1AnchorPoint1NavigationEdge = edge;
                    if (curr.Position == intersection.Road1AnchorPoint2)
                        intersection.Road1AnchorPoint2NavigationEdge = edge;
                    if (curr.Position == intersection.Road2AnchorPoint1)
                        intersection.Road2AnchorPoint1NavigationEdge = edge;
                    if (curr.Position == intersection.Road2AnchorPoint2)
                        intersection.Road2AnchorPoint2NavigationEdge = edge;
                }
                curr = curr.Next;
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