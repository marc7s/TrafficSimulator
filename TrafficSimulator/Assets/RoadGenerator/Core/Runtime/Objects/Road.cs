using System.Collections.Generic;
using UnityEngine;
using System;
using POIs;

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

    public struct QueuedNode : IComparable<QueuedNode>
    {
        public RoadNodeType NodeType;
        public float Distance;
        public Vector3 Position;
        public bool EndsIntersection;
        public Intersection Intersection;
        public string Reference;
        public QueuedNode(RoadNodeType nodeType, float distance, Vector3 position, bool endsIntersection, Intersection intersection)
        {
            NodeType = nodeType;
            Distance = distance;
            Position = position;
            EndsIntersection = endsIntersection;
            Intersection = intersection;
            Reference = intersection.ID;
        }
        public int CompareTo(QueuedNode other)
        {
            int distCmp = Distance.CompareTo(other.Distance);

            // Compare the distance first, if they are equal compare the node type
            return distCmp == 0 ? NodeType.CompareTo(other.NodeType) : distCmp;
        }
    }

    // A struct used for building RoadNodes. It keeps the necessary information to be able to append new nodes to the end of the list
    public struct NodeBuilder
    {
        public RoadNode Prev;
        public RoadNode Curr;
        public float CurrLength;
        public NodeBuilder(RoadNode prev, RoadNode curr, float currLength) => (Prev, Curr, CurrLength) = (prev, curr, currLength);
    }

    public enum EndOfRoadType
    {
        Start,
        End
    }
    public struct ConnectedRoad
    {
        public Road Road;
        public EndOfRoadType EndOfRoadType;
        public ConnectedRoad(Road road, EndOfRoadType endOfRoadType) => (this.Road, this.EndOfRoadType) = (road, endOfRoadType);
    }

    [ExecuteInEditMode()]
    [RequireComponent(typeof(PathCreator))]
    [Serializable]
	public abstract class Road : MonoBehaviour
	{
        [Header ("Connections")]
        public GameObject RoadObject;
        public GameObject RoadNodePrefab;
        public GameObject LaneNodePrefab;
        public RoadSystem RoadSystem;
        [SerializeField] protected GameObject _trafficSignContainer;

        [Header ("Road settings")]
        public LaneAmount LaneAmount = LaneAmount.One;
        public float LaneWidth = 6f;
        [Range (0, .5f)] public float Thickness = .15f;
        [Range(0.1f, 5f)] public float MaxAngleError = 2f;
        [Range(0, 5f)] public float MinVertexDistance = 0;
        [Range(1f, 20f)] public float MaxRoadNodeDistance = 5f;
        public SpeedLimit SpeedLimit = RoadSystem.DefaultSpeedLimit;
        public bool GenerateSpeedSigns = true;
        // If two road endpoints are within this distance of each other, they will be connected
        public float ConnectionDistanceThreshold = 3f;
        public bool IsOneWay = false;

        [Header ("Traffic sign settings")]
        public float SpeedSignDistanceFromIntersectionEdge = 5f;
        public float SpeedSignDistanceFromRoadEnd = 5f;
        public bool ShouldSpawnLampPoles = true;
        public float LampPoleIntervalDistance = 20f;
        public float LampPoleSideDistanceOffset = 1f;
        public float DefaultTrafficSignOffset = 0.5f;


        [Header ("Debug settings")]
        public bool DrawLanes = false;
        public bool DrawRoadNodes = false;
        public bool DrawLaneNodes = false;
        public bool DrawLaneNodePointers = false;
        
        [SerializeField][HideInInspector] public RoadNode StartRoadNode;
        [SerializeField][HideInInspector] public RoadNode EndRoadNode;
        [SerializeField][HideInInspector] public LaneNode EndLaneNode;
        [SerializeField][HideInInspector] protected List<Lane> _lanes = new List<Lane>();
        [SerializeField][HideInInspector] protected GameObject _laneContainer;
        [SerializeField][HideInInspector] protected GameObject _roadNodeContainer;
        [SerializeField][HideInInspector] protected GameObject _laneNodeContainer;
        [SerializeField][HideInInspector] protected GameObject _POIContainer;
        [SerializeField][HideInInspector] protected VertexPath _path;
        [SerializeField][HideInInspector] public PathCreator PathCreator;
        [SerializeField][HideInInspector] protected EndOfPathInstruction _endOfPathInstruction = EndOfPathInstruction.Stop;
        [HideInInspector] public List<Intersection> Intersections = new List<Intersection>();
        [SerializeField][HideInInspector] protected RoadNavigationGraph _navigationGraph;
        [SerializeField][HideInInspector] protected float _length;
        [HideInInspector] public bool IsFirstRoadInClosedLoop = false;
        [HideInInspector] public ConnectedRoad? ConnectedToAtStart;
        [HideInInspector] public ConnectedRoad? ConnectedToAtEnd;
        [HideInInspector] public List<POI> POIs = new List<POI>();
        [HideInInspector] public bool IsRoadClosed = false;
        protected const string POI_CONTAINER_NAME = "POIs";
        protected const string LANE_NAME = "Lane";
        protected const string LANE_CONTAINER_NAME = "Lanes";
        protected const string ROAD_NODE_CONTAINER_NAME = "Road Nodes";
        protected const string LANE_NODE_NAME = "LaneNode";
        protected const string LANE_NODE_CONTAINER_NAME = "Lane Nodes";
        protected const string LANE_NODE_POINTER_NAME = "Lane Node Pointer";
        protected const string TRAFFIC_SIGN_CONTAINER_NAME = "Traffic Sign Container";

        void Awake()
        {
            PathCreator = GetComponent<PathCreator>();
        }

        private void SetupPOIs()
        {
            POIs.Clear();
            if(_POIContainer == null)
            {
                // Try to find the lane container if it has already been created
                foreach(Transform child in transform)
                {
                    if(child.name == POI_CONTAINER_NAME)
                    {
                        _POIContainer = child.gameObject;
                        break;
                    }
                }
            }

            if(_POIContainer == null)
            {
                _POIContainer = new GameObject(POI_CONTAINER_NAME);
                _POIContainer.transform.parent = transform;
            } 
            else
            {
                foreach(Transform child in _POIContainer.transform)
                {
                    POI poi = child.GetComponent<POI>();
                    poi.Road = this;
                    poi.Setup();
                }
            }
        }

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
            return true;
        }

        /// <summary>This function is called during the time a node is being dragged</summary>
        public void OnDrag()
        {
            // Do nothing when nodes are moved
        }
        /// <summary>Connects this road with any other roads that have their endpoint close to this roads end points </summary>
        protected void ConnectRoadIfEndPointsAreClose()
        {
            // Find and map the connected roads
            MapConnectedRoads();

            if (ConnectedToAtStart == null && ConnectedToAtEnd == null)
                return;

            // Start the search at an endpoint of the road
            Road startRoad = FindStartRoadInConnection();

            if (startRoad.ConnectedToAtStart != null)
                startRoad.Reverse();

            ReverseConnectedRoad(startRoad);
            UpdateAllConnectedRoads(GetConnectedRoadsBezierAnchorPoints(startRoad));
        }

        /// <summary>Maps a connection between all roads connected to this one</summary>
        public void MapConnectedRoads()
        {
            Road currentRoad = this;
            List<Road> queuedRoads = new List<Road>();
            List<Road> visitedRoads = new List<Road>();

            queuedRoads.Add(currentRoad);

            // Search through all roads connected to each other and map the connections
            while(queuedRoads.Count > 0)
            {
                currentRoad = queuedRoads[0];
                queuedRoads.RemoveAt(0);
                if (visitedRoads.Contains(currentRoad))
                    continue;

                visitedRoads.Add(currentRoad);
                List<Road> roadsToCheck = new List<Road>();
                if (this is TramRail)
                {
                    roadsToCheck = new List<Road>(RoadSystem.TramRails);
                }
                else if (this is DefaultRoad)
                {
                    roadsToCheck = new List<Road>(RoadSystem.DefaultRoads);
                }
                foreach (Road road in roadsToCheck)
                {
                    if (road == currentRoad)
                        continue;

                    currentRoad.UpdateStartConnectionRoad(road);
                    currentRoad.UpdateEndConnectionRoad(road);
                }

                // Add the connected roads to the queue
                if (currentRoad.ConnectedToAtStart != null)
                    queuedRoads.Add(currentRoad.ConnectedToAtStart?.Road);

                if (currentRoad.ConnectedToAtEnd != null)
                    queuedRoads.Add(currentRoad.ConnectedToAtEnd?.Road);
            }
        }
        public void Reverse()
        {
            (ConnectedToAtEnd, ConnectedToAtStart) = (ConnectedToAtStart, ConnectedToAtEnd);
            PathCreator.bezierPath.Reverse();
            UpdateRoad();
        }

        protected void ReverseConnectedRoad(Road startRoad)
        {
            // Special case, closed loop between two roads
            if (startRoad.ConnectedToAtStart?.Road == startRoad.ConnectedToAtEnd?.Road)
            {
                if (startRoad.StartRoadNode.Position == startRoad.ConnectedToAtStart.Value.Road.StartRoadNode.Position)
                    startRoad.Reverse();

                return;
            }

            Road currentRoad = startRoad;
            bool firstIteration = true;
            while (currentRoad.ConnectedToAtEnd != null)
            {
                if (currentRoad == startRoad && !firstIteration)
                    return;

                Road nextRoad = currentRoad.ConnectedToAtEnd.Value.Road;
                // If the next road is not connected to its start, it will be reversed as the current road will connect to its end
                if (nextRoad.ConnectedToAtStart?.Road != currentRoad)
                    nextRoad.Reverse();

                firstIteration = false;
                currentRoad = nextRoad;
            }
        }

        /// <summary> Returns the bezier points of a joined bezier curve with anchor points of all roads connected to this road </summary>
        public List<(Vector3, PathCreator)> GetConnectedRoadsBezierAnchorPoints(Road startRoad)
        {
            List<(Vector3, PathCreator)> points = new List<(Vector3, PathCreator)>();
            Road currentRoad = startRoad; 
            while(currentRoad != null)
            {   
                // For closed loop, return when the start road is reached again
                if (currentRoad == startRoad && points.Count != 0)
                    return points;

                BezierPath bezierPath = currentRoad.PathCreator.bezierPath;
                // Add the bezier points of this road
                for (int i = 0; i < bezierPath.NumPoints; i +=3)
                {
                    if (i != 0 || currentRoad == startRoad)
                        points.Add((bezierPath.GetPoint(i), currentRoad.PathCreator));
                }

                currentRoad = currentRoad.ConnectedToAtEnd?.Road;
            }

            return points;
        }
        /// <summary> Finds the starting road in a connected road. If the connected road is a closed loop, it will return this road </summary>
        protected Road FindStartRoadInConnection()
        {
            Road endRoadInStartDirection = FindEndRoadInConnectedDirection(EndOfRoadType.Start);
            Road endRoadInEndDirection = FindEndRoadInConnectedDirection(EndOfRoadType.End);
            if (endRoadInStartDirection.ConnectedToAtStart == null)
                return endRoadInStartDirection;

            if (endRoadInEndDirection.ConnectedToAtStart == null)
                return endRoadInEndDirection;
            
            // Closed loop, if both directions doesn't have an end
            // Set a flag to indicate that this road is the first road in the closed loop
            endRoadInStartDirection.IsFirstRoadInClosedLoop = true;
            return endRoadInStartDirection;
        }

        /// <summary> Finds the end road in a connected road. If the connected road is a closed loop, it will return this road </summary>
        protected Road FindEndRoadInConnectedDirection(EndOfRoadType direction)
        {
            // If the next road in the direction is null, return this road
            if ((direction == EndOfRoadType.Start ? ConnectedToAtStart : ConnectedToAtEnd) == null)
                return this;

            Road prevRoad = this;
            Road road = direction == EndOfRoadType.Start ? ConnectedToAtStart.Value.Road : ConnectedToAtEnd.Value.Road;
            if (road == null)
                return this;

            while (true)
            {
                // Reset the closed loop flag
                road.IsFirstRoadInClosedLoop = false;
                // If the road is closed, return the road
                if((road.ConnectedToAtStart?.Road == this || road.ConnectedToAtEnd?.Road == this) && ConnectedToAtEnd?.Road == road)
                    return this;

                // Found the end of the road
                if (road.ConnectedToAtStart == null || road.ConnectedToAtEnd == null)
                    return road;

                // Find the next connected road that is not the previous road
                if (road.ConnectedToAtStart?.Road == prevRoad)
                {
                    prevRoad = road;
                    road = road.ConnectedToAtEnd?.Road;
                }
                else
                {
                    prevRoad = road;
                    road = road.ConnectedToAtStart?.Road;
                }
            }
        }

        protected void UpdateAllConnectedRoads(List<(Vector3, PathCreator)> bezierAnchorPoints)
        {
            List<Vector3> anchorPositions = new List<Vector3>();
            foreach ((Vector3, PathCreator) bezierAnchorPoint in bezierAnchorPoints)
                anchorPositions.Add(bezierAnchorPoint.Item1);

            bool isClosed = bezierAnchorPoints[0].Item1 == bezierAnchorPoints[bezierAnchorPoints.Count - 1].Item1;
            if (isClosed)
                anchorPositions.RemoveAt(anchorPositions.Count - 1);

            BezierPath connectedBezierPath = new BezierPath(anchorPositions, isClosed, PathSpace.xz);
            PathCreator currentPathCreator = bezierAnchorPoints[0].Item2;
            List<(int, PathCreator)> roadPathCreator = new List<(int, PathCreator)>();
            int count = 0;
            bool first = true;
            foreach ((Vector3, PathCreator) bezierAnchorPoint in bezierAnchorPoints)
            {
                if (bezierAnchorPoint.Item2 != currentPathCreator)
                {
                    if (!first)
                        count++;
                    else
                        first = false;

                    // count * 3 - 2 is the total amount of points the bezier path will have from the current path creator
                    roadPathCreator.Add((count * 3 - 2, currentPathCreator));
                    currentPathCreator = bezierAnchorPoint.Item2;
                    count = 0;
                }
                count++;
            }

            if (!first)
                count++;

            roadPathCreator.Add((count * 3 - 2, currentPathCreator));
            // If the road is closed, the last point of the bezier path is not added as the closed loop bezier path will handle the control points
            if (isClosed)
                roadPathCreator[roadPathCreator.Count - 1] = (roadPathCreator[roadPathCreator.Count - 1].Item1 - 3, roadPathCreator[roadPathCreator.Count - 1].Item2);

            List<Road> roads = new List<Road>();
            int index = 0;
            int pathCreatorIndex = 0;
            foreach ((int, PathCreator) roadCreator in roadPathCreator)
            {
                for (int i = 0; i < roadCreator.Item1; i++)
                {
                    pathCreatorIndex = i;
                    roadCreator.Item2.bezierPath.SetPoint(pathCreatorIndex, connectedBezierPath.GetPoint(index));
                    index++;
                }

                index--;
                roadCreator.Item2.bezierPath.NotifyPathModified();

                if (!roads.Contains(roadCreator.Item2.gameObject.GetComponent<Road>()))
                    roads.Add(roadCreator.Item2.gameObject.GetComponent<Road>());
            }

            // If the road is closed, manually add the last control points 
            if (isClosed)
            {
                PathCreator lastRoadPathCreator = roadPathCreator[roadPathCreator.Count - 1].Item2;
                lastRoadPathCreator.bezierPath.SetPoint(pathCreatorIndex + 1 , connectedBezierPath.GetPoint(index + 1));
                lastRoadPathCreator.bezierPath.SetPoint(pathCreatorIndex + 2, connectedBezierPath.GetPoint(index + 2));
                lastRoadPathCreator.bezierPath.SetPoint(pathCreatorIndex + 3, connectedBezierPath.GetPoint(0));
                lastRoadPathCreator.bezierPath.NotifyPathModified();
            }

            foreach (Road road in roads)
            {
                road.IsRoadClosed = isClosed;
                road.UpdateRoad();
            }
        }
        

        public void UpdateStartConnectionRoad(Road road)
        {
            BezierPath bezierPath = PathCreator.bezierPath;
            Vector3 startPos = bezierPath.GetFirstAnchorPos();
            BezierPath bezierPathOtherRoad = road.RoadObject.GetComponent<PathCreator>().bezierPath;
            Vector3 startPosOtherRoad = bezierPathOtherRoad.GetFirstAnchorPos();
            Vector3 endPosOtherRoad = bezierPathOtherRoad.GetLastAnchorPos();   
            
            if (Vector3.Distance(startPosOtherRoad, startPos) < ConnectionDistanceThreshold && (road.ConnectedToAtStart == null || road.ConnectedToAtStart?.Road == this))
            {
                bezierPath.SetFirstAnchorPos(startPosOtherRoad);
                ConnectedToAtStart = new ConnectedRoad(road, EndOfRoadType.Start);
                road.ConnectedToAtStart = new ConnectedRoad(this, EndOfRoadType.Start);
            }
            else if (Vector3.Distance(endPosOtherRoad, startPos) < ConnectionDistanceThreshold && (road.ConnectedToAtEnd == null || road.ConnectedToAtEnd?.Road == this))
            {
                bezierPath.SetFirstAnchorPos(endPosOtherRoad);
                ConnectedToAtStart = new ConnectedRoad(road, EndOfRoadType.End);
                road.ConnectedToAtEnd = new ConnectedRoad(this, EndOfRoadType.Start);
            }  
            else
            {
                ClearConnectedRoad(road, EndOfRoadType.Start);
            }
        }
        public void UpdateEndConnectionRoad(Road road)
        {
            BezierPath bezierPath = PathCreator.bezierPath;
            Vector3 endPos = bezierPath.GetLastAnchorPos();
            BezierPath bezierPathOtherRoad = road.RoadObject.GetComponent<PathCreator>().bezierPath;
            Vector3 startPosOtherRoad = bezierPathOtherRoad.GetFirstAnchorPos();
            Vector3 endPosOtherRoad = bezierPathOtherRoad.GetLastAnchorPos();
            if (Vector3.Distance(startPosOtherRoad, endPos) < ConnectionDistanceThreshold && (road.ConnectedToAtStart == null || road.ConnectedToAtStart?.Road == this))
            {
                bezierPath.SetLastAnchorPos(startPosOtherRoad);
                ConnectedToAtEnd = new ConnectedRoad(road, EndOfRoadType.Start);
                road.ConnectedToAtStart = new ConnectedRoad(this, EndOfRoadType.End);
            }
            else if (Vector3.Distance(endPosOtherRoad, endPos) < ConnectionDistanceThreshold && (road.ConnectedToAtEnd == null || road.ConnectedToAtEnd?.Road == this))
            {
                bezierPath.SetLastAnchorPos(endPosOtherRoad);
                ConnectedToAtEnd = new ConnectedRoad(road, EndOfRoadType.End);
                road.ConnectedToAtEnd = new ConnectedRoad(this, EndOfRoadType.End);
            }
            else
            {
                ClearConnectedRoad(road, EndOfRoadType.End);
            }
        }

        protected void ClearConnectedRoad(Road road, EndOfRoadType type)
        {
            ConnectedRoad? connectedRoad = type == EndOfRoadType.Start ? ConnectedToAtStart : ConnectedToAtEnd;
            if (connectedRoad?.Road == road && connectedRoad != null)
            {
                BezierPath bezierPathConnectedRoad = connectedRoad?.Road.RoadObject.GetComponent<PathCreator>().bezierPath;
                bezierPathConnectedRoad.AutoSetAllControlPoints();
                bezierPathConnectedRoad.NotifyPathModified();

                if (connectedRoad?.EndOfRoadType == EndOfRoadType.Start)
                    road.ConnectedToAtStart = null;
                else
                    road.ConnectedToAtEnd = null;

                if (type == EndOfRoadType.Start)
                    ConnectedToAtStart = null;
                else
                    ConnectedToAtEnd = null;

                road.OnChange();
            }
        }

        /// <summary>This function is called when the road has changed, like moving a node or adding/removing nodes</summary>
        public void OnChange()
        {
            if(RoadSystem == null)
                return;

            ConnectRoadIfEndPointsAreClose();
            // Update the intersections and road when a node is changed
            IntersectionCreator.UpdateIntersections(this);
            UpdateRoad();
        }

        protected void UpdateRoad()
        {
            UpdateRoadNodes();
            UpdateLanes();
            UpdateMesh();
            
            foreach(Intersection intersection in Intersections)
                intersection.UpdateMesh();
            
            PlaceTrafficSigns();

            ShowLanes();
            ShowRoadNodes();
            ShowLaneNodes();
        }

        public abstract void UpdateMesh();

        public void UpdateRoadNoGraphUpdate()
        {
            UpdateRoadNodes();
            UpdateLanes();
            UpdateMesh();
            
            foreach(Intersection intersection in Intersections)
                intersection.UpdateMesh();
            
            PlaceTrafficSigns();
        }

        protected (Vector3, Vector3, float, float) GetPositionsAndDistancesInOrder(Vector3 position1, Vector3 position2, VertexPath path)
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

        /// <summary> Appends a new RoadNode to the end and returns the new NodeBuilder </summary>
        protected NodeBuilder AppendNode(NodeBuilder builder, Vector3 position, Vector3 tangent, Vector3 normal, RoadNodeType type, Intersection intersection = null)
        {
            // Update the previous node
            builder.Prev = builder.Curr;
            
            // Calculate the distance from the new position to the previous node, and update the current length accordingly
            float dstToPrev = Vector3.Distance(builder.Prev.Position, position);
            builder.CurrLength += dstToPrev;
            // If an end node is connected to a road, it should be a road connection node
            if (type == RoadNodeType.End)
                type = (ConnectedToAtEnd == null || ConnectedToAtEnd?.Road.IsFirstRoadInClosedLoop == true) ? RoadNodeType.End : RoadNodeType.RoadConnection;

            // Add the new node to the end
            builder.Curr = new RoadNode(this, position, tangent, normal, type, builder.Prev, null, dstToPrev, builder.CurrLength / Length, intersection);
            bool shouldBeNavigationNode = (type == RoadNodeType.End && IsClosed()) || type == RoadNodeType.RoadConnection;
            if (shouldBeNavigationNode)
                builder.Curr.IsNavigationNode = true;

            // Update the previous node's next pointer
            builder.Prev.Next = builder.Curr;

            return builder;
        }
        
        /// <summary> Adds intermediate RoadNodes between start and end point to bridge the gap, making sure the MaxRoadNodeDistance invariant is upheld </summary>
        protected NodeBuilder AddIntermediateNodes(NodeBuilder builder, Vector3 start, Vector3 end, Vector3 tangent, Vector3 normal, RoadNodeType type = RoadNodeType.Default)
        {
            // Calculate the total distance that needs to be bridged
            float distanceToBridge = Vector3.Distance(start, end);
            
            // Create a list to hold all intermediate positions that need to be added
            List<Vector3> roadNodePositions = new List<Vector3>();

            // Calculate how many intermediate nodes to add
            int positionsToAdd = Mathf.CeilToInt(distanceToBridge / MaxRoadNodeDistance) - 1;
            
            // Add the intermediate positions to the list
            for(int posCount = 0; posCount < positionsToAdd; posCount++)
            {
                // Calculate the percentage of how far along the line from the start to the end node this intermediate node should be
                float t = (float)(posCount + 1) / (positionsToAdd + 1);

                // Calculate the position of the intermediate node
                Vector3 pos = Vector3.Lerp(start, end, t);
                // Add the position to the list
                roadNodePositions.Add(pos);
            }

            // Add all the intermediate nodes
            while(roadNodePositions.Count > 0)
            {
                // Get the first position to add
                Vector3 position = roadNodePositions[0];

                // Add the intermediate node
                builder = AppendNode(builder, position, tangent, normal, type);

                // This position has now been added, so remove it from the list
                roadNodePositions.RemoveAt(0);
            }

            return builder;
        }

        /// <summary>Updates the road nodes</summary>
        public void UpdateRoadNodes()
        {
            // Create the vertex path for the road
            BezierPath path = RoadObject.GetComponent<PathCreator>().bezierPath;
            _path = new VertexPath(path, transform, MaxAngleError, MinVertexDistance);
            
            _length = _path.length;

            // Set the end of path instruction depending on if the path is closed or not
            _endOfPathInstruction = path.IsClosed ? EndOfPathInstruction.Loop : EndOfPathInstruction.Stop;

            RoadNodeType startType = ConnectedToAtStart == null || IsFirstRoadInClosedLoop ? RoadNodeType.End : RoadNodeType.RoadConnection;
            // Create the start node for the road. The start node must be an end node
            StartRoadNode = new RoadNode(this, _path.GetPoint(0), _path.GetTangent(0), _path.GetNormal(0), startType, 0, 0);

            if (startType == RoadNodeType.RoadConnection || (startType == RoadNodeType.End && IsClosed()))
                StartRoadNode.IsNavigationNode = true;
            
            // Create a new node builder starting at the start node
            NodeBuilder roadBuilder = new NodeBuilder(null, StartRoadNode, 0);

            PriorityQueue<QueuedNode> queuedNodes = QueueIntersectionNodes();

            // A dictionary to keep track of the intersections. If it is empty then we are currently not inside an intersection
            Dictionary<string, int> insideIntersections = new Dictionary<string, int>();
            
            // Go through each point in the path of the road
            for(int i = 0; i < _path.NumPoints; i++)
            {
                Vector3 lastPosition = roadBuilder.Curr.Position;
                Vector3 currPosition = _path.GetPoint(i);

                // Handle if the road has a three way intersection at the start
                if(i == 0 && queuedNodes.Count > 0 && queuedNodes.Peek().Distance == 0)
                    insideIntersections.TryAdd(queuedNodes.Peek().Reference, 0);
                
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
                            QueuedNode nextNode = queuedNodes.Peek();

                            // At this point we know that we have a queued node to be added this iteration. However, it might be too far away, so if we are not yet
                            // in the intersection we need to bridge the gap and add intermediate RoadNodes up to the junction edge
                            if(insideIntersections.Count == 0)
                                roadBuilder = AddIntermediateNodes(roadBuilder, roadBuilder.Curr.Position, nextNode.Position, _path.GetTangent(i), _path.GetNormal(i));
                            
                            // Append the queued node
                            roadBuilder = AppendNode(roadBuilder, nextNode.Position, _path.GetTangent(i), _path.GetNormal(i), nextNode.NodeType, nextNode.Intersection);
                            
                            // Update the dictionary used to determine if we are inside an intersection
                            // Inside for 4 way intersections meaning between the junction edge, for 3 way meaning between the junction edge and intersection
                            if(nextNode.EndsIntersection)
                                insideIntersections.Remove(nextNode.Reference);  
                            else
                                insideIntersections.TryAdd(nextNode.Reference, i);

                            // The queued node has now been added, so dequeue it
                            queuedNodes.Dequeue();
                            
                            // If an intersection ended at this node, then we need to update the last and current position so no 
                            // intermediate nodes are created prior to this as those have already been created
                            if(nextNode.EndsIntersection)
                            {
                                lastPosition = roadBuilder.Curr.Position;
                                currPosition = lastPosition;
                            }
                        }
                    }
                    
                    // If we are inside an intersection, do not add this vertex point RoadNode
                    if(insideIntersections.Count > 0)
                    {
                        continue;
                    }
                }

                // Bridge the gap between the current node and the current vertex point
                roadBuilder = AddIntermediateNodes(roadBuilder, lastPosition, currPosition, _path.GetTangent(i), _path.GetNormal(i));
 
                if (i == _path.NumPoints - 1)
                    roadBuilder = AppendNode(roadBuilder, currPosition, _path.GetTangent(i), _path.GetNormal(i), RoadNodeType.End);
            }

            EndRoadNode = roadBuilder.Curr;
            ConnectRoadNodesForConnectedRoads();

            SetupPOIs();
            UpdatePOIs();

            // Create a new navigation graph
            _navigationGraph = new RoadNavigationGraph(StartRoadNode);
            StartRoadNode.AddNavigationEdgeToRoadNodes(_navigationGraph.StartNavigationNode, PathCreator.bezierPath.IsClosed, true);

            if (!IsOneWay)
                StartRoadNode.AddNavigationEdgeToRoadNodes(_navigationGraph.EndNavigationNode, PathCreator.bezierPath.IsClosed, false);
        
            // If an intersection exists on the road, update the intersection junction edge navigation
            if(Intersections.Count > 0)
                StartRoadNode.UpdateIntersectionJunctionEdgeNavigation(this);
        }

        protected void ConnectRoadNodesForConnectedRoads()
        {
            if (ConnectedToAtStart != null && !IsFirstRoadInClosedLoop)
            {
                Road road = ConnectedToAtStart?.Road;
                if (road.StartRoadNode != null && road.LaneCount > 0)
                {
                    StartRoadNode.Prev = road.EndRoadNode;
                    road.EndRoadNode.Next = StartRoadNode;
                    for (int i = 0; i < _lanes.Count; i++)
                    {
                        LaneNode otherRoadLaneNode = road._lanes[i].StartNode.GetLastLaneNodeInRoad();
                        LaneNode thisRoadLaneNode = _lanes[i].StartNode.GetLastLaneNodeInRoad();

                        if (otherRoadLaneNode == null)
                            continue;

                        if (_lanes[i].Type.Side == LaneSide.Primary)
                        {
                            _lanes[i].StartNode.Prev = otherRoadLaneNode;
                            otherRoadLaneNode.Next = _lanes[i].StartNode;
                        }
                        else
                        {
                            thisRoadLaneNode.Next = road._lanes[i].StartNode;
                            road._lanes[i].StartNode.Prev = thisRoadLaneNode;
                        }
                    }
                }
            }

            if (ConnectedToAtEnd != null && ConnectedToAtEnd?.Road.IsFirstRoadInClosedLoop == false)
            {
                Road road = ConnectedToAtEnd?.Road;

                if (road.StartRoadNode != null && road._lanes.Count != 0)
                {
                    EndRoadNode.Next = road.StartRoadNode;
                    road.StartRoadNode.Prev = EndRoadNode;
                
                    for (int i = 0; i < _lanes.Count; i++)
                    {
                        LaneNode thisRoadLaneNode = _lanes[i].StartNode.GetLastLaneNodeInRoad();
                        LaneNode otherRoadLaneNode = road._lanes[i].StartNode.GetLastLaneNodeInRoad();

                        if (thisRoadLaneNode == null)
                            continue;

                        if (_lanes[i].Type.Side == LaneSide.Primary)
                        {
                            thisRoadLaneNode.Next = road._lanes[i].StartNode;
                            road._lanes[i].StartNode.Prev = thisRoadLaneNode;
                        }
                        else
                        {
                            _lanes[i].StartNode.Prev = otherRoadLaneNode;
                            otherRoadLaneNode.Next = _lanes[i].StartNode;
                        }
                    }
                }
            }
        }

        /// <summary> Adds a new lane node and returns the new previous and new current nodes </summary>
        protected (LaneNode, LaneNode) AddLaneNode(RoadNode roadNode, LaneNode previous, LaneNode current, bool isPrimary)
        {
            // Determine the offset direction
            int direction = (int)RoadSystem.DrivingSide * (isPrimary ? 1 : -1);

            // Update the previous node since we are adding a new one
            previous = current;

            // Calculate the position of the new node
            Vector3 position = roadNode.Position - roadNode.Normal * direction * LaneWidth * (0.5f + current.LaneIndex);
            // Create the new node
            current = new LaneNode(position, isPrimary ? LaneSide.Primary : LaneSide.Secondary, current.LaneIndex, roadNode, previous, null, Vector3.Distance(position, previous.Position));
            
            // Update the next pointer of the previous node to the newly created node
            previous.Next = current;

            if (roadNode.Type == RoadNodeType.End)
               EndLaneNode = current;

            return (previous, current);
        }

        /// <summary>Updates the lanes</summary>
        public void UpdateLanes()
        {
            // If the road is a one way, then we place the LaneNodes on top of the RoadNodes
            if (IsOneWay)
            {
                _lanes.Clear();
                RoadNode curr = StartRoadNode.Next;
                LaneNode startLaneNode = new LaneNode(curr.Position, LaneSide.Primary, 0, curr, 0);
                LaneNode prevLaneNode = startLaneNode;

                // Primary lane node
                while (curr != null)
                {
                    LaneNode currLaneNode = new LaneNode(curr.Position, LaneSide.Primary, 0, curr, prevLaneNode, null, Vector3.Distance(curr.Position, prevLaneNode.Position));
                    prevLaneNode.Next = currLaneNode;
                    prevLaneNode = currLaneNode;
                    curr = curr.Next;
                }

                Lane primaryLane = new Lane(this, startLaneNode, new LaneType(LaneSide.Primary, 0));
                _lanes.Add(primaryLane);
                ConnectRoadNodesForConnectedRoads();
                return;
            }

            // Get the lane count
            int laneCount = (int)LaneAmount;

            // Use the driving side as a coefficient to offset the lanes in the correct direction based on the driving side
            int drivingSide = (int)RoadSystem.DrivingSide;

            // Remove all lanes
            _lanes.Clear();

            List<LaneNode> primaryLaneNodes = new List<LaneNode>();
            List<LaneNode> secondaryLaneNodes = new List<LaneNode>();

            RoadNode currRoadNode = StartRoadNode;
            // The list that will contain a pair of (PrevNode, CurrNode) used when creating the lane nodes
            List<(LaneNode, LaneNode)> laneNodes = new List<(LaneNode, LaneNode)>();

            // Add start nodes for every lane
            for(int i = 0; i < laneCount; i++)
            {
                // Primary lane node
                laneNodes.Add((null, new LaneNode(currRoadNode.Position - currRoadNode.Normal * drivingSide * LaneWidth * (0.5f + i), LaneSide.Primary, i, currRoadNode, 0)));

                // Secondary lane node
                laneNodes.Add((null, new LaneNode(currRoadNode.Position + currRoadNode.Normal * drivingSide * LaneWidth * (0.5f + i), LaneSide.Secondary, i, currRoadNode, 0)));
            }

            // The lane nodes for the first road node has already been added, so we skip that one
            currRoadNode = currRoadNode.Next;
            
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
                
                if (currRoadNode.Type == RoadNodeType.RoadConnection)
                    break;
                    
                currRoadNode = currRoadNode.Next;
            }

            // Create the lanes
            for(int i = 0; i < laneNodes.Count; i +=2)
            {
                // Get the final nodes from the list
                (LaneNode primaryPrev, LaneNode primaryCurr) = laneNodes[i];
                (LaneNode secondaryPrev, LaneNode secondaryCurr) = laneNodes[i + 1];

                // Create the lanes
                Lane primaryLane = new Lane(this, primaryCurr.First, new LaneType(LaneSide.Primary, i / 2));
                Lane secondaryLane = new Lane(this, secondaryCurr.First.Reverse(), new LaneType(LaneSide.Secondary, i / 2));

                // Add the lanes
                _lanes.Add(primaryLane);
                _lanes.Add(secondaryLane);
            }
            ConnectRoadNodesForConnectedRoads();
        }
        protected PriorityQueue<QueuedNode> QueueIntersectionNodes()
        {
            // Calculating the path distance for each intersection on the road
            PriorityQueue<QueuedNode> queuedNodes = new PriorityQueue<QueuedNode>();

            foreach(Intersection intersection in Intersections)
            {
                List<IntersectionArm> armsFromThisRoad = intersection.GetArms(this);
                RoadNodeType intersectionType = intersection.IsThreeWayIntersection() ? RoadNodeType.ThreeWayIntersection : RoadNodeType.FourWayIntersection;
                float intersectionDistance = _path.GetClosestDistanceAlongPath(intersection.IntersectionPosition);

                if (armsFromThisRoad.Count == 2)
                {
                        (Vector3 startPoint, Vector3 endPoint, float startDistance, float endDistance) = GetPositionsAndDistancesInOrder(armsFromThisRoad[0].JunctionEdgePosition, armsFromThisRoad[1].JunctionEdgePosition, _path);
                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, startDistance, startPoint, false, intersection));
                        queuedNodes.Enqueue(new QueuedNode(intersectionType, intersectionDistance, intersection.IntersectionPosition, false, intersection));
                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, endDistance, endPoint, true, intersection));
                }
                else if (armsFromThisRoad.Count == 1)
                {
                    bool endsIntersection = intersection.Type == IntersectionType.ThreeWayIntersectionAtEnd;
                    bool junctionEdgeEndsIntersection = intersection.Type == IntersectionType.ThreeWayIntersectionAtStart;
                    float anchorDistance = _path.GetClosestDistanceAlongPath(armsFromThisRoad[0].JunctionEdgePosition);
                    queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, anchorDistance, armsFromThisRoad[0].JunctionEdgePosition, junctionEdgeEndsIntersection, intersection));
                    queuedNodes.Enqueue(new QueuedNode(intersectionType, intersectionDistance, intersection.IntersectionPosition, endsIntersection, intersection));
                }
            } 
            return queuedNodes;
        }

        public float? DistanceToNextIntersection(RoadNode roadNode, out Intersection intersection)
        {
            RoadNode current = roadNode.Next;
            float distance = 0;
            while (current != null)
            {
                distance += current.DistanceToPrevNode;
                if (current.Type == RoadNodeType.JunctionEdge)
                {
                    intersection = current.Intersection;
                    return distance;
                }

                current = current.Next;
            }
            intersection = null;
            return null;
        }

        public abstract TrafficSignAssessor GetNewTrafficSignAssessor();

        public void PlaceTrafficSigns()
        {
            TrafficSignAssessor trafficSignCreator = GetNewTrafficSignAssessor();

            // Destroy the old container and create a new one
            if (_trafficSignContainer != null)
                DestroyImmediate(_trafficSignContainer);

            _trafficSignContainer = new GameObject(TRAFFIC_SIGN_CONTAINER_NAME);
            _trafficSignContainer.transform.parent = transform;

            RoadNode currentNode = StartNode;
            float distanceToStartNode = 0;
            float distanceToEndNode = currentNode.GetDistanceToEndOfRoad();
            float? distanceToNextIntersection = DistanceToNextIntersection(currentNode, out Intersection nextIntersection);
            float? distanceToPrevIntersection = null;
            
            // If the there is an threeway intersection at start
            bool intersectionFound = StartNode.Next.IsIntersection();
            Intersection prevIntersection = null;
            
            while(currentNode != null)
            {
                if (currentNode.Road != this)
                    return;

                distanceToStartNode += currentNode.DistanceToPrevNode;
                distanceToEndNode -= currentNode.DistanceToPrevNode;

                if (currentNode.Type == RoadNodeType.JunctionEdge)
                {
                    if (intersectionFound)
                    {
                        distanceToNextIntersection = DistanceToNextIntersection(currentNode, out nextIntersection);
                        distanceToPrevIntersection = 0;
                        prevIntersection = currentNode.Intersection;
                    }
                    intersectionFound = !intersectionFound;
                }
                else
                {
                    distanceToPrevIntersection += currentNode.DistanceToPrevNode;
                    distanceToNextIntersection -= currentNode.DistanceToPrevNode;
                }

                RoadNodeData roadNodeData = new RoadNodeData(currentNode, distanceToStartNode, distanceToEndNode, intersectionFound, this, distanceToNextIntersection, distanceToPrevIntersection, nextIntersection, prevIntersection);
                List<TrafficSignData> trafficSignsToPlace = trafficSignCreator.GetSignsThatShouldBePlaced(roadNodeData);

                foreach (TrafficSignData trafficSignData in trafficSignsToPlace)
                    SpawnTrafficSign(trafficSignData);

                currentNode = currentNode.Next;
            }
        }

        /// <summary> Spawns the traffic signs along the road </summary>
        protected GameObject SpawnTrafficSign(TrafficSignData data)
        {
            Quaternion rotation = data.RoadNode.Rotation * (data.IsForward ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
            GameObject trafficSign = Instantiate(data.SignPrefab, data.RoadNode.Position, rotation);
            data.RoadNode.TrafficSignType = data.TrafficSignType;
            bool isDrivingRight = RoadSystem.DrivingSide == DrivingSide.Right;
            Vector3 offsetDirection =  data.RoadNode.Normal * (isDrivingRight ? 1 : -1) * (data.IsForward ? 1 : -1);
            float laneWidthFromCenter = data.DistanceFromRoad + (LaneCount / (IsOneWay ? 1 : 2) * LaneWidth);
            trafficSign.transform.position += laneWidthFromCenter * offsetDirection;
            trafficSign.transform.parent = _trafficSignContainer.transform;

            if(data.TrafficSignType == TrafficSignType.TrafficLight)
                AssignTrafficLightController(data.RoadNode, trafficSign);

            return trafficSign;
        }

        protected void AssignTrafficLightController(RoadNode roadNode, GameObject trafficLightObject)
        {
            TrafficLight trafficLight = trafficLightObject.GetComponent<TrafficLight>();
            
            // Add the traffic light to the correct traffic light group, Road1 gets added to trafficLightGroup1 and Road2 gets added to trafficLightGroup2
            if (roadNode.Intersection.GetIntersectionArmAtJunctionEdge(roadNode).FlowControlGroupID == 0)
                roadNode.Intersection.TrafficLightController.TrafficLightsGroup1.Add(trafficLight);
            else
                roadNode.Intersection.TrafficLightController.TrafficLightsGroup2.Add(trafficLight);

            trafficLight.trafficLightController = roadNode.Intersection.TrafficLightController;
            roadNode.TrafficLight = trafficLight;
        }

        private void UpdatePOIs()
        {
            RoadNode curr = StartNode;
            float distance = 0;
            List<POI> toPlace = new List<POI>(POIs);
            toPlace.Sort((x, y) => x.DistanceAlongRoad.CompareTo(y.DistanceAlongRoad));
            
            while (toPlace.Count > 0 && curr != null)
            {
                while (toPlace.Count > 0 && toPlace[0].DistanceAlongRoad <= distance)
                {
                    POI poi = toPlace[0];
                    
                    // Set the POI's RoadNode depending on if it uses DistanceAlongRoad or not
                    RoadNode poiNode = poi.UseDistanceAlongRoad ? curr : poi.RoadNode;
                    
                    poiNode.IsNavigationNode = true;
                    
                    poiNode.POI = poi;
                    toPlace.RemoveAt(0);
                    
                    // Update the RoadNode if the POI uses DistanceAlongRoad
                    if(poi.UseDistanceAlongRoad)
                        poi.RoadNode = poiNode;
                    
                    // Only translate the POI if it should be moved
                    if(poi.MoveToRoadNode)
                    {
                        (Vector3 pos, Quaternion rot) = GetPOIOffsetPosition(curr, poi);
                        poi.transform.position = pos;
                        poi.transform.rotation = rot;
                    }
                    
                    poi.Setup();
                }
                
                distance += curr.DistanceToPrevNode;
                curr = curr.Next;
            }
        }

        private (Vector3, Quaternion) GetPOIOffsetPosition(RoadNode node, POI poi)
        {
            const float sideOffset = 3f;
            int sideCoef = poi.LaneSide == LaneSide.Primary ? 1 : -1;
            Bounds bounds = poi.gameObject.GetComponent<Renderer>().bounds;
            Vector3 position = node.Position + sideCoef * node.Normal * (poi.Size.x / 2 + (int)LaneAmount * LaneWidth + sideOffset);
            Quaternion rotation = poi.LaneSide == LaneSide.Secondary ? node.Rotation : node.Rotation * Quaternion.Euler(Vector3.up * 180);
            
            return (position, rotation);
        }

        /// <summary>Draws the lanes as coloured lines </summary>
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
                        LaneNodeInfo laneNodeInfo = laneNodeObject.GetComponent<LaneNodeInfo>();
                        if(laneNodeInfo != null)
                            laneNodeInfo.SetReference(curr);
                        
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
                RoadNode curr = StartRoadNode;
                int i = 0;
                while(curr != null)
                {
                    GameObject roadNodeObject = Instantiate(RoadNodePrefab, curr.Position, curr.Rotation, _roadNodeContainer.transform);
                    RoadNodeInfo roadNodeInfo = roadNodeObject.GetComponent<RoadNodeInfo>();
                    if(roadNodeInfo != null)
                        roadNodeInfo.SetReference(curr);
                    
                    roadNodeObject.name = i + " " + curr.Type;

                    if (i != 0 && (curr.Type == RoadNodeType.RoadConnection || curr.Type == RoadNodeType.End))
                        return;
                    curr = curr.Next;
                    i++;
                }
            }
        }

        /// <summary>Returns true if the road is a closed loop</summary>
        public bool IsClosed()
        {
            return RoadObject.GetComponent<PathCreator>().bezierPath.IsClosed || IsRoadClosed;
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
        protected static void DrawPath(GameObject line, Vector3[] path, Color color, float width = 0.5f)
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
        protected static void DrawLane(Lane lane, Color color, GameObject parent)
        {
            if(lane.StartNode.Count < 1) return;
            
            // Create the lane object
            GameObject laneObject = new GameObject();
            
            // Set the lane name
            string sidePrefix = lane.Type.Side == LaneSide.Primary ? "Primary" : "Secondary";
            laneObject.name = sidePrefix + LANE_NAME + lane.Type.Index;
            
            // Set the lane as a child of the Lanes container object
            laneObject.transform.parent = parent.transform;

            // Add a line renderer to the lane
            laneObject.AddComponent<LineRenderer>();
            
            // Draw the lane path
            DrawPath(laneObject, lane.StartNode.GetPositions(), color: color);
        }

        /// <summary>Draws a pointer from a lane node to its corresponding road node</summary>
        protected static void DrawAllLaneNodePointers(LaneNode laneNode, Color color, GameObject parent)
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
            get => StartRoadNode;
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
            if(RoadSystem == null) 
                return;
            
            RoadSystem.RemoveRoad(this);
            // Cleanup connected roads references
            if (ConnectedToAtStart != null)
            {
                Road road = ConnectedToAtStart?.Road;
                road.ConnectedToAtEnd = null;
            }
            
            if (ConnectedToAtEnd != null)
            {
                Road road = ConnectedToAtEnd?.Road;
                road.ConnectedToAtStart = null;
            }

            int count = Intersections.Count;
            for (int i = 0; i < count; i++)
            {
                Intersection intersection = Intersections[0];
                Intersections.RemoveAt(0);
                DestroyImmediate(intersection.gameObject);
            }
            RoadSystem.UpdateRoadSystemGraph();
        }
    }
}