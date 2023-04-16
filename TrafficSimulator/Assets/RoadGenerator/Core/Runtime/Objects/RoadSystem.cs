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
        [SerializeField] private GameObject _railPrefab;        
        [SerializeField] private GameObject _intersectionPrefab;

        [SerializeField] private GameObject _roadSystemGraphNodePrefab;

        [Header("Road system settings")]
        public DrivingSide DrivingSide = DrivingSide.Right;
        
        [Header("Default models")]
        public GameObject DefaultTrafficLightPrefab;
        public GameObject DefaultTrafficLightControllerPrefab;
        public GameObject DefaultStopSignPrefab;
        public bool ShowGraph = false;
        public bool SpawnRoadsAtOrigin = false;
        [HideInInspector] public const SpeedLimit DefaultSpeedLimit = SpeedLimit.FiftyKPH;
        [SerializeField][HideInInspector] private List<Road> _defaultRoads = new List<Road>();
        [SerializeField][HideInInspector] private List<TramRail> _tramRails = new List<TramRail>();

        [SerializeField][HideInInspector] private List<Intersection> _intersections = new List<Intersection>();

        [SerializeField][HideInInspector] public List<NavigationNode> RoadSystemGraph;
        [HideInInspector] public GameObject GraphContainer;
        private const float NEW_ROAD_LENGTH = 10f;
        
        private bool _isSetup = false;
        public void AddIntersection(Intersection intersection) => _intersections.Add(intersection);
        public void RemoveIntersection(Intersection intersection) => _intersections.Remove(intersection);
        public void AddRoad(Road road) => _defaultRoads.Add(road);
        public void AddRail(TramRail rail) => _tramRails.Add(rail);

        public void RemoveRoad(Road road)
        {
            if (road is DefaultRoad)
            {
                _defaultRoads.Remove(road as DefaultRoad);
            }
            else if (road is TramRail)
            {
                _tramRails.Remove(road as TramRail);
            }
        }
        public void AddNewRoad(PathType pathType)
        {
            Vector3 spawnPoint = Vector3.zero;
#if UNITY_EDITOR
            if(!SpawnRoadsAtOrigin)
            {
                int layerMask = LayerMask.GetMask("RoadSystem");
                RaycastHit hit;
                UnityEditor.SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
                Camera camera = sceneView.camera;

                // Get the nearest point on the surface the camera is looking at, ignoring the RoadSystem layer
                if(!camera || !Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, Mathf.Infinity, ~layerMask))
                {
                    Debug.LogError("No surface found in line of sight to spawn road. Make sure the surface you are looking at has a collider");
                    return;
                }
                spawnPoint = hit.point;
                
                Vector3 roadStartPoint = spawnPoint + Vector3.left * NEW_ROAD_LENGTH / 2;
                Vector3 roadEndPoint = roadStartPoint + Vector3.right * NEW_ROAD_LENGTH;
                
                if(PositionsAreInRoadSystem(new Vector3[]{ roadStartPoint, roadEndPoint })){
                    Debug.LogError($"Cannot spawn a road at {hit.point}, there is already a road there");
                    return;
                }
            }
