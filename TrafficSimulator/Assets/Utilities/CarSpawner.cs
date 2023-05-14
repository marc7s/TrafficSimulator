using System.Collections.Generic;
using UnityEngine;
using VehicleBrain;
using System.Linq;
using System;

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

        [Header("Settings")]
        [SerializeField] private DrivingMode _drivingMode = DrivingMode.Quality;
        [SerializeField] private bool _randomVehicleTypes = true;
        [SerializeField] private SpawnMode _mode = SpawnMode.Total;
        [SerializeField] private CarSpawnerNavigationMode _navigationMode = CarSpawnerNavigationMode.FollowPrefabs;

        [Header("Debug Settings")]
        [SerializeField] private ShowTargetLines _showTargetLines = ShowTargetLines.None;
        [SerializeField] private bool _logBrakeReason = false;
        [SerializeField] private bool _showNavigationPath = false;

        // Total number of cars to spawn in mode Total
        public int TotalCars = 5;
        
        // Percentage of cars to spawn per lane in mode LaneRatio
        [Range(0, 1)] public float LaneCarRatio = 0.5f; 
        
        // Delay before spawning cars
        public float SpawnDelay = 1f;

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

        private List<CarsForLane> spawnableLanes = new List<CarsForLane>();

        
        public struct SpawnableLaneNode
        {
            private float _distance;
            private LaneNode _node;

            public float Distance { get => _distance; set => _distance = value; }
            public LaneNode Node { get => _node; set => _node = value; }

            public SpawnableLaneNode(float distance, LaneNode node)
            {
                _distance = distance;
                _node = node;
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

        private void Start()
        {
            _vehicleTypes = _randomVehicleTypes ? new List<GameObject>(){ _sedanPrefab, _sportsCar1Prefab, _sportsCar2Prefab, _suv1Prefab, _suv2Prefab, _van1Prefab, _van2Prefab } : new List<GameObject>(){ _sedanPrefab };

            _carLength = GetLongestCarLength(_vehicleTypes);

            _roadSystem = _roadSystemObject.GetComponent<RoadSystem>();
            _roadSystem.Setup();

            _roads = new List<DefaultRoad>(_roadSystem.DefaultRoads);

            AddLanesToList();
            CalculateLaneIndexes();
            CreateCarInLaneDict();
        }

        void Update()
        {
            if (!_spawned)
            {
                if (Time.time > SpawnDelay)
                {
                    _spawned = true;
                    SpawnCars();
                    Debug.Log("Total cars spawned: " + _carCounter);
                }
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

        private void CreateCarInLaneDict()
        {
            spawnableLanes.Clear();

            int maxCars = 0;
            for (int i = 0; i < _lanes.Count; i++)
            {
                List<SpawnableLaneNode> carsInLane = GetSpawnableLaneNodesInLane(_lanes[i]);
                spawnableLanes.Add(new CarsForLane(_lanes[i], carsInLane));
                
                maxCars += carsInLane.Count;
            }

            Debug.Log("Max cars: " + maxCars);
        }

        private List<CarsForLane> CalculateCarsPerLane(int carCount)
        {
            // Loop through _maxCarsInLane and subtract 1 car from each lane
            List<CarsForLane> availableLanes = new List<CarsForLane>(spawnableLanes);
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

        private List<SpawnableLaneNode> GetSpawnableLaneNodesInLane(Lane lane)
        {
            float distance = 0;
            float offset = 6f;

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
                    curr = CalculateNextNode(curr, ((_carLength / 2) + offset));
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
            if (carsToSpawn == 0)
            {
                Debug.Log("No cars to spawn");
                return;
            }

            // Calculate cars to spawn per lane
            List<CarsForLane> allLanes = CalculateCarsPerLane(carsToSpawn);

            // Loop through _carsThatShouldSpawn and spawn cars
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
        }

        /// <summary>Checks multiple conditions to determine if a car is able to spawn on node</summary>
        private bool IsCarSpawnable(LaneNode node)
        {
            if(node == null)
                return false;
                
            bool isThreeWayIntersection = true;
            bool temp = false;

            isThreeWayIntersection = node.RoadNode.Type == RoadNodeType.End && (node.Next?.IsIntersection() == true || node.Prev?.IsIntersection() == true);
            temp = !(node.RoadNode.IsIntersection() || node.RoadNode.Type == RoadNodeType.JunctionEdge || node == null || node.Next == null || node.HasVehicle()) && !isThreeWayIntersection && !node.RoadNode.IsNavigationNode;

            return temp;
        }

        /// <summary>Spawns a car at the current lane node</summary>
        private void SpawnCar(int index)
        {
            _currentCar = Instantiate(GetRandomCar(_vehicleTypes), _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);
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
        }

        private LaneNode CalculateNextNode(LaneNode startNode, float targetLength)
        {
            LaneNode curr = startNode.Next;
            float currentLength = 0;
            
            while(curr != null && ((curr.Type == RoadNodeType.JunctionEdge || curr.IsIntersection()) || currentLength < targetLength))
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
