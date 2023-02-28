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

    struct QueuedNode : IComparable<QueuedNode>
    {
        public RoadNodeType NodeType;
        public float Distance;
        public Vector3 Position;
        public bool EndsIntersection;
        public Intersection Intersection;
        public QueuedNode(RoadNodeType nodeType, float distance, Vector3 position, bool endsIntersection, Intersection intersection = null)
        {
            NodeType = nodeType;
            Distance = distance;
            Position = position;
            EndsIntersection = endsIntersection;
            this.Intersection = intersection;
        }
        public int CompareTo(QueuedNode other)
        {
            return Distance.CompareTo(other.Distance);
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
        public GameObject RoadNodePrefab;
        public GameObject LaneNodePrefab;
        

        [Header ("Road settings")]
        public LaneAmount LaneAmount = LaneAmount.One;
        public float LaneWidth = 4f;
        [Range (0, .5f)] public float Thickness = .15f;
        

        [Header ("Lane settings")]
        [Range(0.1f, 10f)] public float LaneVertexSpacing = 1f;
        

        [Header ("Debug settings")]
        public bool DrawLanes = false;
        public bool DrawRoadNodes = false;
        public bool DrawLaneNodes = false;
        public bool DrawLaneNodePointers = false;
        
        [SerializeField][HideInInspector] private RoadNode _start = new RoadNode(Vector3.zero, Vector3.zero, Vector3.zero, RoadNodeType.End, 0, 0);
        [SerializeField][HideInInspector] private List<Lane> _lanes = new List<Lane>();
        [SerializeField][HideInInspector] private GameObject _laneContainer;
        [SerializeField][HideInInspector] private GameObject _roadNodeContainer;
        [SerializeField][HideInInspector] private GameObject _laneNodeContainer;
        [SerializeField][HideInInspector] private VertexPath _path;
        [SerializeField][HideInInspector] private EndOfPathInstruction _endOfPathInstruction = EndOfPathInstruction.Stop;
        [HideInInspector] public List<Intersection> Intersections = new List<Intersection>();
        [SerializeField][HideInInspector] private RoadNavigationGraph _navigationGraph;
        [SerializeField][HideInInspector] private float _length;
        
        private const string LANE_NAME = "Lane";
        private const string LANE_CONTAINER_NAME = "Lanes";
        private const string ROAD_NODE_NAME = "RoadNode";
        private const string ROAD_NODE_CONTAINER_NAME = "Road nodes";
        private const string LANE_NODE_NAME = "LaneNode";
        private const string LANE_NODE_CONTAINER_NAME = "Lane nodes";
        private const string LANE_NODE_POINTER_NAME = "Lane node pointer";


        public Intersection[] GetIntersections()
        {
            return Intersections.ToArray();
        }

        public bool HasIntersection(Intersection intersection)
        {
            return Intersections.Contains(intersection);
        }

        public void AddIntersection(Intersection intersection)
        {
            Intersections.Add(intersection);
        }
        public bool RemoveIntersection(Intersection intersection)
        {
            Intersections.Remove(intersection);
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
                foreach(Intersection intersection in Intersections)
                    intersection.UpdateMesh();
                RoadSystem.UpdateRoadSystemGraph();
                ShowLanes();
                ShowRoadNodes();
                ShowLaneNodes();
            }
        }
        public void UpdateRoad2()
        {
           RoadMeshCreator roadMeshCreator = RoadObject.GetComponent<RoadMeshCreator>();
            if(roadMeshCreator != null)
            {
                UpdateRoadNodes();
                UpdateLanes();
                roadMeshCreator.UpdateMesh();
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

        private (Vector3, Vector3, float, float) GetPositionsAndDistancesInOrder(Vector3 position1, Vector3 position2, VertexPath path)
        {
            float distance1 = path.GetClosestDistanceAlongPath(position1);
            float distance2 = path.GetClosestDistanceAlongPath(position2);

            bool swap = distance1 > distance2;

            Vector3 firstPos = swap ? position2 : position1;
            Vector3 secondPos = swap ? position1 : position2;
            float firstDistance = swap ? distance2 : distance1;
            float secondDistance = swap ? distance1 : distance2;
            
            return (firstPos, secondPos, firstDistance, secondDistance);
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
            PriorityQueue<QueuedNode> queuedNodes = new PriorityQueue<QueuedNode>();
            foreach(Intersection intersection in Intersections)
            {
                if(intersection.Type == IntersectionType.ThreeWayIntersectionAtStart || intersection.Type == IntersectionType.ThreeWayIntersectionAtEnd)
                { 
                    if(intersection.Road1 == this)
                    {
                        // This is Road1, so the intersection is somewhere in the middle of this road
                        Vector3 anchor1 = intersection.Road1AnchorPoint1;
                        Vector3 anchor2 = intersection.Road1AnchorPoint2;
                        float firstDistance = _path.GetClosestDistanceAlongPath(anchor1);
                        float intersectionDistance = _path.GetClosestDistanceAlongPath(intersection.IntersectionPosition);
                        float secondDistance = _path.GetClosestDistanceAlongPath(anchor2);

                        (Vector3 startPoint, Vector3 endPoint, float startDistance, float endDistance) = GetPositionsAndDistancesInOrder(anchor1, anchor2, _path);
                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, startDistance, startPoint, false, intersection));
                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.ThreeWayIntersection, intersectionDistance, intersection.IntersectionPosition, false, intersection));
                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, endDistance, endPoint, true, intersection));
                    }
                    else
                    {
                        // This is Road2, so the intersection is at the start or end of this road
                        // The first anchor is AnchorPoint1 of Road2, however the second anchor is the intersection position since it starts or ends there
                        Vector3 anchor1 = intersection.Road2AnchorPoint1;
                        Vector3 anchor2 = intersection.IntersectionPosition;

                        bool isStart = intersection.Type == IntersectionType.ThreeWayIntersectionAtStart;

                        // Force the intersection distance to be either the the minimum or maximum since the road either starts or ends at the intersection
                        float intersectionDistance = isStart ? 0 : _path.length;
                        
                        float junctionDistance = _path.GetClosestDistanceAlongPath(anchor1);

                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, junctionDistance, anchor1, isStart, intersection));
                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.ThreeWayIntersection, intersectionDistance, intersection.IntersectionPosition, !isStart, intersection));
                    }
                }
                else if(intersection.Type == IntersectionType.FourWayIntersection)
                {
                    Vector3 anchor1 = intersection.Road1 == this ? intersection.Road1AnchorPoint1 : intersection.Road2AnchorPoint1;
                    Vector3 anchor2 = intersection.Road1 == this ? intersection.Road1AnchorPoint2 : intersection.Road2AnchorPoint2;
                    float firstDistance = _path.GetClosestDistanceAlongPath(anchor1);
                    float intersectionDistance = _path.GetClosestDistanceAlongPath(intersection.IntersectionPosition);
                    float secondDistance = _path.GetClosestDistanceAlongPath(anchor2);

                    bool swap = firstDistance > secondDistance;

                    (Vector3 startPoint, Vector3 endPoint, float startDistance, float endDistance) = GetPositionsAndDistancesInOrder(anchor1, anchor2, _path);
                    queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, startDistance, startPoint, false, intersection));
                    queuedNodes.Enqueue(new QueuedNode(RoadNodeType.FourWayIntersection, intersectionDistance, intersection.IntersectionPosition, false, intersection));
                    queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, endDistance, endPoint, true, intersection));
                }
            }

            // Go through each point in the path of the road
            bool intersectionStarted = false;
            bool intersectionEnded = false;
            
            for(int i = 0; i < _path.NumPoints; i++)
            {
                // Add an intersection node if there is an intersection between the previous node and the current node
                QueuedNode? possibleNextIntersectionNode = queuedNodes.Count > 0 ? queuedNodes.Peek() : null;
                
                // If there are more intersection nodes left to add
                if(possibleNextIntersectionNode != null)
                {
                    // Get the next intersection node to be added
                    QueuedNode nextIntersectionNode = (QueuedNode)possibleNextIntersectionNode;

                    // Check that the next intersection node is between the current and next vertex point (or that we are at the last vertex point)
                    if(nextIntersectionNode.Distance >= _path.cumulativeLengthAtEachVertex[i] && (i == _path.NumPoints - 1 || nextIntersectionNode.Distance <= _path.cumulativeLengthAtEachVertex[i + 1]))
                    {
                        // There might be multiple queued nodes to be added between these vertex points, so we continue adding until there are no more nodes left to add, or the next one should be added later
                        while(queuedNodes.Count > 0 && (i == _path.NumPoints - 1 || queuedNodes.Peek().Distance <= _path.cumulativeLengthAtEachVertex[i + 1]))
                        {
                            QueuedNode nextNode = (QueuedNode)queuedNodes.Peek();
                            
                            // Create a new node for the queued intersection node
                            prev = curr;
                            curr = new RoadNode(nextNode.Position, _path.GetTangent(i), _path.GetNormal(i), nextNode.NodeType, prev, null, Vector3.Distance(prev.Position, nextNode.Position), _path.times[i], nextNode.Intersection);
                            prev.Next = curr;

                            // Update the flags used to determine if we are inside an intersection
                            // Inside for 4 way intersections meaning between the junction edge, for 3 way meaning between the junction edge and intersection
                            if(nextNode.EndsIntersection)
                                intersectionEnded = true;
                            else
                                intersectionStarted = true;

                            queuedNodes.Dequeue();
                        }
                    }
                    // If a started intersection has ended, reset the flags and do not add this vertex point
                    if(intersectionStarted && intersectionEnded)
                    {
                        intersectionStarted = false;
                        intersectionEnded = false;
                        continue;
                    }
                    
                    // If we are inside an intersection, do not add this vertex point RoadNode
                    if(intersectionStarted && !intersectionEnded)
                        continue;
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
            _navigationGraph = new RoadNavigationGraph(_start, this, path.IsClosed);
            if(Intersections.Count > 0)
            {
                // Update the navigation graph with the intersections
                _start.UpdateIntersectionJunctionEdgeNavigation(_navigationGraph.StartNavigationNode, this);
            }
            
            _start.AddNavigationEdgeToRoadNodes(_navigationGraph.StartNavigationNode);    
        }

        /// <summary> Adds a new lane node and returns the new previous and new current nodes </summary>
        private (LaneNode, LaneNode) AddLaneNode(RoadNode roadNode, LaneNode previous, LaneNode current, bool isPrimary)
        {
            // Determine the offset direction
            int direction = (int)RoadSystem.DrivingSide * (isPrimary ? 1 : -1);

            // Update the previous node since we are adding a new one
            previous = current;

            // Calculate the position of the new node
            Vector3 position = roadNode.Position - roadNode.Normal * direction * LaneWidth / 2;
            
            // Create the new node
            current = new LaneNode(position, roadNode.Rotation, roadNode, previous, null, Vector3.Distance(position, previous.Position));
            
            // Update the next pointer of the previous node to the newly created node
            previous.Next = current;

            return (previous, current);
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

            List<LaneNode> primaryLaneNodes = new List<LaneNode>();
            List<LaneNode> secondaryLaneNodes = new List<LaneNode>();

            RoadNode currRoadNode = _start;
            // The list that will contain a pair of (PrevNode, CurrNode) used when creating the lane nodes
            List<(LaneNode, LaneNode)> laneNodes = new List<(LaneNode, LaneNode)>();

            // Add start nodes for every lane
            for(int i = 0; i < laneCount; i++)
            {
                // Primary lane node
                laneNodes.Add((null, new LaneNode(currRoadNode.Position - currRoadNode.Normal * drivingSide * LaneWidth / 2, currRoadNode.Rotation, currRoadNode, 0)));

                // Secondary lane node
                laneNodes.Add((null, new LaneNode(currRoadNode.Position + currRoadNode.Normal * drivingSide * LaneWidth / 2, currRoadNode.Rotation, currRoadNode, 0)));
            }
            
            // Go through all road nodes and add the corresponding lane nodes
            while(currRoadNode != null)
            {
                // For every road node, add a pair of lane nodes for each lane. If the road has three lanes, each iteration will add two lane nodes and in total after
                // the execution of this for loop there will have been 6 lane nodes added
                for(int i = 0; i < laneNodes.Count; i += 2)
                {
                    // Get the current nodes from the list
                    (LaneNode primaryPrev, LaneNode primaryCurr) = laneNodes[i];
                    (LaneNode secondaryPrev, LaneNode secondaryCurr) = laneNodes[i + 1];

                    // Add the new nodes and update the list
                    laneNodes[i] = AddLaneNode(currRoadNode, primaryPrev, primaryCurr, true);
                    laneNodes[i + 1] = AddLaneNode(currRoadNode, secondaryPrev, secondaryCurr, false);
                }
                currRoadNode = currRoadNode.Next;
            }

            // Create the lanes
            for(int i = 0; i < laneNodes.Count; i +=2)
            {
                // Get the final nodes from the list
                (LaneNode primaryPrev, LaneNode primaryCurr) = laneNodes[i];
                (LaneNode secondaryPrev, LaneNode secondaryCurr) = laneNodes[i + 1];

                // Create the lanes
                Lane primaryLane = new Lane(this, primaryCurr.First, new LaneType(LaneSide.PRIMARY, i / 2));
                Lane secondaryLane = new Lane(this, secondaryCurr.First.Reverse(), new LaneType(LaneSide.SECONDARY, i / 2));

                // Add the lanes
                _lanes.Add(primaryLane);
                _lanes.Add(secondaryLane);
            }
        }

        /// <summary>Draws the lanes as coloured lines</summary>
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
                    DrawLane(_lanes[i], GetColor(i), _laneContainer);
                }
            }
        }

        /// <summary> Displays all lane nodes as coloured spheres </summary>
        public void ShowLaneNodes()
        {
            if(_laneNodeContainer == null)
            {
                // Try to find the lane container if it has already been created
                foreach(Transform child in transform)
                {
                    if(child.name == LANE_NODE_CONTAINER_NAME)
                    {
                        _laneNodeContainer = child.gameObject;
                        break;
                    }
                }
            }

            // Destroy the lane container, and with it all the previous lanes
            if(_laneNodeContainer != null)
                DestroyImmediate(_laneNodeContainer);

            // Create a new empty lane container
            _laneNodeContainer = new GameObject(LANE_NODE_CONTAINER_NAME);
            _laneNodeContainer.transform.parent = transform;

            // Draw the lane nodes if the setting is enabled
            if(DrawLaneNodes)
            {
                foreach(Lane lane in _lanes)
                {
                    LaneNode curr = lane.StartNode;
                    int i = 0;
                    while(curr != null)
                    {
                        GameObject laneNodeObject = Instantiate(LaneNodePrefab, curr.Position, curr.Rotation, _laneNodeContainer.transform);
                        laneNodeObject.name = LANE_NODE_NAME + i;

                        curr = curr.Next;
                        i++;
                    }

                    // Draw the lane node pointers from each lane node to its corresponding road node if the setting is enabled
                    if(DrawLaneNodePointers)
                    {
                        DrawAllLaneNodePointers(lane.StartNode, Color.cyan, _laneNodeContainer);
                    }
                }
            }
        }

        /// <summary> Displays all road nodes as coloured spheres </summary>
        public void ShowRoadNodes()
        {
            if(_roadNodeContainer == null)
            {
                // Try to find the lane container if it has already been created
                foreach(Transform child in transform)
                {
                    if(child.name == ROAD_NODE_CONTAINER_NAME)
                    {
                        _roadNodeContainer = child.gameObject;
                        break;
                    }
                }
            }

            // Destroy the lane container, and with it all the previous lanes
            if(_roadNodeContainer != null)
                DestroyImmediate(_roadNodeContainer);

            // Create a new empty lane container
            _roadNodeContainer = new GameObject(ROAD_NODE_CONTAINER_NAME);
            _roadNodeContainer.transform.parent = transform;

            // Draw the lines if the setting is enabled
            if(DrawRoadNodes)
            {
                RoadNode curr = _start;
                int i = 0;
                while(curr != null)
                {
                    GameObject roadNodeObject = Instantiate(RoadNodePrefab, curr.Position, curr.Rotation, _roadNodeContainer.transform);
                    roadNodeObject.name = ROAD_NODE_NAME + i;

                    curr = curr.Next;
                    i++;
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
        private static void DrawPath(GameObject line, Vector3[] path, Color color, float width = 0.5f)
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
            lr.positionCount = path.Length;
            lr.SetPositions(path);
        }

        /// <summary>Draws a lane</summary>
        private static void DrawLane(Lane lane, Color color, GameObject parent)
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
            DrawPath(laneObject, lane.StartNode.GetPositions(), color: color);
        }

        /// <summary>Draws a pointer from a lane node to its corresponding road node</summary>
        private static void DrawAllLaneNodePointers(LaneNode laneNode, Color color, GameObject parent)
        {
            int i = 0;
            LaneNode curr = laneNode;
            while(curr != null)
            {
                // Create the lane object
                GameObject laneNodePointerObject = new GameObject();
                
                // Set the lane name
                laneNodePointerObject.name = LANE_NODE_POINTER_NAME + i;
                
                // Set the lane as a child of the Lanes container object
                laneNodePointerObject.transform.parent = parent.transform;

                // Add a line renderer to the lane
                laneNodePointerObject.AddComponent<LineRenderer>();
                
                // Draw the lane path
                DrawPath(laneNodePointerObject, new Vector3[]{ curr.Position, curr.RoadNode.Position }, color: color);
                
                curr = curr.Next;
                i++;
            }
        }

        public List<Lane> Lanes
        {
            get => _lanes;
        }
        public int LaneCount 
        {
            get => _lanes.Count;
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
            int count = Intersections.Count;
            for (var i = 0; i < count; i++)
            {
                Intersection intersection = Intersections[0];
                Intersections.RemoveAt(0);
                DestroyImmediate(intersection.gameObject);
            }
            RoadSystem.UpdateRoadSystemGraph();
        }
    }
}