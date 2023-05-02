using UnityEngine;
using System.Linq;
using POIs;

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
        private Road _road;
        public TrafficSignType? TrafficSignType;
        public TrafficLight TrafficLight;
        public Intersection Intersection;
        public NavigationNodeEdge PrimaryNavigationNodeEdge;
        public NavigationNodeEdge SecondaryNavigationNodeEdge;
        public bool IsNavigationNode = false;
        public NavigationNode PrimaryNavigationNode;
        public NavigationNode SecondaryNavigationNode;
        public POI POI = null;
        private Vector3 _tangent;
        private Vector3 _normal;
        private RoadNodeType _type;
        private float _time;

        // A list of all intersection types
        private static RoadNodeType[] _intersectionTypes = new RoadNodeType[]{ RoadNodeType.ThreeWayIntersection, RoadNodeType.FourWayIntersection, RoadNodeType.Roundabout };
        
        public RoadNode(Road road, Vector3 position, Vector3 tangent, Vector3 normal, RoadNodeType type, float distanceToPrevNode, float time) : this(road, position, tangent, normal, type, null, null, distanceToPrevNode, time){}
        public RoadNode(Road road, Vector3 position, Vector3 tangent, Vector3 normal, RoadNodeType type, RoadNode prev, RoadNode next, float distanceToPrevNode, float time, Intersection intersection = null)
        {
            _road = road;
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
            _index = prev == null ? 0 : prev.Index + 1;
        }
        public override RoadNode Copy()
        {
            return new RoadNode(_road, _position, _tangent, _normal, _type, _prev, _next, _distanceToPrevNode, _time)
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
                
                while(curr.Next != null && curr.Next.Road == this.Road)
                {
                    if(!curr.IsIntersection())
                        count++;
                    curr = curr.Next;
                }
                return count;
            }
        }
        public void AddNavigationEdgeToRoadNodes(NavigationNode startNavigationNode, bool isClosed, bool inPrimaryDirection)
        {
            // Since the start node can be another road if they are connected, we use StartRoadNode and EndRoadNode to get the correct start node
            RoadNode curr = inPrimaryDirection ? startNavigationNode.RoadNode.Road.StartRoadNode : startNavigationNode.RoadNode.Road.EndRoadNode;
            NavigationNode prevNavigationNode;
            NavigationNodeEdge currentNavigationNodeEdge = startNavigationNode.Edges[0];

            // Changing the prev and next navigation node to always be the nodes closest to the current node in each direction
            while(curr != null) 
            {
                // If the current node is not on the same road as the start node, then we have reached the end of the road
                if (curr.Road != this.Road)
                    break;
                
                if (curr.IsIntersection() || curr.IsNavigationNode)
                {
                    // We do not want to skip the first node of the road
                    if (curr.Position == startNavigationNode.RoadNode.Position)
                    {
                        curr = inPrimaryDirection ? curr.Next : curr.Prev;
                        continue;
                    }
                    // If the intersection is a threeway intersection at the end of the road
                    if (curr.Type == RoadNodeType.ThreeWayIntersection && (curr.Next?.Type == RoadNodeType.End || curr.Prev?.Type == RoadNodeType.End))
                    {
                        curr = inPrimaryDirection ? curr.Next : curr.Prev;
                        continue;
                    }

                    if (inPrimaryDirection)
                    {
                        prevNavigationNode = currentNavigationNodeEdge.EndNavigationNode;
                        currentNavigationNodeEdge = prevNavigationNode.PrimaryDirectionEdge;
                        curr.PrimaryNavigationNodeEdge = currentNavigationNodeEdge;
                    }
                    else
                    {
                        prevNavigationNode = currentNavigationNodeEdge.EndNavigationNode;
                        currentNavigationNodeEdge = prevNavigationNode.SecondaryDirectionEdge;
                        curr.SecondaryNavigationNodeEdge = currentNavigationNodeEdge;
                    }

                    curr = inPrimaryDirection ? curr.Next : curr.Prev;
                    continue;
                }

                // If the end of the road and the road is closed
                if (curr.IsNavigationNode && !curr.IsIntersection() && isClosed && curr.Next == null)
                {
                    prevNavigationNode = currentNavigationNodeEdge.EndNavigationNode;
                    if (inPrimaryDirection)
                        curr.PrimaryNavigationNodeEdge = prevNavigationNode.PrimaryDirectionEdge;
                    return;
                }

                if (inPrimaryDirection)
                    curr.PrimaryNavigationNodeEdge = currentNavigationNodeEdge;
                else
                    curr.SecondaryNavigationNodeEdge = currentNavigationNodeEdge;

                curr = inPrimaryDirection ? curr.Next : curr.Prev;
            }
        }

        public void UpdateIntersectionJunctionEdgeNavigation(Road road)
        {
            RoadNode curr = this;
            while(curr != null && curr.Road == road)
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
                    IntersectionArm arm = intersection.GetIntersectionArmAtJunctionEdge(curr);

                    if (arm == null)
                        continue;

                    IntersectionArm intersectionArm = arm;
                    intersectionArm.NavigationNodeEdgeOutwards = edge;
                }
                curr = curr.Next;
            }
        }
        public float GetDistanceToEndOfRoad()
        {
            RoadNode current = this.Next;
            float distance = 0;
            while(current != null)
            {
                if (current.Road != this.Road)
                    break;
                
                distance += current.DistanceToPrevNode;
                current = current.Next;
            }
            return distance;
        }

        /// <summary>Returns `true` if this node is an intersection</summary>
        public bool IsIntersection() => _intersectionTypes.Contains(_type);

        public Road Road
        {
            get => _road;
        }

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