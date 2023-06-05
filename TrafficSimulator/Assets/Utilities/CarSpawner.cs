using System.Collections.Generic;
using UnityEngine;
using VehicleBrain;
using System.Linq;
using System;
using UI;

namespace RoadGenerator
{
    public enum SpawnMode
    {
        Total,
        LaneRatio
    }

    public enum CarSpawnerNavigationMode
    {
        FollowPrefabs,
        Random,
        RandomPath,
        Path
    }

    public enum CarSpawnerMaxSpeed
    {
        FollowPrefabs,
        LimitMaxSpeed
    }

    public enum CarSpawnerBaseTLD
    {
        FollowPrefabs,
        OverrideBaseTLD
    }

    public enum StartMode
    {
        UI,
        Delay
    }

    public class CarSpawner : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private GameObject _sedanPrefab;
        [SerializeField] private GameObject _sportsCar1Prefab;
        [SerializeField] private GameObject _sportsCar2Prefab;
        [SerializeField] private GameObject _suv1Prefab;
        [SerializeField] private GameObject _suv2Prefab;
        [SerializeField] private GameObject _van1Prefab;
        [SerializeField] private GameObject _van2Prefab;

        [SerializeField] private GameObject _roadSystemObject;

        [Header("Vehicle Settings")]
        [SerializeField] private DrivingMode _drivingMode = DrivingMode.Quality;
        [SerializeField] private bool _randomVehicleTypes = true;
        [SerializeField] private CarSpawnerNavigationMode _navigationMode = CarSpawnerNavigationMode.FollowPrefabs;
        
        [SerializeField] private CarSpawnerMaxSpeed _maxSpeedMode = CarSpawnerMaxSpeed.FollowPrefabs;
        [SerializeField] private float _maxSpeed = 7f;
        
        [SerializeField] private CarSpawnerBaseTLD _overrideBaseTLD = CarSpawnerBaseTLD.FollowPrefabs;
        [SerializeField] private float _baseTLD = 10f;
        
        [Header("Spawn Settings")]
        [SerializeField] private SpawnMode _mode = SpawnMode.Total;
        
        // Total number of cars to spawn in mode Total
        public int TotalCars = 5;
        
        // Percentage of cars to spawn per lane in mode LaneRatio
        [Range(0, 1)] public float LaneCarRatio = 0.5f;

        [Header("Debug Settings")]
        [SerializeField] private ShowTargetLines _showTargetLines = ShowTargetLines.None;
        [SerializeField] private bool _logBrakeReason = false;
        [SerializeField] private bool _showNavigationPath = false;

        private OverlayController _overlayController;
        private MenuController _menuController;
        
        // Delay before spawning cars
        [Range(1, 10)] public float SpawnDelay = 1f;

        private RoadSystem _roadSystem;
        private List<DefaultRoad> _roads;

        private List<GameObject> _vehicleTypes;

        private List<Lane> _lanes = new List<Lane>(); 
        private List<int> _indexes = new List<int>();

        private LaneNode _laneNodeCurrent;

        private GameObject _currentCar;

        private int _carCounter = 0;
        private float _carLength;

        private bool _spawned = false;
        private StartMode _startMode = StartMode.UI;

        private List<CarsForLane> _spawnableLanes = new List<CarsForLane>();

        // Create an empty Gameobject to hold all vehicles
        private GameObject _carContainer;
        private float _startTime = 0;
        private bool _uiStart = false;

        public struct SpawnableLaneNode
        {
            public float Distance;
            public LaneNode Node;

            public SpawnableLaneNode(float distance, LaneNode node)
            {
                Distance = distance;
                Node = node;
            }
        }

        public class CarsForLane
        {
            private Lane _lane;
            public int CapacityLeft;
            private List<SpawnableLaneNode> _spawnableLaneNodes;

