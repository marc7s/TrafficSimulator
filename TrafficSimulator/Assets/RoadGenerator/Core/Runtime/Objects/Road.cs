using System.Collections.Generic;
using UnityEngine;

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
    [RequireComponent(typeof(PathSceneTool))]
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
        private bool started = true;
        
        private RoadNode _start = new RoadNode(Vector3.zero, RoadNodeType.End);
        private List<Lane> _lanes = new List<Lane>();
        private GameObject _laneContainer;
        private VertexPath _path;

        private EndOfPathInstruction _endOfPathInstruction = EndOfPathInstruction.Stop;
        [HideInInspector]
        
        public List<Intersection> Intersections = new List<Intersection>();
        
        private const string LANE_NAME = "Lane";
        private const string LANE_CONTAINER_NAME = "Lanes";

        /// <summary>Function that runs once when the road is updated the first time</summary>
        void OnStart()
        {
            // Look for an existing lane container
            foreach(Transform t in RoadObject.transform)
            {
                if(t.name == LANE_CONTAINER_NAME)
                {
                    _laneContainer = t.gameObject;
                }
            }
        }

        /// <summary>Used to trigger an update of the road through the PathSceneTool</summary>
        public void Update()
        {
            PathSceneTool pathSceneTool = RoadObject.GetComponent<PathSceneTool>();
            pathSceneTool.TriggerUpdate();
        }
        
        /// <summary>DO NOT USE. Used by the mesh generation to trigger an update of the road</summary>
        public void DoNotUse_TriggerUpdate()
        {
            // Run the OnStart method once
            if(started)
            {
                OnStart();
                started = false;
            }

            UpdateRoadNodes();
            UpdateLanes();
        }
        
        /// <summary>Updates the road nodes</summary>
        private void UpdateRoadNodes()
        {
            // Create the vertex path for the road
            BezierPath path = RoadObject.GetComponent<PathCreator>().bezierPath;
            _path = new VertexPath(path, transform, LaneVertexSpacing);

            // Set the end of path instruction depending on if the path is closed or not
            this._endOfPathInstruction = path.IsClosed ? EndOfPathInstruction.Loop : EndOfPathInstruction.Stop;

            // Create the start node for the road. The start node must be an end node
            this._start = new RoadNode(_path.GetPoint(0), RoadNodeType.End);
            
            // Create a previous and current node that will be used when creating the linked list
            RoadNode prev = null;
            RoadNode curr = _start;

            // Go through each point in the path of the road
            for(int i = 1; i < _path.NumPoints; i++)
            {
                // The current node type is assumed to be default
                RoadNodeType currentType = RoadNodeType.Default;
                
                // If the current node is the last node in the path, then the current node type is an end node
                if(i == _path.NumPoints - 1)
                {
                    currentType = RoadNodeType.End;
                }

                // Update the previous node and create a new current node
                prev = curr;
                curr = new RoadNode(_path.GetPoint(i), currentType, prev, null);

                // Set the next pointer for the previous node
                prev.Next = curr;
            }
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

            // Destroy the lane container, and with it all the previous lanes
            if(_laneContainer != null)
            {
                DestroyImmediate(_laneContainer);
            }

            // Draw the lines if the setting is enabled
            if(DrawLanes)
            {
                // Create a new lane container
                _laneContainer = new GameObject(LANE_CONTAINER_NAME);
                _laneContainer.transform.parent = transform;
                
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
            lr.positionCount = lane.Start.Count;
            lr.SetPositions(lane.Start.GetPositions());
        }

        /// <summary>Draws a lane</summary>
        private static void DrawLane(Road road, Lane lane, Color color, GameObject parent)
        {
            if(lane.Start.Count < 1) return;
            
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
        public RoadNode Start
        {
            get => _start;
        }
        public EndOfPathInstruction EndOfPathInstruction
        {
            get => _endOfPathInstruction;
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
        }
    }
}