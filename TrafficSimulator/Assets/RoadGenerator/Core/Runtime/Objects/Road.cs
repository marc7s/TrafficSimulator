using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator
{
    public enum LaneAmount 
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4
    }

    [RequireComponent(typeof(PathCreator))]
    [RequireComponent(typeof(PathSceneTool))]
	public class Road : MonoBehaviour
	{
        [Header ("Connections")]
        public GameObject road;
        public RoadSystem roadSystem;
        
        [Header ("Road settings")]
        public LaneAmount LaneAmount = LaneAmount.One;
        public float LaneWidth = 4f;
        [Range (0, .5f)] public float Thickness = .15f;
        
        [Header ("Lane settings")]
        [Range(0.1f, 10f)] public float LaneVertexSpacing = 1f;
        public bool DrawLanes = false;
        
        private List<Lane> _lanes = new List<Lane>();
        private GameObject _laneContainer;
        private static string LANE_NAME = "Lane";

        public List<Lane> Lanes
        {
            get => _lanes;
        }
        public void Update()
        {
            PathSceneTool pst = road.GetComponent<PathSceneTool>();
            pst.TriggerUpdate();
        }
        public void UpdateLanes()
        {
            // Get the lane count
            int laneCount = (int)LaneAmount;
            int drivingSide = (int)roadSystem.DrivingSide;

            // Remove all lanes
            _lanes.Clear();

            // Create the lanes
            for(int i = 0; i < laneCount; i++)
            {
                // Get the center path of the road
                BezierPath path = road.GetComponent<PathCreator>().bezierPath;
                
                // Create a primary (same direction as driving side) lane path
                BezierPath primaryLaneBezierPath = path.OffsetInNormalDirection(-drivingSide * LaneWidth * (i + 0.5f), transform, LaneVertexSpacing);
                VertexPath primaryLaneVertexPath = new VertexPath(primaryLaneBezierPath, transform, LaneVertexSpacing);

                // Create a secondary (opposite direction as driving side) lane path that is reversed so that it goes in the opposite direction to the primary lane
                BezierPath secondaryLaneBezierPath = path.OffsetInNormalDirection(drivingSide * LaneWidth * (i + 0.5f), transform, LaneVertexSpacing, true);
                VertexPath secondaryLaneVertexPath = new VertexPath(secondaryLaneBezierPath, transform, LaneVertexSpacing);
                
                // Create the primary and secondary lanes
                Lane primaryLane = new Lane(new LaneType(LaneSide.PRIMARY, i), primaryLaneVertexPath);
                Lane secondaryLane = new Lane(new LaneType(LaneSide.SECONDARY, i), secondaryLaneVertexPath);

                // Add the lanes to the list
                _lanes.Add(primaryLane);
                _lanes.Add(secondaryLane);
            }

            // Clear the lane container
            if(_laneContainer != null)
                DestroyImmediate(_laneContainer);

            // Draw the lines if the setting is enabled
            if(DrawLanes)
            {
                // Create a new lane container
                _laneContainer = new GameObject("Lanes");
                _laneContainer.transform.parent = transform;
                
                // Draw each lane
                for(int i = 0; i < _lanes.Count; i++)
                {
                    DrawLane(this, _lanes[i], GetColor(i), _laneContainer);
                }
            }
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
            lr.positionCount = lane.Path.localPoints.Length;
            lr.SetPositions(lane.Path.localPoints);
        }

        /// <summary>Draws a lane</summary>
        private static void DrawLane(Road road, Lane lane, Color color, GameObject parent)
        {
            if(lane.Path.localPoints.Length < 1) return;
            
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
    }
}