            public Lane Lane { get => _lane; }
            public List<SpawnableLaneNode> SpawnableLaneNodes { get => _spawnableLaneNodes; }
            public int Capacity { get => _spawnableLaneNodes.Count; }

            public CarsForLane(Lane lane, List<SpawnableLaneNode> spawnableLaneNodes)
            {
                _lane = lane;
                _spawnableLaneNodes = spawnableLaneNodes;
                CapacityLeft = Capacity;
            }
        }

        void Awake()
        {
            GameObject carContainer = GameObject.Find("CarContainer");
            
            if (carContainer == null)
                _carContainer = new GameObject("CarContainer");
            else
                _carContainer = carContainer;
            
            _carContainer.transform.parent = transform;

            // Get Overlay Controller
            _overlayController = FindObjectOfType<OverlayController>();

            // Get Overlay Controller
            _menuController = FindObjectOfType<MenuController>();

            // Set the start mode depending on if the UI start menu is available
            if (_overlayController != null && _menuController != null)
            {
                // Subscribe to start event
                _menuController.OnSimulationStart += SimulationStartHandler;
                _overlayController.OnSimulationStop += SimulationStopHandler;

                _startMode = StartMode.UI;
            }
            else
            {
                _startMode = StartMode.Delay;
            }
        }

        void Update()
        {
            if(_spawned || (_startMode == StartMode.UI && !_uiStart))
                return;
            
            if (Time.time - _startTime > SpawnDelay)
                Run();
        }

        private void SimulationStartHandler()
        {
            MenuStart(_overlayController.CarsToSpawn);
        }

        private void SimulationStopHandler()
        {
            RemoveCars();
        }


        private void Setup()
        {
            _vehicleTypes = _randomVehicleTypes ? new List<GameObject>(){ _sedanPrefab, _sportsCar1Prefab, _sportsCar2Prefab, _suv1Prefab, _suv2Prefab, _van1Prefab, _van2Prefab } : new List<GameObject>(){ _sedanPrefab };

            _carLength = GetLongestCarLength(_vehicleTypes) + 1f;

            _roadSystem = _roadSystemObject.GetComponent<RoadSystem>();

            _roads = new List<DefaultRoad>(_roadSystem.DefaultRoads);

            AddLanesToList();
            CalculateLaneIndexes();
            CalculateMaxCarsForLanes();
        }

        private DrivingMode GetMenuDrivingMode()
        {
            switch(PlayerPrefs.GetString("SimulationMode"))
            {
                case "Quality":
                    return DrivingMode.Quality;
                case "Performance":
                    return DrivingMode.Performance;
                default:
                    return DrivingMode.Quality;
            }
        }

        private void MenuStart(int totalCars)
        {
            TotalCars = totalCars;
            _mode = SpawnMode.Total;
            _drivingMode = GetMenuDrivingMode();
            _startTime = Time.time;
            _uiStart = true;
        }

        private void RemoveCars()
        {
            if (_carContainer.transform.childCount > 0)
            {
                foreach (Transform child in _carContainer.transform)
                    Destroy(child.gameObject);
                
                _spawned = false;
            }
        }

        public void Run()
        {
            if (!_spawned)
            {
                Setup();
                _spawned = true;
                SpawnCars();
            }
            else
            {
                Debug.Log("Cars already spawned");
            }
        }
        
        private float GetLongestCarLength(List<GameObject> cars)
        {
            float longestCarLength = 0;
            foreach (GameObject car in cars)
            {
                float carLength = car.transform.GetChild(1).GetComponent<MeshRenderer>().bounds.size.z;
                if (carLength > longestCarLength)
                    longestCarLength = carLength;
            }
            return longestCarLength * 1.25f;
        }

        private void AddLanesToList()
        {
            foreach (DefaultRoad road in _roadSystem.DefaultRoads)
            {
                foreach (Lane lane in road.Lanes)
                    _lanes.Add(lane);
            }
        }

