using UnityEngine;
using System.Collections.Generic;
using System;

namespace RoadGenerator
{
    public enum DrivingSide
    {
        Left = 1,
        Right = -1
    }
    
    [ExecuteInEditMode()]
    [Serializable]
	public class RoadSystem : MonoBehaviour
	{
        [Header("Connections")]       
        [SerializeField] private GameObject _roadContainer;
        [SerializeField] private GameObject _intersectionContainer;
        [SerializeField] private GameObject _roadPrefab;
        [SerializeField] private GameObject _intersectionPrefab;

        [SerializeField] private GameObject _roadSystemGraphNodePrefab;

        [Header("Road system settings")]
        public DrivingSide DrivingSide = DrivingSide.Right;
        
        [Header("Default models")]
        [SerializeField] public GameObject _defaultTrafficLightPrefab;

        public bool ShowGraph = false;
        public bool SpawnRoadsAtOrigin = false;

        [SerializeField][HideInInspector] private List<Road> _roads = new List<Road>();

        [SerializeField][HideInInspector] private List<Intersection> _intersections = new List<Intersection>();

        [SerializeField][HideInInspector] private Dictionary<string, GraphNode> _roadSystemGraph;
        [HideInInspector] public GameObject GraphContainer;

        public void AddIntersection(Intersection intersection) => _intersections.Add(intersection);
        public void RemoveIntersection(Intersection intersection) => _intersections.Remove(intersection);
        public void AddRoad(Road road) => _roads.Add(road);

        public void RemoveRoad(Road road) => _roads.Remove(road);
        public void AddNewRoad()
        {
            Vector3 spawnPoint = Vector3.zero;
            #if UNITY_EDITOR
            if(!SpawnRoadsAtOrigin)
            {
                RaycastHit hit;
                UnityEditor.SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
                Camera camera = sceneView.camera;
                
                // Get the nearest point on the surface the camera is looking at
                if(!camera || !Physics.Raycast(camera.transform.position, camera.transform.forward, out hit))
                {
                    Debug.LogError("No surface found in line of sight to spawn road. Make sure the surface you are looking at has a collider");
                    return;
                }
                spawnPoint = hit.point;
            }
            #endif
            

            // Instantiate a new road prefab
            GameObject roadObj = Instantiate(_roadPrefab, Vector3.zero, Quaternion.identity);
            
            // Set the name of the road
            roadObj.name = "Road" + RoadCount;
            
            // Set the road as a child of the road container
            roadObj.transform.parent = _roadContainer.transform;
            
            // Get the road from the prefab
            Road road = roadObj.GetComponent<Road>();

            // Move the road to the spawn point
            PathCreator pathCreator = roadObj.GetComponent<PathCreator>();
            pathCreator.bezierPath = new BezierPath(spawnPoint);
            
            // Set the road pointers
            road.RoadObject = roadObj;
            road.RoadSystem = this;
            
            // Update the road to display it
            road.OnChange();

            AddRoad(road);
        }

        // Since serialization did not work, this sets up the road system by locating all its roads and intersections
        public void Setup()
        {
            // Find roads
            foreach(Transform roadT in _roadContainer.transform)
            {
                Road road = roadT.GetComponent<Road>();
                road.RoadSystem = this;
                
                AddRoad(road);
            }

            // Find intersections
            foreach(Transform intersectionT in _intersectionContainer.transform)
            {
                Intersection intersection = intersectionT.GetComponent<Intersection>();
                intersection.RoadSystem = this;
                
                AddIntersection(intersection);
            }

            // Find the graph container
            GraphContainer = GameObject.Find("Graph");
        }

        public Intersection AddNewIntersection(IntersectionPointData intersectionPointData, Road road1, Road road2)
        {
            GameObject intersectionObject = Instantiate(_intersectionPrefab, intersectionPointData.Position, intersectionPointData.Rotation);
            intersectionObject.name = "Intersection" + IntersectionCount;
            intersectionObject.transform.parent = _intersectionContainer.transform;
            
            Intersection intersection = intersectionObject.GetComponent<Intersection>();
            intersection.ID = System.Guid.NewGuid().ToString();
            intersection.Type = intersectionPointData.Type;
            intersection.IntersectionObject = intersectionObject;
            intersection.RoadSystem = this;
            intersection.IntersectionPosition = intersectionPointData.Position;
            intersection.Road1PathCreator = intersectionPointData.Road1PathCreator;
            intersection.Road2PathCreator = intersectionPointData.Road2PathCreator;
            intersection.Road1 = road1;
            intersection.Road2 = road2;
            intersection.Road1AnchorPoint1 = intersectionPointData.Road1AnchorPoint1;
            intersection.Road1AnchorPoint2 = intersectionPointData.Road1AnchorPoint2;
            intersection.Road2AnchorPoint1 = intersectionPointData.Road2AnchorPoint1;
            intersection.Road2AnchorPoint2 = intersectionPointData.Road2AnchorPoint2;
            
            road1.AddIntersection(intersection);
            road2.AddIntersection(intersection);
            
            AddIntersection(intersection);
            
            return intersection;
        }
        /// <summary> Checks if an intersection already exists at the given position </summary>
        public bool DoesIntersectionExist(Vector3 position)
        {
            foreach (Intersection intersection in _intersections)
            {
                if (Vector3.Distance(position, intersection.IntersectionPosition) < Intersection.IntersectionLength)
                {
                    return true;
                }
            }
            return false;
        }
        public void UpdateRoadSystemGraph()
        {
            // Clear the graph
            ClearRoadGraph();
            
            foreach (Road road in _roads)
            {
                // This needs to be called because after script update the scene reloads and the roads don't save their graph correctly
                // This can be removed if roads serialize the graph correctly
                road.UpdateRoadNodes();
            }

            // Generate a new graph
            _roadSystemGraph = RoadSystemNavigationGraph.GenerateRoadSystemNavigationGraph(this);

            // Display the graph if the setting is active
            if (ShowGraph) {
                // Create a new empty graph
                CreateEmptyRoadGraph();
                RoadSystemNavigationGraph.DrawGraph(this, _roadSystemGraph, _roadSystemGraphNodePrefab);
            }
                
        }

        public void UpdateRoads()
        {
            foreach(Road road in _roads)
            {
                road.OnChange();
            }
        }

        private void ClearRoadGraph() {
            _roadSystemGraph = null;
            if(GraphContainer != null) {
                DestroyImmediate(GraphContainer);
            }
            GraphContainer = null;
        }

        private void CreateEmptyRoadGraph() {
            GraphContainer = new GameObject("Graph");
            GraphContainer.transform.parent = transform;
        }
        public int IntersectionCount 
        {
            get => _intersections.Count;
        }
        /// <summary>Returns the number of roads in the road system</summary>
        public int RoadCount 
        {
            get => _roads.Count;
        }
        public List<Road> Roads 
        {
            get => _roads;
        }
        void OnDestroy()
        {
            // Need to disable showing the graph as the road system is being destroyed
            // Otherwise the a graph will be created in the same frame as the road system is destroyed and will cause an error
            ShowGraph = false;
        }
    }
}