#endif
            

            // Instantiate a new road prefab
            GameObject roadObj = Instantiate(GetPathPrefab(pathType), Vector3.zero, Quaternion.identity);
            
            // Set the name of the road
            roadObj.name = GetPathName(pathType) + (pathType == PathType.Road ? RoadCount : TramRailCount);
            
            // Set the road as a child of the road container
            roadObj.transform.parent = _roadContainer.transform;
            
            // Move the road to the spawn point
            PathCreator pathCreator = roadObj.GetComponent<PathCreator>();
            pathCreator.bezierPath = new BezierPath(spawnPoint, width: NEW_ROAD_LENGTH / 2);

            if (pathType == PathType.Road)
            {
                // Get the road from the prefab
                Road road = roadObj.GetComponent<Road>();
                // Set the road pointers
                road.RoadObject = roadObj;
                road.RoadSystem = this;
                
                // Update the road to display it
                road.OnChange();

                AddRoad(road);
            }
            else if (pathType == PathType.Rail)
            {
                // Get the rail from the prefab
                TramRail rail = roadObj.GetComponent<TramRail>();
                rail.RoadObject = roadObj;
                rail.RoadSystem = this;
                
                // Update the rail to display it
                rail.OnChange();

                AddRail(rail);
            }

        }

        private GameObject GetPathPrefab(PathType pathType)
        {
            switch(pathType)
            {
                case PathType.Road: return _roadPrefab;
                case PathType.Rail: return _railPrefab;
                default: return null;
            }
        }

        private string GetPathName(PathType pathType)
        {
            switch(pathType)
            {
                case PathType.Road: return "Road";
                case PathType.Rail: return "Rail";
                default: return null;
            }
        }

        private bool PositionsAreInRoadSystem(Vector3[] positions)
        {
            bool collides = false;
            // The distance up and down to check for road collisions. 
            // We do not want this set too big as there might be roads above or below that do not interfere in the case of bridges and tunnels
            const float collisionDetectionDistance = 10f;
            int layerMask = LayerMask.GetMask("RoadSystem");
            foreach(Vector3 pos in positions)
            {
                collides = collides || Physics.Raycast(pos, Vector3.up, out RaycastHit upHit, collisionDetectionDistance, layerMask);
                collides = collides || Physics.Raycast(pos, Vector3.down, out RaycastHit downHit, collisionDetectionDistance, layerMask);
            }
            return collides;
        }

        // Since serialization did not work, this sets up the road system by locating all its roads and intersections
        public void Setup()
        {
            // Making sure this is only called once
            if (_isSetup) 
                return;
            
            _isSetup = true;
            
            // Find roads
            foreach(Transform roadT in _roadContainer.transform)
            {
                Road road = roadT.GetComponent<Road>();
                road.RoadSystem = this;
                
                if (road is DefaultRoad)
                {
                    AddRoad(road);
                }
                else if (road is TramRail)
                {
                    AddRail(road as TramRail);
                }
            }

            // Find intersections
            foreach(Transform intersectionT in _intersectionContainer.transform)
            {
                Intersection intersection = intersectionT.GetComponent<Intersection>();
                intersection.RoadSystem = this;
                
                AddIntersection(intersection);
            }

            foreach (Road road in _defaultRoads)
                road.OnChange();

            // Find the graph container
            GraphContainer = gameObject.transform.Find("Graph")?.gameObject;
            
            // Update the road system graph
            UpdateRoadSystemGraph();
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
            intersection.IntersectionLength = Intersection.CalculateIntersectionLength(road1, road2);
            
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
                if (Vector3.Distance(position, intersection.IntersectionPosition) < intersection.IntersectionLength)
                    return true;
            }
            return false;
        }
        void Start()
        {
            Setup();
        }
        public void UpdateRoadSystemGraph()
        {
            // Clear the graph
            ClearRoadGraph();
            
            foreach (Road road in _defaultRoads)
            {
                // This needs to be called because after script update the scene reloads and the roads don't save their graph correctly
                // This can be removed if roads serialize the graph correctly
               road.UpdateRoadNodes();
               road.UpdateLanes();
            }

            // Generate a new graph
            RoadSystemGraph = RoadSystemNavigationGraph.GenerateRoadSystemNavigationGraph(this);

            // Display the graph if the setting is active
            if (ShowGraph)
            {
                // Create a new empty graph
                CreateEmptyRoadGraph();
                RoadSystemNavigationGraph.DrawGraph(this, RoadSystemGraph, _roadSystemGraphNodePrefab);
            }
        }

        public void UpdateRoads()
        {
            foreach(Road road in _defaultRoads)
                road.OnChange();
        }

        private void ClearRoadGraph() {
            RoadSystemGraph = null;
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
            get => _defaultRoads.Count;
        }

        public int TramRailCount
        {
            get => _tramRails.Count;
        }
        public List<Road> DefaultRoads 
        {
            get => _defaultRoads;
        }

        public List<TramRail> TramRails
        {
            get => _tramRails;
        }
        public List<Intersection> Intersections 
        {
            get => _intersections;
        }

        void OnDestroy()
        {
            // Need to disable showing the graph as the road system is being destroyed
            // Otherwise the a graph will be created in the same frame as the road system is destroyed and will cause an error
            ShowGraph = false;
        }
    }
}