        /// <summary>Saves the index of each lane in a list</summary>
        private void CalculateLaneIndexes()
        {
            // Loop through all roads
            foreach (DefaultRoad road in _roadSystem.DefaultRoads)
            {
                // Loop through all lanes
                for (int i = 0; i < road.LaneCount; i++)
                    _indexes.Add(i);
            }
        }

        /// <summary>Calculate the maximum amount of vehicles per lane </summary>
        private void CalculateMaxCarsForLanes()
        {
            _spawnableLanes.Clear();

            int maxCars = 0;
            for (int i = 0; i < _lanes.Count; i++)
            {
                List<SpawnableLaneNode> carsInLane = GetSpawnableLaneNodesInLane(_lanes[i]);
                _spawnableLanes.Add(new CarsForLane(_lanes[i], carsInLane));
                
                maxCars += carsInLane.Count;
            }
        }

        /// <summary>Remove 1 capacity from each lane until there are no cars left or no lanes left </summary>
        private List<CarsForLane> T_CalculateCarsPerLane(int carCount)
        {
            List<CarsForLane> availableLanes = new List<CarsForLane>(_spawnableLanes);
            List<CarsForLane> fullLanes = new List<CarsForLane>();

            while (carCount >= 1 && availableLanes.Count > 0)
            {
                for (int i = availableLanes.Count - 1; i >= 0; i--)
                {
                    if(carCount < 1)
                        break;

                    if (availableLanes[i].CapacityLeft > 0)
                    {
                        availableLanes[i].CapacityLeft--;
                        carCount--;
                    }
                    else
                    {
                        fullLanes.Add(availableLanes[i]);
                        availableLanes.RemoveAt(i);
                    }
                }
            }

            List<CarsForLane> allLanes = new List<CarsForLane>(availableLanes);
            allLanes.AddRange(fullLanes);

            return allLanes;
        }

        /// <summary>Calculate the amount of cars per lane by ratio </summary>
        private List<CarsForLane> R_CalculateCarsPerLane()
        {
            int carCount = 0;
            List<CarsForLane> availableLanes = new List<CarsForLane>(_spawnableLanes);

            foreach (CarsForLane lane in availableLanes)
                carCount += Mathf.CeilToInt(lane.Capacity * LaneCarRatio);
            
            return T_CalculateCarsPerLane(carCount);
        }

        /// <summary>Find all acceptable spawn nodes in a lane </summary>
        private List<SpawnableLaneNode> GetSpawnableLaneNodesInLane(Lane lane)
        {
            float distance = 0;
            float offset = 16f;

            List<SpawnableLaneNode> spawnableNodes = new List<SpawnableLaneNode>();

            // Return if the lane is too short to spawn a car
            if (lane.GetLaneLengthNoIntersections() < (_carLength + offset))
                return spawnableNodes;

            LaneNode curr = lane.StartNode.Next;

            while (curr != null)
            {
                if(IsCarSpawnable(curr))
                {
                    distance += curr.DistanceToPrevNode;
                    spawnableNodes.Add(new SpawnableLaneNode(distance, curr));
                    curr = CalculateNextNode(curr, _carLength / 2 + offset);
                }
                else
                {
                    distance += curr.DistanceToPrevNode;
                    curr = curr.Next;
                }
            }
            
            return spawnableNodes;
        }

        private void SpawnCars()
        {
            int carsToSpawn = TotalCars;

            // Return if there are no cars to spawn
            if (carsToSpawn == 0 && _mode == SpawnMode.Total)
                return;

            // Calculate cars to spawn per lane
            List<CarsForLane> allLanes = _mode == SpawnMode.Total ? T_CalculateCarsPerLane(carsToSpawn) : R_CalculateCarsPerLane();

            // Loop through all lanes and spawn their cars
            foreach (CarsForLane lane in allLanes)
            {
                for (int i = 0; i < lane.Capacity - lane.CapacityLeft; i++)
                {
                    if(_carCounter >= TotalCars)
                        return;
                    
                    _laneNodeCurrent = lane.SpawnableLaneNodes[i].Node;
                    
                    SpawnCar(0);
                    _carCounter++;
                }
            }

            Debug.Log($"Spawned {_carCounter}{(_mode == SpawnMode.Total ? $"/{TotalCars}" : "")} cars");
        }

