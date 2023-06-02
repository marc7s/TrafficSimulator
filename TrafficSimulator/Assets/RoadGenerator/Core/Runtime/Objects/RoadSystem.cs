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

    public struct ColorShade
    {
        public int RedMin;
        public int RedMax;
        public int GreenMin;
        public int GreenMax;
        public int BlueMin;
        public int BlueMax;
        public bool GreyScale;

        public ColorShade((int, int) redRange, (int, int) greenRange, (int, int) blueRange, bool greyScale)
        {
            (RedMin, RedMax) = redRange;
            (GreenMin, GreenMax) = greenRange;
            (BlueMin, BlueMax) = blueRange;
            GreyScale = greyScale;
        }

        public Color GetColor()
        {
            int r = UnityEngine.Random.Range(RedMin, RedMax);
            int g = UnityEngine.Random.Range(GreenMin, GreenMax);
            int b = UnityEngine.Random.Range(BlueMin, BlueMax);

            if(GreyScale)
            {
                int avg = (r + g + b) / 3;
                r = avg;
                g = avg;
                b = avg;
            }

            return new Color(
                r / 255f,
                g / 255f,
                b / 255f
            );
        }
    }
    
    [ExecuteInEditMode()]
    [Serializable]
	public class RoadSystem : MonoBehaviour
	{
        [Header("Connections")]
        public GameObject RoadContainer;
        [SerializeField] private GameObject _intersectionContainer;
        public GameObject BuildingContainer;
        public GameObject BusStopContainer;
        public GameObject NatureContainer;
        public Terrain Terrain;
        [SerializeField] private GameObject _roadPrefab;
        [SerializeField] private GameObject _railPrefab;        
        [SerializeField] private GameObject _intersectionPrefab;
        [SerializeField] private MapGenerator _mapGenerator;
        [SerializeField] private GameObject _roadSystemGraphNodePrefab;
        [SerializeField] private GameObject _roadSideParkingPrefab;
        [SerializeField] private GameObject _parkingLotPrefab;

        [Header("Road system settings")]
        public DrivingSide DrivingSide = DrivingSide.Right;
        
        [Header("OSM Settings")]
        public bool ShouldGenerateBuildings = true;
        public bool ShouldGenerateBusStops = true;
        public bool ShouldGenerateRoads = true;
        public bool ShouldGenerateTerrain = true;
        public bool UseOSM = false;
        public bool IsGeneratingOSM = false;
        
        [Header("Default models")]
        public GameObject DefaultTrafficLightPrefab;
        public GameObject DefaultTrafficLightControllerPrefab;
        public GameObject DefaultStopSignPrefab;
        public GameObject DefaultBusStopPrefab;
        public GameObject DefaultTreePrefab;
        public GameObject DefaultYieldSignPrefab;
        public bool ShowGraph = false;
        public bool SpawnRoadsAtOrigin = false;
        [HideInInspector] public const SpeedLimit DefaultSpeedLimit = SpeedLimit.FiftyKPH;
        [SerializeField][HideInInspector] private List<DefaultRoad> _defaultRoads = new List<DefaultRoad>();
        [SerializeField][HideInInspector] private List<TramRail> _tramRails = new List<TramRail>();

        [SerializeField][HideInInspector] private List<Intersection> _intersections = new List<Intersection>();

        [SerializeField][HideInInspector] public List<NavigationNode> RoadSystemGraph;
        [HideInInspector] public GameObject GraphContainer;
        private const float NEW_ROAD_LENGTH = 10f;
        
        private bool _isSetup = false;
        public void AddIntersection(Intersection intersection) => _intersections.Add(intersection);
        public void RemoveIntersection(Intersection intersection) => _intersections.Remove(intersection);
        public void AddRoad(DefaultRoad road) => _defaultRoads.Add(road);
        public void AddRail(TramRail rail) => _tramRails.Add(rail);

        public void RemoveRoad(Road road)
        {
            if (road is DefaultRoad)
                _defaultRoads.Remove(road as DefaultRoad);
            else if (road is TramRail)
                _tramRails.Remove(road as TramRail);
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
                
                if(PositionsAreInRoadSystem(new Vector3[]{ roadStartPoint, roadEndPoint }))
                {
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
            roadObj.transform.parent = RoadContainer.transform;
            
            // Move the road to the spawn point
            PathCreator pathCreator = roadObj.GetComponent<PathCreator>();
            pathCreator.bezierPath = new BezierPath(spawnPoint, width: NEW_ROAD_LENGTH / 2);

            if (pathType == PathType.Road)
            {
                // Get the road from the prefab
                DefaultRoad road = roadObj.GetComponent<DefaultRoad>();
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

        public void GenerateOSMRoads()
        {
            DeleteAllRoads();
            DeleteAllBuildings();
            DeleteAllBusStops();
            DeleteAllNature();
            _mapGenerator.GenerateMap(this);
            ChangeBuildingColors();
        }

        public void SpawnBusStops()
        {
            _mapGenerator.AddBusStops();
        }

        public void DeleteAllRoads()
        {
            List<GameObject> roads = new List<GameObject>();

            foreach(Transform roadT in RoadContainer.transform)
                roads.Add(roadT.gameObject);

            foreach(GameObject road in roads)
                DestroyImmediate(road);

            DefaultRoads.Clear();
        }

        public void DeleteAllBuildings()
        {
            List<GameObject> buildings = new List<GameObject>();

            foreach(Transform buildingT in BuildingContainer.transform)
                buildings.Add(buildingT.gameObject);
            
            foreach(GameObject building in buildings)
                DestroyImmediate(building);
        }

        public void ChangeBuildingColors()
        {
            List<ColorShade> wallColorScheme = new List<ColorShade>();
            List<ColorShade> roofColorScheme = new List<ColorShade>();

            ColorShade whiteGrey = new ColorShade((60, 200), (60, 200), (60, 200), true);
            ColorShade yellow = new ColorShade((255, 255), (180, 200), (1, 73), false);
            ColorShade brown = new ColorShade((80, 160), (60, 120), (30, 40), false);
            ColorShade red = new ColorShade((100, 130), (50, 60), (50, 40), false);

            wallColorScheme.Add(whiteGrey);
            wallColorScheme.Add(brown);
            wallColorScheme.Add(red);

            roofColorScheme.Add(yellow);
            roofColorScheme.Add(red);

            foreach(Transform buildingT in BuildingContainer.transform)
            {
                MaterialPropertyBlock wallProperty = new MaterialPropertyBlock();
                MaterialPropertyBlock roofProperty = new MaterialPropertyBlock();
                
                // Get mesh renderer
                MeshRenderer meshRenderer = buildingT.GetComponent<MeshRenderer>();

                Color wallColor = wallColorScheme[UnityEngine.Random.Range(0, wallColorScheme.Count)].GetColor();
                Color roofColor = roofColorScheme[UnityEngine.Random.Range(0, roofColorScheme.Count)].GetColor();

                wallProperty.SetColor("_Color", wallColor);
                roofProperty.SetColor("_Color", roofColor);
                
                meshRenderer.SetPropertyBlock(wallProperty, 0);
                meshRenderer.SetPropertyBlock(roofProperty, 1);
            }
        }

        public void DeleteAllNature()
        {
            List<GameObject> nature = new List<GameObject>();

            foreach(Transform natureT in NatureContainer.transform)
                nature.Add(natureT.gameObject);

            foreach(GameObject natureObj in nature)
                DestroyImmediate(natureObj);
        }

        public void DeleteAllBusStops()
        {
            List<GameObject> busStops = new List<GameObject>();

            foreach(Transform busStopT in BusStopContainer.transform)
                busStops.Add(busStopT.gameObject);
            
            foreach(GameObject busStop in busStops)
                DestroyImmediate(busStop);
        }

        // Since serialization did not work, this sets up the road system by locating all its roads and intersections
        public void Setup()
        {
            // Making sure this is only called once
            if (_isSetup) 
                return;

            ChangeBuildingColors();

            _isSetup = true;
            
            DefaultRoads.Clear();
            
            // Find roads
            foreach(Transform roadT in RoadContainer.transform)
            {
                Road road = roadT.GetComponent<Road>();
                road.RoadSystem = this;
                
                if (road is DefaultRoad)
                    AddRoad(road as DefaultRoad);
                else if (road is TramRail)
                    AddRail(road as TramRail);
            }

            Intersections.Clear();
            
            // Find intersections
            foreach(Transform intersectionT in _intersectionContainer.transform)
            {
                Intersection intersection = intersectionT.GetComponent<Intersection>();
                intersection.RoadSystem = this;
                
                AddIntersection(intersection);
            }

            foreach(DefaultRoad road in _defaultRoads)
                road.OnChange();

            // Find the graph container
            GraphContainer = gameObject.transform.Find("Graph")?.gameObject;
            
            // Update the road system graph
            UpdateRoadSystemGraph();
        }

        /// <summary> Setup the POIs for all roads in the road system </summary>
        public void UpdatePOIs()
        {
            foreach(DefaultRoad road in _defaultRoads)
                road.UpdateRoadNodes();
        }

        public Intersection AddNewIntersection(IntersectionPointData intersectionPointData)
        {
            GameObject intersectionObject = Instantiate(_intersectionPrefab, intersectionPointData.Position, Quaternion.identity);
            intersectionObject.name = "Intersection" + IntersectionCount;
            intersectionObject.transform.parent = _intersectionContainer.transform;

            Intersection intersection = intersectionObject.GetComponent<Intersection>();
            intersection.ID = System.Guid.NewGuid().ToString();
            intersection.Type = intersectionPointData.Type;
            intersection.IntersectionObject = intersectionObject;
            intersection.RoadSystem = this;
            intersection.IntersectionPosition = intersectionPointData.Position;

            foreach (JunctionEdgeData junctionEdgeData in intersectionPointData.JunctionEdgeDatas)
                intersection.IntersectionArms.Add(new IntersectionArm(junctionEdgeData));
            
            intersection.SetupIntersectionArms();

            foreach (Road road in intersection.GetIntersectionRoads())
                road.AddIntersection(intersection);

            // Finding roads in the opposide directions to measure the intersection length
            foreach (IntersectionArm arm in intersection.IntersectionArms)
            {
                if (arm != intersection.IntersectionArms[0] && arm.OppositeArmID != intersection.IntersectionArms[0].ID)
                    intersection.IntersectionLength = Intersection.CalculateIntersectionLength(intersection.IntersectionArms[0].Road, arm.Road);
            }

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
            
            foreach (DefaultRoad road in _defaultRoads)
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
            
            if(GraphContainer != null)
                DestroyImmediate(GraphContainer);

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
        public List<DefaultRoad> DefaultRoads 
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

        public GameObject RoadSideParkingPrefab
        {
            get => _roadSideParkingPrefab;
        }

        public GameObject ParkingLotPrefab
        {
            get => _parkingLotPrefab;
        }

        void OnDestroy()
        {
            // Need to disable showing the graph as the road system is being destroyed
            // Otherwise the a graph will be created in the same frame as the road system is destroyed and will cause an error
            ShowGraph = false;
        }
    }
}