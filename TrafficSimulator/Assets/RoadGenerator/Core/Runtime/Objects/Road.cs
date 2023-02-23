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
        
        [SerializeField][HideInInspector] private RoadNode _start = new RoadNode(Vector3.zero, RoadNodeType.End, 0);
        [SerializeField][HideInInspector] private List<Lane> _lanes = new List<Lane>();
        [SerializeField][HideInInspector] private GameObject _laneContainer;
        [SerializeField][HideInInspector] private VertexPath _path;
        [SerializeField][HideInInspector] private EndOfPathInstruction _endOfPathInstruction = EndOfPathInstruction.Stop;
        [HideInInspector] public List<Intersection> _intersections = new List<Intersection>();
        [SerializeField][HideInInspector] private RoadNavigationGraph _navigationGraph;
        
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

        private void UpdateRoad()
        {
            RoadMeshCreator roadMeshCreator = RoadObject.GetComponent<RoadMeshCreator>();
            if(roadMeshCreator != null)
            {
                UpdateRoadNodes();
                UpdateLanes();
                roadMeshCreator.UpdateMesh();
                RoadSystem.UpdateRoadSystemGraph();
                ShowLanes();
            }
        }
        
        /// <summary>Updates the road nodes</summary>
        public void UpdateRoadNodes()
        {
            // Create the vertex path for the road
            BezierPath path = RoadObject.GetComponent<PathCreator>().bezierPath;
            _path = new VertexPath(path, transform, LaneVertexSpacing);

            // Set the end of path instruction depending on if the path is closed or not
            this._endOfPathInstruction = path.IsClosed ? EndOfPathInstruction.Loop : EndOfPathInstruction.Stop;

            // Create the start node for the road. The start node must be an end node
            this._start = new RoadNode(_path.GetPoint(0), RoadNodeType.End, 0);
            
            // Create a previous and current node that will be used when creating the linked list
            RoadNode prev = null;
            RoadNode curr = _start;

            // Calculating the path distance for each intersection on the road
            List<float> distanceAtIntersection = new List<float>();
            foreach(Intersection intersection in _intersections)
            {
                distanceAtIntersection.Add(_path.GetClosestDistanceAlongPath(intersection.IntersectionPosition));
            }

            // Go through each point in the path of the road
            for(int i = 1; i < _path.NumPoints; i++)
            {
                // Add an intersection node if there is an intersection between the previous node and the current node
                for (int j = 0; j < distanceAtIntersection.Count; j++)
                {
                    // If the path distance at the intersection is between the previous node distance and the current node distance
                    if (distanceAtIntersection[j] > _path.cumulativeLengthAtEachVertex[i-1] && distanceAtIntersection[j] < _path.cumulativeLengthAtEachVertex[i])
                    {
                        Vector3 intersectionPosition = _intersections[j].IntersectionPosition;
                        
                        // TODO: Look into if these distances need to follow the path
                        float distanceToIntersection = Vector3.Distance(curr.Position, intersectionPosition);

                        // Add the intersection node
                        curr.Next = new RoadNode(intersectionPosition, RoadNodeType.FourWayIntersection, curr, null, distanceToIntersection);
                        curr = curr.Next;

                        // If the road does not end at the intersection, update the distance on the following node as well
                        if(curr != null)
                            curr.DistanceToPrevNode = Vector3.Distance(intersectionPosition, curr.Position);
                    }
                }
                
                // The current node type is assumed to be default
                RoadNodeType currentType = RoadNodeType.Default;
                
                // If the current node is the last node in the path, then the current node type is an end node
                if(i == _path.NumPoints - 1)
                {
                    currentType = RoadNodeType.End;
                }

                // Update the previous node and create a new current node
                prev = curr;
                curr = new RoadNode(_path.GetPoint(i), currentType, prev, null, _path.DistanceBetweenPoints(i - 1, i));

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