        /// <summary>Checks multiple conditions to determine if a car is able to spawn on node</summary>
        private bool IsCarSpawnable(LaneNode node)
        {
            if(node == null)
                return false;

            bool isThreeWayIntersection = node.RoadNode.Type == RoadNodeType.End && (node.Next?.IsIntersection() == true || node.Prev?.IsIntersection() == true);

            // Check a few nodes back so that there is no vehicle close behind
            float distance = 0;
            LaneNode curr = node;
            while(curr != null && distance <= _carLength)
            {
                if(curr.HasVehicle())
                    return false;
                
                distance += curr.DistanceToPrevNode;
                curr = curr.Prev;
            }

            // Check if the node is an intersection, a junction edge, is null, is the last node in the lane or is a navigation node
            return !(node.RoadNode.IsIntersection() || node.RoadNode.Type == RoadNodeType.JunctionEdge || node == null || node.Next == null) && !isThreeWayIntersection && !node.RoadNode.IsNavigationNode;
        }

        /// <summary>Spawns a car at the current lane node</summary>
        private void SpawnCar(int index)
        {
            GameObject car = GetRandomCar(_vehicleTypes);
            _currentCar = Instantiate(car, _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);

            // Add the car to the container
            _currentCar.transform.parent = _carContainer.transform;

            AutoDrive autoDrive = _currentCar.GetComponent<AutoDrive>();
            autoDrive.StartingRoad = _lanes[index].Road;
            autoDrive.LaneIndex = _indexes[index];

            // If a custom car is being used as a spawn prefab it should be deactivated to not interfere, so activate this car
            _currentCar.SetActive(true);

            autoDrive.CustomStartNode = _laneNodeCurrent;

            autoDrive.Mode = _drivingMode;
            autoDrive.ShowTargetLines = _showTargetLines;
            autoDrive.LogBrakeReason = _logBrakeReason;
            autoDrive.ShowNavigationPath = _showNavigationPath;

            if(_overrideBaseTLD == CarSpawnerBaseTLD.OverrideBaseTLD)
                autoDrive.BaseTLD = _baseTLD;
            
            // Overwrite the navigation mode if a custom one is set
            switch(_navigationMode)
            {
                case CarSpawnerNavigationMode.Random:
                    autoDrive.OriginalNavigationMode = NavigationMode.Random;
                    break;
                case CarSpawnerNavigationMode.RandomPath:
                    autoDrive.OriginalNavigationMode = NavigationMode.RandomNavigationPath;
                    break;
                case CarSpawnerNavigationMode.Path:
                    autoDrive.OriginalNavigationMode = NavigationMode.Path;
                    break;
            }

            autoDrive.Setup();

            // Set the name of the GameObject
            _currentCar.name = $"{car.name} | {autoDrive.LicensePlate}";

            if(_maxSpeedMode == CarSpawnerMaxSpeed.LimitMaxSpeed)
                autoDrive.SetMaxSpeed(_maxSpeed);
        }

        /// <summary>Calculates the next node in a lane based on the target length</summary>
        private LaneNode CalculateNextNode(LaneNode startNode, float targetLength)
        {
            LaneNode curr = startNode.Next;
            float currentLength = 0;
            
            while(curr != null && (curr.Type == RoadNodeType.JunctionEdge || curr.IsIntersection() || currentLength < targetLength))
            {
                currentLength += curr.DistanceToPrevNode;
                curr = curr.Next;
            }

            return curr;
        }

        private GameObject GetRandomCar(List<GameObject> cars)
        {
            return cars[UnityEngine.Random.Range(0, cars.Count)];
        }
    }
}
