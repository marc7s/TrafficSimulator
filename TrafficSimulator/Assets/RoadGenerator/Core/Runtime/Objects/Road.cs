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
    struct NodeBuilder
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
    [RequireComponent(typeof(RoadMeshCreator))]
    [Serializable]
	public class Road : MonoBehaviour
	{
        [Header ("Connections")]
        public GameObject RoadObject;
        public RoadSystem RoadSystem;
        public GameObject RoadNodePrefab;
        public GameObject LaneNodePrefab;
        [SerializeField] private GameObject _trafficSignContainer;
        [SerializeField] private GameObject _speedSignTenKPH;
        [SerializeField] private GameObject _speedSignTwentyKPH;
        [SerializeField] private GameObject _speedSignThirtyKPH;
        [SerializeField] private GameObject _speedSignFortyKPH;
        [SerializeField] private GameObject _speedSignFiftyKPH;
        [SerializeField] private GameObject _speedSignSixtyKPH;
        [SerializeField] private GameObject _speedSignSeventyKPH;
        [SerializeField] private GameObject _speedSignEightyKPH;
        [SerializeField] private GameObject _speedSignNinetyKPH;
        [SerializeField] private GameObject _speedSignOneHundredKPH;
        [SerializeField] private GameObject _speedSignOneHundredTenKPH;
        [SerializeField] private GameObject _speedSignOneHundredTwentyKPH;
        [SerializeField] private GameObject _speedSignOneHundredThirtyKPH;
        

        [Header ("Road settings")]
        public LaneAmount LaneAmount = LaneAmount.One;
        public float LaneWidth = 4f;
        [Range (0, .5f)] public float Thickness = .15f;
        [Range(0.1f, 5f)] public float MaxAngleError = 2f;
        [Range(0, 5f)] public float MinVertexDistance = 0;
        [Range(1f, 20f)] public float MaxRoadNodeDistance = 5f;
        public SpeedLimit SpeedLimit = RoadSystem.DefaultSpeedLimit;
        public bool GenerateSpeedSigns = true;
        public float SpeedSignDistanceFromIntersectionEdge = 5f;
        

        [Header ("Debug settings")]
        public bool DrawLanes = false;
        public bool DrawRoadNodes = false;
        public bool DrawLaneNodes = false;
        public bool DrawLaneNodePointers = false;
        
        [SerializeField][HideInInspector] public RoadNode StartRoadNode;
        [SerializeField][HideInInspector] public RoadNode EndRoadNode;
        [SerializeField][HideInInspector] public LaneNode EndLaneNode;
        [SerializeField][HideInInspector] public List<Lane> _lanes = new List<Lane>();
        [SerializeField][HideInInspector] private GameObject _laneContainer;
        [SerializeField][HideInInspector] private GameObject _roadNodeContainer;
        [SerializeField][HideInInspector] private GameObject _laneNodeContainer;
        [SerializeField][HideInInspector] private VertexPath _path;
        [SerializeField][HideInInspector] public PathCreator PathCreator;
        [SerializeField][HideInInspector] private EndOfPathInstruction _endOfPathInstruction = EndOfPathInstruction.Stop;
        [HideInInspector] public List<Intersection> Intersections = new List<Intersection>();
        [SerializeField][HideInInspector] private RoadNavigationGraph _navigationGraph;
        [SerializeField][HideInInspector] private float _length;

        [HideInInspector] public ConnectedRoad? ConnectedToAtStart;
        [HideInInspector] public ConnectedRoad? ConnectedToAtEnd;
        float connectionDistanceThreshold = 3f;
        private const string LANE_NAME = "Lane";
        private const string LANE_CONTAINER_NAME = "Lanes";
        private const string ROAD_NODE_CONTAINER_NAME = "Road Nodes";
        private const string LANE_NODE_NAME = "LaneNode";
        private const string LANE_NODE_CONTAINER_NAME = "Lane Nodes";
        private const string LANE_NODE_POINTER_NAME = "Lane Node Pointer";
        private const string TRAFFIC_SIGN_CONTAINER_NAME = "Traffic Sign Container";

        void Awake()
        {
            PathCreator = GetComponent<PathCreator>();
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

        private void ConnectRoadIfEndPointsAreClose()
        {
            if (IsClosed())
                return;
            MapConnectedRoads();
            if (ConnectedToAtStart == null && ConnectedToAtEnd == null)
            {
                Debug.Log("No connection found");
                return;
            }

            // Start the search at an endpoint of the road

            Road startRoad = FindStartRoadInConnection();

            if (startRoad.ConnectedToAtStart != null)
            {
                startRoad.Reverse();
            }

            ReverseConnectedRoad(startRoad);
            //GetConnectedRoadsBezierAnchorPoints(startRoad);
            UpdateAllConnectedRoads(GetConnectedRoadsBezierAnchorPoints(startRoad));
        }
        public void MapConnectedRoads()
        {
            Road currentRoad = this;
            List<Road> queuedRoads = new List<Road>();
            List<Road> visitedRoads = new List<Road>();
            queuedRoads.Add(currentRoad);

            while(queuedRoads.Count > 0)
            {
            currentRoad = queuedRoads[0];
            queuedRoads.RemoveAt(0);
            if (visitedRoads.Contains(currentRoad))
                continue;
            visitedRoads.Add(currentRoad);

            foreach (Road road in RoadSystem.Roads)
            {
                if (road == currentRoad)
                    continue;
                currentRoad.UpdateStartConnectionRoad(road);
                currentRoad.UpdateEndConnectionRoad(road);
            }
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

        private void ReverseConnectedRoad(Road startRoad)
        {
            // Special case, closed loop between two roads
            if (startRoad.ConnectedToAtStart?.Road == startRoad.ConnectedToAtEnd?.Road)
            {
                if (startRoad.StartRoadNode.Position == startRoad.ConnectedToAtStart.Value.Road.StartRoadNode.Position)
                    startRoad.Reverse();
                return;
            }

            Road currentRoad = startRoad;
            int count = 0;
            while (currentRoad.ConnectedToAtEnd != null)
            {
                if (currentRoad == startRoad && count != 0)
                    return;
                Road nextRoad = currentRoad.ConnectedToAtEnd.Value.Road;
                // If the next road is not connected to its start, it will be reversed as the current road will connect to its end
                if (nextRoad.ConnectedToAtStart?.Road != currentRoad)
                {
                    nextRoad.Reverse();
                }
                count ++;
                if (count > 100)
                {
                    Debug.LogError("Infinite loop detected");
                    break;
                }
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
                // for closed loop, return when the start road is reached again
                if (currentRoad == startRoad && points.Count != 0)
                    return points;
                BezierPath bezierPath = currentRoad.PathCreator.bezierPath;
            // Add the bezier points of this road
            for (var i = 0; i < bezierPath.NumPoints; i +=3)
            {
                if (i != 0 || currentRoad == startRoad)
                    points.Add((bezierPath.GetPoint(i), currentRoad.PathCreator));
            }

            currentRoad = currentRoad.ConnectedToAtEnd?.Road;
            }

            return points;
        }
        /// <summary> Finds the starting road in a connected road. If the connected road is a closed loop, it will return this road </summary>
        private Road FindStartRoadInConnection()
        {
            Road endRoadInStartDirection = FindEndRoadInConnectedDirection(EndOfRoadType.Start);
            Road endRoadInEndDirection = FindEndRoadInConnectedDirection(EndOfRoadType.End);
            if (endRoadInStartDirection.ConnectedToAtStart == null)
                return endRoadInStartDirection;
            if (endRoadInEndDirection.ConnectedToAtStart == null)
                return endRoadInEndDirection;
            return endRoadInStartDirection;
        }
        private Road FindEndRoadInConnectedDirection(EndOfRoadType direction)
        {
            if ((direction == EndOfRoadType.Start ? ConnectedToAtStart : ConnectedToAtEnd) == null)
                return this;
            Road prevRoad = this;
            Road road = direction == EndOfRoadType.Start ? ConnectedToAtStart.Value.Road : ConnectedToAtEnd.Value.Road;
            if (road == null)
                return this;
            while (true)
            {
                // If the road is closed, return the road
                if((road.ConnectedToAtStart?.Road == this || road.ConnectedToAtEnd?.Road == this) && ConnectedToAtEnd?.Road == road)
                    return this;
                if (road.ConnectedToAtStart == null || road.ConnectedToAtEnd == null)
                    return road;
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
        private void UpdateAllConnectedRoads(List<(Vector3, PathCreator)> bezierAnchorPoints)
        {
            List<Vector3> anchorPositions = new List<Vector3>();
            foreach ((Vector3, PathCreator) bezierAnchorPoint in bezierAnchorPoints)
            {
                anchorPositions.Add(bezierAnchorPoint.Item1);
            }
            bool isClosed = bezierAnchorPoints[0].Item1 == bezierAnchorPoints[bezierAnchorPoints.Count - 1].Item1;
            if (isClosed)
                anchorPositions.RemoveAt(anchorPositions.Count - 1);
            BezierPath connectedBezierPath = new BezierPath(anchorPositions, isClosed, PathSpace.xz);

            PathCreator currentPathCreator = bezierAnchorPoints[0].Item2;
            List<(int, PathCreator)> roadsCreator = new List<(int, PathCreator)>();
            int count = 0;
            bool first = true;
            foreach ((Vector3, PathCreator) bezierAnchorPoint in bezierAnchorPoints)
            {
                if (bezierAnchorPoint.Item2 != currentPathCreator)
                {
                    if (!first)
                        count ++;
                    else
                        first = false;
                    roadsCreator.Add((count * 3 - 2, currentPathCreator));
                    currentPathCreator = bezierAnchorPoint.Item2;
                    count = 0;
                }
                
                count++;
            }

            if (!first)
                count ++;
            roadsCreator.Add((count * 3 - 2, currentPathCreator));
            // If the road is closed, the last point of the bezier path is not added as the closed loop bezier path will handle the control points
            if (isClosed)
                roadsCreator[roadsCreator.Count - 1] = (roadsCreator[roadsCreator.Count - 1].Item1 - 3, roadsCreator[roadsCreator.Count - 1].Item2);
            List<Road> roads = new List<Road>();
            int index = 0;
            int pathCreatorIndex = 0;
            foreach ((int, PathCreator) roadCreator in roadsCreator)
            {
                for (var i = 0; i < roadCreator.Item1; i++)
                {
                    pathCreatorIndex = i;
                   // Debug.Log(roadCreator.Item2 + " pathIndex"  + pathCreatorIndex + " " + connectedBezierPath.GetPoint(index));
                    roadCreator.Item2.bezierPath.SetPoint(pathCreatorIndex, connectedBezierPath.GetPoint(index));
                    index++;
                }
                index --;
                roadCreator.Item2.bezierPath.NotifyPathModified();
                if (!roads.Contains(roadCreator.Item2.gameObject.GetComponent<Road>()))
                    roads.Add(roadCreator.Item2.gameObject.GetComponent<Road>());
            }

            // If the road is closed, manually add the last control points 
            if (isClosed)
            {
                PathCreator lastRoadPathCreator = roadsCreator[roadsCreator.Count - 1].Item2;
                lastRoadPathCreator.bezierPath.SetPoint(pathCreatorIndex + 1 , connectedBezierPath.GetPoint(index + 1));
                lastRoadPathCreator.bezierPath.SetPoint(pathCreatorIndex + 2, connectedBezierPath.GetPoint(index + 2));
                lastRoadPathCreator.bezierPath.SetPoint(pathCreatorIndex + 3, connectedBezierPath.GetPoint(0));
                lastRoadPathCreator.bezierPath.NotifyPathModified();
            }

            foreach (Road road in roads)
            {
                road.UpdateRoad();
            }
        }
        

        public void UpdateStartConnectionRoad(Road road)
        {
            BezierPath bezierPath = PathCreator.bezierPath;
            Vector3 startPos = bezierPath.GetFirstAnchorPos();
            Vector3 endPos = bezierPath.GetLastAnchorPos();
            BezierPath bezierPathOtherRoad = road.RoadObject.GetComponent<PathCreator>().bezierPath;
            Vector3 startPosOtherRoad = bezierPathOtherRoad.GetFirstAnchorPos();
            Vector3 endPosOtherRoad = bezierPathOtherRoad.GetLastAnchorPos();   
            if (Vector3.Distance(startPosOtherRoad, startPos) < connectionDistanceThreshold && ((road.ConnectedToAtStart == null) || road.ConnectedToAtStart?.Road == this))
            {
                bezierPath.SetFirstAnchorPos(startPosOtherRoad);
                ConnectedToAtStart = new ConnectedRoad(road, EndOfRoadType.Start);
                road.ConnectedToAtStart = new ConnectedRoad(this, EndOfRoadType.Start);
            }
            else if (Vector3.Distance(endPosOtherRoad, startPos) < connectionDistanceThreshold && (road.ConnectedToAtEnd == null || road.ConnectedToAtEnd?.Road == this))
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
            Vector3 startPos = bezierPath.GetFirstAnchorPos();
            Vector3 endPos = bezierPath.GetLastAnchorPos();
            BezierPath bezierPathOtherRoad = road.RoadObject.GetComponent<PathCreator>().bezierPath;
            Vector3 startPosOtherRoad = bezierPathOtherRoad.GetFirstAnchorPos();
            Vector3 endPosOtherRoad = bezierPathOtherRoad.GetLastAnchorPos();
            if (Vector3.Distance(startPosOtherRoad, endPos) < connectionDistanceThreshold && (road.ConnectedToAtStart == null || road.ConnectedToAtStart?.Road == this))
            {
                bezierPath.SetLastAnchorPos(startPosOtherRoad);
                ConnectedToAtEnd = new ConnectedRoad(road, EndOfRoadType.Start);
                road.ConnectedToAtStart = new ConnectedRoad(this, EndOfRoadType.End);
            }
            else if (Vector3.Distance(endPosOtherRoad, endPos) < connectionDistanceThreshold && (road.ConnectedToAtEnd == null || road.ConnectedToAtEnd?.Road == this))
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

        private void ClearConnectedRoad(Road road, EndOfRoadType type)
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
               // road.ConnectRoadIfEndPointsAreClose();
            }
        }
        /// <summary>This function is called when the road has changed, like moving a node or adding/removing nodes</summary>
        public void OnChange()
        {
            ConnectRoadIfEndPointsAreClose();
            // Update the intersections and road when a node is changed
            IntersectionCreator.UpdateIntersections(this);
            UpdateRoad();
        }

        public void UpdateMesh()
        {
            UpdateRoadNodes();
            UpdateLanes();
            RoadMeshCreator roadMeshCreator = RoadObject.GetComponent<RoadMeshCreator>();
            if(roadMeshCreator != null)
                roadMeshCreator.UpdateMesh();
            PlaceTrafficSigns();
            // There might have been changes to the RoadNodes and therefore LaneNodes, so we need to update the visual representations
            ShowRoadNodes();
            ShowLaneNodes();
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
                PlaceTrafficSigns();
                ShowLanes();
                ShowRoadNodes();
                ShowLaneNodes();
            }
        }

        public void UpdateRoadNoGraphUpdate()
        {
            RoadMeshCreator roadMeshCreator = RoadObject.GetComponent<RoadMeshCreator>();
            if(roadMeshCreator != null)
            {
                UpdateRoadNodes();
                UpdateLanes();
                roadMeshCreator.UpdateMesh();
                foreach(Intersection intersection in Intersections)
                    intersection.UpdateMesh();
                PlaceTrafficSigns();
            } 
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

        /// <summary> Appends a new RoadNode to the end and returns the new NodeBuilder </summary>
        private NodeBuilder AppendNode(NodeBuilder builder, Vector3 position, Vector3 tangent, Vector3 normal, RoadNodeType type, Intersection intersection = null)
        {
            // Update the previous node
            builder.Prev = builder.Curr;
            
            // Calculate the distance from the new position to the previous node, and update the current length accordingly
            float dstToPrev = Vector3.Distance(builder.Prev.Position, position);
            builder.CurrLength += dstToPrev;

            RoadNodeType type2 = type == RoadNodeType.End ? ConnectedToAtEnd == null ? RoadNodeType.End : RoadNodeType.RoadConnection : type;
            // Add the new node to the end
            builder.Curr = new RoadNode(position, tangent, normal, type2, builder.Prev, null, dstToPrev, builder.CurrLength / Length, intersection);
            if (type2 == RoadNodeType.RoadConnection)
            {
                builder.Curr.IsNavigationNode = true;
            }

            builder.Curr.Road = this;
            // Update the previous node's next pointer
            builder.Prev.Next = builder.Curr;

            return builder;
        }
        
        /// <summary> Adds intermediate RoadNodes between start and end point to bridge the gap, making sure the MaxRoadNodeDistance invariant is upheld </summary>
        private NodeBuilder AddIntermediateNodes(NodeBuilder builder, Vector3 start, Vector3 end, Vector3 tangent, Vector3 normal, bool endIsLastNode, RoadNodeType type = RoadNodeType.Default)
        {
            // Calculate the total distance that needs to be bridged
            float distanceToBridge = Vector3.Distance(start, end);

            // If the distance is less than the max distance, no intermediate nodes need to be added
            if(distanceToBridge <= MaxRoadNodeDistance)
            {
                return endIsLastNode ? AppendNode(builder, end, tangent, normal, RoadNodeType.End) : builder;
            }

            
            // Create a list to hold all intermediate positions that need to be added
            List<Vector3> roadNodePositions = new List<Vector3>();

            // Calculate how many intermediate nodes to add
            int positionsToAdd = Mathf.CeilToInt(distanceToBridge / MaxRoadNodeDistance);

            // Calculate the distance between each intermediate node
            float distanceBetweenPoints = distanceToBridge / positionsToAdd;
            
            // Add the intermediate positions to the list
            for(int posCount = 0; posCount < positionsToAdd; posCount++)
            {
                // Calculate the percentage of how far along the line from the start to the end node this intermediate node should be
                float t = (float)(posCount + 1) / (positionsToAdd);

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

                // The current node type is assumed to be the desired type
                RoadNodeType currentType = type;
                
                // If the current node is the last node in the path, then the current node type is an end node
                if(endIsLastNode && roadNodePositions.Count == 1)
                    currentType = RoadNodeType.End;

                // Add the intermediate node
                builder = AppendNode(builder, position, tangent, normal, currentType);

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

            RoadNodeType startType = ConnectedToAtStart == null ? RoadNodeType.End : RoadNodeType.RoadConnection;
            // Create the start node for the road. The start node must be an end node
            StartRoadNode = new RoadNode(_path.GetPoint(0), _path.GetTangent(0), _path.GetNormal(0), startType, 0, 0);
            StartRoadNode.Road = this;
            
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
                                roadBuilder = AddIntermediateNodes(roadBuilder, roadBuilder.Curr.Position, nextNode.Position, _path.GetTangent(i), _path.GetNormal(i), false);
                            
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
                roadBuilder = AddIntermediateNodes(roadBuilder, lastPosition, currPosition, _path.GetTangent(i), _path.GetNormal(i), i == _path.NumPoints - 1);
            }
            EndRoadNode = StartRoadNode.Last;
            ConnectRoadNodesForConnectedRoads();
            // Create a new navigation graph
            _navigationGraph = new RoadNavigationGraph(StartRoadNode, path.IsClosed);
            StartRoadNode.AddNavigationEdgeToRoadNodes(_navigationGraph.StartNavigationNode, path.IsClosed); 
        
            // If an intersection exists on the road, update the intersection junction edge navigation
            if(Intersections.Count > 0)
                StartRoadNode.UpdateIntersectionJunctionEdgeNavigation(this);
        }

        private void ConnectRoadNodesForConnectedRoads()
        {
            if (ConnectedToAtStart != null)
            {
                Road road = ConnectedToAtStart?.Road;
                if (road.StartRoadNode != null && road._lanes.Count != 0)
                {
                                Debug.Log("Connecting road nodes for connected roads" + name);
                    StartRoadNode.Prev = road.EndRoadNode;
                    road.EndRoadNode.Next = StartRoadNode;
                    for (var i = 0; i < _lanes.Count; i++)
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
            if (ConnectedToAtEnd != null)
            {
                Road road = ConnectedToAtEnd?.Road;
                if (road.StartRoadNode != null && road._lanes.Count != 0)
                {
                    EndRoadNode.Next = road.StartRoadNode;
                    road.StartRoadNode.Prev = EndRoadNode;
                
                    for (var i = 0; i < _lanes.Count; i++)
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
        private (LaneNode, LaneNode) AddLaneNode(RoadNode roadNode, LaneNode previous, LaneNode current, bool isPrimary)
        {
            // Determine the offset direction
            int direction = (int)RoadSystem.DrivingSide * (isPrimary ? 1 : -1);

            // Update the previous node since we are adding a new one
            previous = current;

            // Calculate the position of the new node
            Vector3 position = roadNode.Position - roadNode.Normal * direction * LaneWidth * (0.5f + current.Index);
            // Create the new node
            current = new LaneNode(position, isPrimary ? LaneSide.Primary : LaneSide.Secondary, current.Index, roadNode, previous, null, Vector3.Distance(position, previous.Position));
            
            // Update the next pointer of the previous node to the newly created node
            previous.Next = current;

            if (roadNode.Type == RoadNodeType.End)
               EndLaneNode = current;

            return (previous, current);
        }

        /// <summary>Updates the lanes</summary>
        public void UpdateLanes()
        {
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
        private PriorityQueue<QueuedNode> QueueIntersectionNodes()
        {
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
                        float intersectionDistance = _path.GetClosestDistanceAlongPath(intersection.IntersectionPosition);

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

                        bool isStart = intersection.Type == IntersectionType.ThreeWayIntersectionAtStart;

                        // Force the intersection distance to be either the the minimum or maximum since the road either starts or ends at the intersection
                        float intersectionDistance = isStart ? 0 : _path.length;
                        
                        float junctionDistance = _path.GetClosestDistanceAlongPath(anchor1);

                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, junctionDistance, anchor1, isStart, intersection));
                        queuedNodes.Enqueue(new QueuedNode(RoadNodeType.ThreeWayIntersection, intersectionDistance, intersection.IntersectionPosition, !isStart, intersection));

                        if(!isStart)
                            queuedNodes.Enqueue(new QueuedNode(RoadNodeType.End, intersectionDistance, intersection.IntersectionPosition, false, intersection));
                    }
                }
                else if(intersection.Type == IntersectionType.FourWayIntersection)
                {
                    Vector3 anchor1 = intersection.Road1 == this ? intersection.Road1AnchorPoint1 : intersection.Road2AnchorPoint1;
                    Vector3 anchor2 = intersection.Road1 == this ? intersection.Road1AnchorPoint2 : intersection.Road2AnchorPoint2;
                    float intersectionDistance = _path.GetClosestDistanceAlongPath(intersection.IntersectionPosition);


                    (Vector3 startPoint, Vector3 endPoint, float startDistance, float endDistance) = GetPositionsAndDistancesInOrder(anchor1, anchor2, _path);
                    queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, startDistance, startPoint, false, intersection));
                    queuedNodes.Enqueue(new QueuedNode(RoadNodeType.FourWayIntersection, intersectionDistance, intersection.IntersectionPosition, false, intersection));
                    queuedNodes.Enqueue(new QueuedNode(RoadNodeType.JunctionEdge, endDistance, endPoint, true, intersection));
                }
            } 
            return queuedNodes;
        }
        // Procedurally places the traffic signs along the road
        public void PlaceTrafficSigns()
        {
            return;
            RoadNode startNode = StartRoadNode;
            // Destroy the old container and create a new one
            if (_trafficSignContainer != null)
                DestroyImmediate(_trafficSignContainer);
            _trafficSignContainer = new GameObject(TRAFFIC_SIGN_CONTAINER_NAME);
            _trafficSignContainer.transform.parent = transform;
            // If the road starts at an intersection, then the first speed sign should be placed at the end of the road
            bool intersectionFound = StartRoadNode.Next.Intersection != null && StartRoadNode.Position == StartRoadNode.Next.Position;
            if (intersectionFound)
            {
                if (this == StartRoadNode.Next.Intersection.Road1)
                    StartRoadNode.Next.Intersection.gameObject.GetComponent<TrafficLightController>().TrafficLightsGroup1 = new List<TrafficLight>();
                else
                    StartRoadNode.Next.Intersection.gameObject.GetComponent<TrafficLightController>().TrafficLightsGroup2 = new List<TrafficLight>();
            }

            if (GenerateSpeedSigns && !IsClosed())
            {
                // Place a speed sign at the start and end of the road
                PlaceTrafficSignAtDistance(startNode, SpeedSignDistanceFromIntersectionEdge, GetSpeedSignType(), true, GetSpeedSignPrefab());
                PlaceTrafficSignAtDistance(startNode.Last, SpeedSignDistanceFromIntersectionEdge, GetSpeedSignType(), false, GetSpeedSignPrefab());
            }

            RoadNode current = startNode;
            while (current != null)
            {
                // Place a speed sign after every junction edge
                if (current.Type == RoadNodeType.JunctionEdge)
                {
                    if(current.Intersection.FlowType == FlowType.TrafficLights)
                    {
                        if (!intersectionFound)
                        {
                            if (this == current.Intersection.Road1)
                                current.Intersection.gameObject.GetComponent<TrafficLightController>().TrafficLightsGroup1 = new List<TrafficLight>();
                            else
                                current.Intersection.gameObject.GetComponent<TrafficLightController>().TrafficLightsGroup2 = new List<TrafficLight>();
                        }
                        SpawnFlowController(current, 0, TrafficSignType.TrafficLight, !intersectionFound, RoadSystem.DefaultTrafficLightPrefab);
                    }
                    else if (current.Intersection.FlowType == FlowType.StopSigns)
                    {
                        SpawnFlowController(current, 0, TrafficSignType.StopSign, !intersectionFound, RoadSystem.DefaultStopSignPrefab);
                    }
                    if (GenerateSpeedSigns && !IsClosed())
                    {
                        PlaceTrafficSignAtDistance(current, intersectionFound ? SpeedSignDistanceFromIntersectionEdge : -SpeedSignDistanceFromIntersectionEdge, GetSpeedSignType(), intersectionFound, GetSpeedSignPrefab()); 
                    }
                    intersectionFound = !intersectionFound;
                }
                current = current.Next;
            }
        }
        /// <summary> Places a traffic sign at a specified distance from the road node </summary>
        private GameObject PlaceTrafficSignAtDistance(RoadNode roadNode, float distanceFromRoadNode, TrafficSignType trafficSignType, bool isForward, GameObject prefab)
        {
            // Rotate the traffic sign so that it faces the road for the current driving side
            Quaternion rotation = roadNode.Rotation * (isForward ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
            if (distanceFromRoadNode == 0)
            {
                roadNode.TrafficSignType = trafficSignType;
                return SpawnTrafficSign(roadNode.Position, rotation, prefab);
            }

            RoadNode current = isForward ? roadNode.Next : roadNode.Prev;
            float currentDistance = 0;
            while (current != null && current.Type != RoadNodeType.RoadConnection)
            {
                // Since we do not want to place a traffic sign at an intersection, we break when one is found
                if (current.Type == RoadNodeType.JunctionEdge || current.IsIntersection())
                    break;
                currentDistance += Vector3.Distance(current.Position, isForward ? current.Prev.Position : current.Next.Position);
                if (currentDistance >= Mathf.Abs(distanceFromRoadNode))
                {
                    current.TrafficSignType = trafficSignType;
                    return SpawnTrafficSign(current.Position, rotation, prefab);
                }
                current = isForward ? current.Next : current.Prev;
            }
            return null;
        }
        /// <summary> Spawns the traffic signs along the road </summary>
        private GameObject SpawnTrafficSign(Vector3 position, Quaternion rotation, GameObject prefab)
        {
            GameObject trafficSign = Instantiate(prefab, position, rotation);
            bool isDrivingRight = RoadSystem.DrivingSide == DrivingSide.Right;
            trafficSign.transform.position += LaneCount / 2 * trafficSign.transform.right * LaneWidth * (isDrivingRight ? -1 : 1);
            trafficSign.transform.parent = _trafficSignContainer.transform;
            return trafficSign;
        }

        private void SpawnFlowController(RoadNode roadNode, float distanceFromRoadNode, TrafficSignType trafficSignType, bool isForward, GameObject prefab)
        {
            GameObject trafficLightObject = PlaceTrafficSignAtDistance(roadNode, distanceFromRoadNode, trafficSignType, isForward, prefab);
            TrafficLight trafficLight = trafficLightObject.GetComponent<TrafficLight>();
            
            // Add the traffic light to the correct traffic light group, Road1 gets added to trafficLightGroup1 and Road2 gets added to trafficLightGroup2
            if (trafficSignType == TrafficSignType.TrafficLight)
            {
                if (this == roadNode.Intersection.Road1)
                    roadNode.Intersection.TrafficLightController.TrafficLightsGroup1.Add(trafficLight);
                else if (this == roadNode.Intersection.Road2)
                    roadNode.Intersection.TrafficLightController.TrafficLightsGroup2.Add(trafficLight);

                trafficLight.trafficLightController = roadNode.Intersection.TrafficLightController;
            }
            
            roadNode.TrafficLight = trafficLight;
        }
        /// <summary> Returns the speed sign type for the current speed limit </summary>
        private TrafficSignType GetSpeedSignType()
        {
            switch (SpeedLimit)
            {
                case SpeedLimit.TenKPH: return TrafficSignType.SpeedSignTenKPH;
                case SpeedLimit.TwentyKPH: return TrafficSignType.SpeedSignTwentyKPH;
                case SpeedLimit.ThirtyKPH: return TrafficSignType.SpeedSignThirtyKPH;
                case SpeedLimit.FortyKPH: return TrafficSignType.SpeedSignFortyKPH;
                case SpeedLimit.FiftyKPH: return TrafficSignType.SpeedSignFiftyKPH;
                case SpeedLimit.SixtyKPH: return TrafficSignType.SpeedSignSixtyKPH;
                case SpeedLimit.SeventyKPH: return TrafficSignType.SpeedSignSeventyKPH;
                case SpeedLimit.EightyKPH: return TrafficSignType.SpeedSignEightyKPH;
                case SpeedLimit.NinetyKPH: return TrafficSignType.SpeedSignNinetyKPH;
                case SpeedLimit.OneHundredKPH: return TrafficSignType.SpeedSignOneHundredKPH;
                case SpeedLimit.OneHundredTenKPH: return TrafficSignType.SpeedSignOneHundredTenKPH;
                case SpeedLimit.OneHundredTwentyKPH: return TrafficSignType.SpeedSignOneHundredTwentyKPH;
                case SpeedLimit.OneHundredThirtyKPH: return TrafficSignType.SpeedSignOneHundredThirtyKPH;
                default:
                    Debug.LogError("Speed sign type mapping for speed limit " + SpeedLimit + " not found");
                    return TrafficSignType.SpeedSignFiftyKPH;
            }
        }
        /// <summary> Returns the speed sign prefab for the current speed limit </summary>
        private GameObject GetSpeedSignPrefab()
        {
            switch (SpeedLimit)
            {
                case SpeedLimit.TenKPH: return _speedSignTenKPH;
                case SpeedLimit.TwentyKPH: return _speedSignTwentyKPH;
                case SpeedLimit.ThirtyKPH: return _speedSignThirtyKPH;
                case SpeedLimit.FortyKPH: return _speedSignFortyKPH;
                case SpeedLimit.FiftyKPH: return _speedSignFiftyKPH;
                case SpeedLimit.SixtyKPH: return _speedSignSixtyKPH;
                case SpeedLimit.SeventyKPH: return _speedSignSeventyKPH;
                case SpeedLimit.EightyKPH: return _speedSignEightyKPH;
                case SpeedLimit.NinetyKPH: return _speedSignNinetyKPH;
                case SpeedLimit.OneHundredKPH: return _speedSignOneHundredKPH;
                case SpeedLimit.OneHundredTenKPH: return _speedSignOneHundredTenKPH;
                case SpeedLimit.OneHundredTwentyKPH: return _speedSignOneHundredTwentyKPH;
                case SpeedLimit.OneHundredThirtyKPH: return _speedSignOneHundredThirtyKPH;
                default:
                    Debug.LogError("Speed sign prefab mapping for speed limit " + SpeedLimit + " not found");
                    return null;
            }
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
                    roadNodeObject.name = i + " " + curr.Type;

                    if ((curr.Type == RoadNodeType.RoadConnection && i != 0) || (curr.Type == RoadNodeType.End && i != 0))
                        return;
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