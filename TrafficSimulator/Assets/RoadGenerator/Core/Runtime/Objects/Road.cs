using System.Collections.Generic;
using UnityEngine;
using System;

namespace RoadGenerator
{
    /// <summary>The amount of lanes in each direction of a road. In total the road will have twice as many lanes</summary>
    public enum LaneAmount
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4
    }

    struct IntersectionVertexPoints : IComparable<IntersectionVertexPoints>
    {
        public int StartIndex;
        public Vector3 IntersectionPoint;
        public int EndIndex;
        public IntersectionType Type;
        public bool IsThreeWayRoad1;
        public IntersectionVertexPoints(int startIndex, Vector3 intersectionPoint, int endIndex, IntersectionType type, bool isThreeWayRoad1)
        {
            StartIndex = startIndex < endIndex ? startIndex : endIndex;
            IntersectionPoint = intersectionPoint;
            EndIndex = endIndex > startIndex ? endIndex : startIndex;
            Type = type;
            IsThreeWayRoad1 = isThreeWayRoad1;
        }
        public int CompareTo(IntersectionVertexPoints other)
        {
            return StartIndex.CompareTo(other.StartIndex);
        }

        public RoadNodeType GetRoadNodeType()
        {
            return Type == IntersectionType.FourWayIntersection ? RoadNodeType.FourWayIntersection : RoadNodeType.ThreeWayIntersection;
        }
    }

    
    [ExecuteInEditMode()]
    [RequireComponent(typeof(PathCreator))]
    [RequireComponent(typeof(RoadMeshCreator))]
    [Serializable]
	public class Road : MonoBehaviour
	{
        [Header ("Connections")]
        public GameObject RoadObject;
        public RoadSystem RoadSystem;
        
        [Header ("Road settings")]
        public LaneAmount LaneAmount = LaneAmount.One;
        public float LaneWidth = 4f;
        [Range (0, .5f)] public float Thickness = .15f;
        
        [Header ("Lane settings")]
        [Range(0.1f, 10f)] public float LaneVertexSpacing = 1f;
        public bool DrawLanes = false;
        
        [SerializeField][HideInInspector] private RoadNode _start = new RoadNode(Vector3.zero, Vector3.zero, Vector3.zero, RoadNodeType.End, 0, 0);
        [SerializeField][HideInInspector] private List<Lane> _lanes = new List<Lane>();
        [SerializeField][HideInInspector] private GameObject _laneContainer;
        [SerializeField][HideInInspector] private VertexPath _path;
        [SerializeField][HideInInspector] private EndOfPathInstruction _endOfPathInstruction = EndOfPathInstruction.Stop;
        [HideInInspector] public List<Intersection> _intersections = new List<Intersection>();
        [SerializeField][HideInInspector] private RoadNavigationGraph _navigationGraph;
        [SerializeField][HideInInspector] private float _length;
        
        private const string LANE_NAME = "Lane";
        private const string LANE_CONTAINER_NAME = "Lanes";


        public Intersection[] GetIntersections()
        {
            return _intersections.ToArray();
        }

        public bool HasIntersection(Intersection intersection)
        {
            return _intersections.Contains(intersection);
        }

        public void AddIntersection(Intersection intersection)
        {
            _intersections.Add(intersection);
        }
        public bool RemoveIntersection(Intersection intersection)
        {
            _intersections.Remove(intersection);
            UpdateRoad();
            return true;
        }

        /// <summary>This function is called during the time a node is being dragged</summary>
        public void OnDrag()
        {
            // Do nothing when nodes are moved
        }

        /// <summary>This function is called when the road has changed, like moving a node or adding/removing nodes</summary>
        public void OnChange()
        {
            // Update the intersections and road when a node is changed
            IntersectionCreator.UpdateIntersections(this);
            UpdateRoad();
        }

        public void UpdateMesh()
        {
            UpdateRoadNodes();
            RoadMeshCreator roadMeshCreator = RoadObject.GetComponent<RoadMeshCreator>();
            if(roadMeshCreator != null)
                roadMeshCreator.UpdateMesh();
        }

        private void UpdateRoad()
        {
            RoadMeshCreator roadMeshCreator = RoadObject.GetComponent<RoadMeshCreator>();
            if(roadMeshCreator != null)
            {
                UpdateRoadNodes();
                UpdateLanes();
                roadMeshCreator.UpdateMesh();
                foreach(Intersection intersection in _intersections)
                    intersection.UpdateMesh();
                RoadSystem.UpdateRoadSystemGraph();
                ShowLanes();
            }
        }

        private void AddIntersectionNode(ref RoadNode curr, Vector3 position, RoadNodeType type)
        {
            // TODO: Look into if these distances need to follow the path
            float distanceToIntersection = Vector3.Distance(curr.Position, position);

            RoadNode next = curr.Next == null ? null : curr.Next.Next;

            float time = next == null ? curr.Time : (next.Time + curr.Time) / 2;

            // Add the intersection node
            curr.Next = new RoadNode(position, curr.Tangent, curr.Normal, type, curr, next, distanceToIntersection, time);
            curr = curr.Next;

            // If the road does not end at the intersection, update the distance on the following node as well
            if(curr != null)
                curr.DistanceToPrevNode = Vector3.Distance(position, curr.Position);
        }
        
        /// <summary>Updates the road nodes</summary>
        public void UpdateRoadNodes()
        {
            // Create the vertex path for the road
            BezierPath path = RoadObject.GetComponent<PathCreator>().bezierPath;
            _path = new VertexPath(path, transform, LaneVertexSpacing);
            
            this._length = _path.length;

            // Set the end of path instruction depending on if the path is closed or not
            this._endOfPathInstruction = path.IsClosed ? EndOfPathInstruction.Loop : EndOfPathInstruction.Stop;

            // Create the start node for the road. The start node must be an end node
            this._start = new RoadNode(_path.GetPoint(0), _path.GetTangent(0), _path.GetNormal(0), RoadNodeType.End, 0, _path.times[0]);
            
            // Create a previous and current node that will be used when creating the linked list
            RoadNode prev = null;
            RoadNode curr = _start;

            // Calculating the path distance for each intersection on the road
            PriorityQueue<IntersectionVertexPoints> intersectionVertices = new PriorityQueue<IntersectionVertexPoints>();
            
            foreach(Intersection intersection in _intersections)
            {
                int startIndex = -1;
                int endIndex = -1;

                if(intersection.Type == IntersectionType.ThreeWayIntersectionAtStart || intersection.Type == IntersectionType.ThreeWayIntersectionAtEnd)
                { 
                    if(intersection.Road1 == this)
                    {
                        // This is Road1, so the intersection is somewhere in the middle of this road
                        Vector3 anchor1 = intersection.Road1AnchorPoint1;
                        Vector3 anchor2 = intersection.Road1AnchorPoint2;
                        startIndex = _path.GetClosestIndexOnPath(anchor1);
                        endIndex = _path.GetClosestIndexOnPath(anchor2);
                    }
                    else
                    {
                        // This is Road2, so the intersection is at the start or end of this road
                        // The first anchor is AnchorPoint1 of Road2, however the second anchor is the intersection position since it starts or ends there
                        Vector3 anchor1 = intersection.Road2AnchorPoint1;
                        Vector3 anchor2 = intersection.IntersectionPosition;

                        bool isStart = intersection.Type == IntersectionType.ThreeWayIntersectionAtStart;

                        // Force the end index to be either the last index or the first index since the road either starts or ends at the intersection
                        int edgeIndex = isStart ? 0 : _path.NumPoints - 1;
                        int junctionIndex = _path.GetClosestIndexOnPath(anchor1);
                        
                        // Set the start and end indices accordingly
                        startIndex = isStart ? edgeIndex : junctionIndex;
                        endIndex = isStart ? junctionIndex : edgeIndex;
                    }
                    
                }
                else if(intersection.Type == IntersectionType.FourWayIntersection)
                {
                    Vector3 anchor1 = intersection.Road1 == this ? intersection.Road1AnchorPoint1 : intersection.Road2AnchorPoint1;
                    Vector3 anchor2 = intersection.Road1 == this ? intersection.Road1AnchorPoint2 : intersection.Road2AnchorPoint2;
                    startIndex = _path.GetClosestIndexOnPath(anchor1);
                    endIndex = _path.GetClosestIndexOnPath(anchor2);
                }

                Vector3 intersectionPoint = intersection.IntersectionPosition;
                intersectionVertices.Enqueue(new IntersectionVertexPoints(startIndex, intersectionPoint, endIndex, intersection.Type, intersection.Road1 == this));
            }

            // Go through each point in the path of the road
            for(int i = 0; i < _path.NumPoints; i++)
            {
                // Add an intersection node if there is an intersection between the previous node and the current node
                IntersectionVertexPoints? possibleNextIntersection = intersectionVertices.Count > 0 ? intersectionVertices.Peek() : null;
                if(possibleNextIntersection != null)
                {
                    IntersectionVertexPoints nextIntersection = (IntersectionVertexPoints)possibleNextIntersection;
                    if(i == nextIntersection.StartIndex || i == nextIntersection.EndIndex)
                    {
                        // If the intersection is a 3-way intersection, we want to add a junction node:
                        // To both indices if this is Road1
                        // To the end index if if the intersection is at the start
                        // To the start index if the intersection is at the end
                        bool threeWayShouldAddJunctionEdge = 
                            nextIntersection.IsThreeWayRoad1
                            || (nextIntersection.Type == IntersectionType.ThreeWayIntersectionAtStart && i == nextIntersection.EndIndex)
                            || (nextIntersection.Type == IntersectionType.ThreeWayIntersectionAtEnd && i == nextIntersection.StartIndex);
    
                        // Add a junction edge node                    
                        if(nextIntersection.GetRoadNodeType() == RoadNodeType.FourWayIntersection || threeWayShouldAddJunctionEdge)
                        {
                            prev = curr;
                            curr = new RoadNode(_path.GetPoint(i), _path.GetTangent(i), _path.GetNormal(i), RoadNodeType.JunctionEdge, prev, null, _path.DistanceBetweenPoints(i - 1, i), _path.times[i]);
                            prev.Next = curr;
                        }
                        
                        
                        // Add the intersection node after the first junction edge node
                        if(i == nextIntersection.StartIndex)
                        {
                            AddIntersectionNode(ref curr, nextIntersection.IntersectionPoint, nextIntersection.GetRoadNodeType());
                        }
                        
                        // If this is Road2 on a 3-way intersection at the end, add a final end node
                        if(!nextIntersection.IsThreeWayRoad1 && nextIntersection.Type == IntersectionType.ThreeWayIntersectionAtEnd && i == nextIntersection.EndIndex)
                        {
                            // Update the previous node and create a new current node
                            prev = curr;
                            curr = new RoadNode(_path.GetPoint(i), _path.GetTangent(i), _path.GetNormal(i), RoadNodeType.End, prev, null, _path.DistanceBetweenPoints(i - 1, i), _path.times[i]);

                            // Set the next pointer for the previous node
                            prev.Next = curr;
                        }
                        
                        
                        if(i == nextIntersection.EndIndex)
                            intersectionVertices.Dequeue();
                        
                        continue;
                    }
                    else if(i > nextIntersection.StartIndex && i < nextIntersection.EndIndex)
                    {
                        // Do not add any nodes other nodes between the start and end of the intersection
                        continue;
                    }
                }
                
                // The first iteration is only for 3-way intersections at the start, so skip the rest of the first iteration
                if(i == 0)
                    continue;
                
                // The current node type is assumed to be default
                RoadNodeType currentType = RoadNodeType.Default;
                
                // If the current node is the last node in the path, then the current node type is an end node
                if(i == _path.NumPoints - 1)
                {
                    currentType = RoadNodeType.End;
                }

                // Update the previous node and create a new current node
                prev = curr;
                curr = new RoadNode(_path.GetPoint(i), _path.GetTangent(i), _path.GetNormal(i), currentType, prev, null, _path.DistanceBetweenPoints(i - 1, i), _path.times[i]);

                // Set the next pointer for the previous node
                prev.Next = curr;
            }
            // Create a new navigation graph
            _navigationGraph = new RoadNavigationGraph(_start, path.IsClosed);
        }

        /// <summary>Updates the lanes</summary>
        private void UpdateLanes()
        {
            // Get the lane count
            int laneCount = (int)LaneAmount;

            // Use the driving side as a coefficient to offset the lanes in the correct direction based on the driving side
            int drivingSide = (int)RoadSystem.DrivingSide;

            // Remove all lanes
            _lanes.Clear();

            // The primary lane starts with the first road node, but since the secondary lane goes in the opposite direction it starts with the last road node
            RoadNode primaryLaneNodeStart = _start;
            RoadNode secondaryLaneNodeStart = _start.Reverse();

            // Create the lanes
            for(int i = 0; i < laneCount; i++)
            {
                // Get the center path of the road
                BezierPath path = RoadObject.GetComponent<PathCreator>().bezierPath;
                
                // Create a primary (same direction as driving side) lane path
                BezierPath primaryLaneBezierPath = path.OffsetInNormalDirection(-drivingSide * LaneWidth * (i + 0.5f), transform, LaneVertexSpacing);
                VertexPath primaryLaneVertexPath = new VertexPath(primaryLaneBezierPath, transform, LaneVertexSpacing);

                // Create a secondary (opposite direction as driving side) lane path that is reversed so that it goes in the opposite direction to the primary lane
                BezierPath secondaryLaneBezierPath = path.OffsetInNormalDirection(drivingSide * LaneWidth * (i + 0.5f), transform, LaneVertexSpacing, true);
                VertexPath secondaryLaneVertexPath = new VertexPath(secondaryLaneBezierPath, transform, LaneVertexSpacing);
                
                // Create the primary and secondary lanes
                Lane primaryLane = new Lane(this, primaryLaneNodeStart, new LaneType(LaneSide.PRIMARY, i), primaryLaneVertexPath);
                Lane secondaryLane = new Lane(this, secondaryLaneNodeStart, new LaneType(LaneSide.SECONDARY, i), secondaryLaneVertexPath);

                // Add the lanes to the list
                _lanes.Add(primaryLane);
                _lanes.Add(secondaryLane);
            }
        }

        public void ShowLanes()
        {
            if(_laneContainer == null)
            {
                // Try to find the lane container if it has already been created
                foreach(Transform child in transform)
                {
                    if(child.name == LANE_CONTAINER_NAME)
                    {
                        _laneContainer = child.gameObject;
                        break;
                    }
                }
            }

            // Destroy the lane container, and with it all the previous lanes
            if(_laneContainer != null)
                DestroyImmediate(_laneContainer);

            // Create a new empty lane container
            _laneContainer = new GameObject(LANE_CONTAINER_NAME);
            _laneContainer.transform.parent = transform;

            // Draw the lines if the setting is enabled
            if(DrawLanes)
            {
                // Draw each lane
                for(int i = 0; i < _lanes.Count; i++)
                {
                    DrawLane(this, _lanes[i], GetColor(i), _laneContainer);
                }
            }
        }

        /// <summary>Returns true if the road is a closed loop</summary>
        public bool IsClosed()
        {
            return RoadObject.GetComponent<PathCreator>().bezierPath.IsClosed;
        }
        
        /// <summary>Get the position at a distance from the start of the path</summary>
        public Vector3 GetPositionAtDistance(float distance, EndOfPathInstruction? endOfPathInstruction = null)
        {
            EndOfPathInstruction eopi = endOfPathInstruction == null ? _endOfPathInstruction : (EndOfPathInstruction)endOfPathInstruction;
            return _path.GetPointAtDistance(distance, eopi);
        }
        
        /// <summary>Get the rotation at a distance from the start of the path</summary>
        public Quaternion GetRotationAtDistance(float distance, EndOfPathInstruction? endOfPathInstruction = null)
        {
            EndOfPathInstruction eopi = endOfPathInstruction == null ? _endOfPathInstruction : (EndOfPathInstruction)endOfPathInstruction;
            return _path.GetRotationAtDistance(distance, eopi);
        }

        /// <summary>Returns a color based on the seed</summary>
        public static Color GetColor(int seed)
        {
            List<Color> colors = new List<Color>(){ Color.red, Color.blue, Color.green, Color.cyan, Color.magenta };
            return colors[seed % colors.Count];
        }

        /// <summary>Helper function that performs the drawing of a lane's path</summary>
        private static void DrawLanePath(GameObject line, Lane lane, Color color, float width = 0.5f)
        {
            // Get the line renderer
            LineRenderer lr = line.GetComponent<LineRenderer>();

            // Give it a material
            lr.sharedMaterial = new Material(Shader.Find("Standard"));

            // Give it a color
            lr.sharedMaterial.SetColor("_Color", color);
            
            // Give it a width
            lr.startWidth = width;
            lr.endWidth = width;
            
            // Set the positions
            lr.positionCount = lane.StartNode.Count;
            lr.SetPositions(lane.StartNode.GetPositions());
        }

        /// <summary>Draws a lane</summary>
        private static void DrawLane(Road road, Lane lane, Color color, GameObject parent)
        {
            if(lane.StartNode.Count < 1) return;
            
            // Create the lane object
            GameObject laneObject = new GameObject();
            
            // Set the lane name
            string sidePrefix = lane.Type.Side == LaneSide.PRIMARY ? "Primary" : "Secondary";
            laneObject.name = sidePrefix + LANE_NAME + lane.Type.Index;
            
            // Set the lane as a child of the Lanes container object
            laneObject.transform.parent = parent.transform;

            // Add a line renderer to the lane
            laneObject.AddComponent<LineRenderer>();
            
            // Draw the lane path
            DrawLanePath(laneObject, lane, color: color);
        }

        public List<Lane> Lanes
        {
            get => _lanes;
        }
        public RoadNode StartNode
        {
            get => _start;
        }
        public EndOfPathInstruction EndOfPathInstruction
        {
            get => _endOfPathInstruction;
        }
        public RoadNavigationGraph NavigationGraph
        {
            get => _navigationGraph;
        }
        public float Length
        {
            get => _path.length;
        }
        
        void OnDestroy()
        {
            RoadSystem.RemoveRoad(this);
            int count = _intersections.Count;
            for (var i = 0; i < count; i++)
            {
                Intersection intersection = _intersections[0];
                _intersections.RemoveAt(0);
                DestroyImmediate(intersection.gameObject);
            }
            RoadSystem.UpdateRoadSystemGraph();
        }
    }
}