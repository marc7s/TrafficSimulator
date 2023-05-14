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

        private Dictionary<Lane, int> _maxCarsInLane = new Dictionary<Lane, int>();
        private Dictionary<Lane, int> _carsToSpawnInLane = new Dictionary<Lane, int>();
        
        public struct SpawnableLaneNodes
        {
            private float _distance;
            private LaneNode _node;

            public float Distance { get => _distance; set => _distance = value; }
            public LaneNode Node { get => _node; set => _node = value; }

            public SpawnableLaneNodes(float distance, LaneNode node)
            {
                _distance = distance;
                _node = node;
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
            int maxCars = 0;

            for (int i = 0; i < _lanes.Count; i++)
            {
                Debug.Log("Something: " + GetSpawnableLaneNodesInLane(_lanes[i]).Count);

                if (!_maxCarsInLane.ContainsKey(_lanes[i]))
                    _maxCarsInLane.Add(_lanes[i], GetSpawnableLaneNodesInLane(_lanes[i]).Count);
                
                if (!_carsToSpawnInLane.ContainsKey(_lanes[i]))
                    _carsToSpawnInLane.Add(_lanes[i], 0);
                
                maxCars += GetSpawnableLaneNodesInLane(_lanes[i]).Count;
            }
            Debug.Log("Count: " + _maxCarsInLane.Count);
            Debug.Log("Max cars: " + maxCars);
        }

        private Dictionary<Lane, int> CalculateCarsPerLane(int carCount)
        {
            // Loop through _maxCarsInLane and subtract 1 car from each lane
            int index = 0;
            int carsToSpawn = carCount;
            Dictionary<Lane, int> _carsThatShouldSpawn = new Dictionary<Lane, int>();

            while (carsToSpawn >= 1)
            {
                // If the lane has cars left to spawn, subtract 1 from it and add 1 to _carsThatShouldSpawn
                if(_maxCarsInLane[_lanes[index]] > 0)
                {
                    _maxCarsInLane[_lanes[index]]--;
                    _carsToSpawnInLane[_lanes[index]]++;
                }
                else
                {
                    // If lane is full, skip it
                    index++;
                    continue;
                }

                // If the lane reaches is max cars, add it to _carsThatShouldSpawn
                if (_maxCarsInLane[_lanes[index]] <= 0)
                    _carsThatShouldSpawn.Add(_lanes[index], _carsToSpawnInLane[_lanes[index]]);

                index++;
                carsToSpawn--;

                // If index is out of range, reset it
                if (index >= _maxCarsInLane.Count)
                    index = 0;
            }

            // Add the rest of _carsToSpawnInLane to _carsThatShouldSpawn
            if (_carsToSpawnInLane.Count > 0)
            {
                foreach (KeyValuePair<Lane, int> pair in _carsToSpawnInLane)
                {
                    if (!_carsThatShouldSpawn.ContainsKey(pair.Key))
                        _carsThatShouldSpawn.Add(pair.Key, pair.Value);
                }
            }

            return _carsThatShouldSpawn;
        }

        private List<SpawnableLaneNodes> GetSpawnableLaneNodesInLane(Lane lane)
        {
            float distance = 0;
            float offset = 4f;

            List<SpawnableLaneNodes> spawnableNodes = new List<SpawnableLaneNodes>();

            LaneNode curr = lane.StartNode.Next;

            while (curr != null)
            {
                if(IsCarSpawnable(curr))
                {
                    distance += curr.DistanceToPrevNode;
                    spawnableNodes.Add(new SpawnableLaneNodes(distance, curr));
                    curr = CalculateNextNode(curr, ((_carLength / 2) + offset));
                }
                else
                {
                    distance += curr.DistanceToPrevNode;
                    curr = curr.Next;
                }

                if (curr == null)
                    break;
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
            Dictionary<Lane, int> _carsThatShouldSpawn = new Dictionary<Lane, int>();
            _carsThatShouldSpawn = CalculateCarsPerLane(carsToSpawn);
            Debug.Log("Dictionary count: " + _carsThatShouldSpawn.Count);

            // Loop through _carsThatShouldSpawn and spawn cars
            foreach (KeyValuePair<Lane, int> pair in _carsThatShouldSpawn)
            {
                if(pair.Value == 0)
                    continue;

                // Get Spawnable nodes
                List<SpawnableLaneNodes> spawnableNodes = GetSpawnableLaneNodesInLane(pair.Key);

                for (int i = 0; i < pair.Value; i++)
                {
                    _laneNodeCurrent = spawnableNodes[i].Node;
                    
                    // Spawn car
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
            
            while((curr != null && currentLength < targetLength) || !curr.RoadNode.IsIntersection())
            {
                currentLength += curr.DistanceToPrevNode;
                curr = curr.Next;

                if(curr == null)
                    break;
            }

            return curr;
        }

        private GameObject GetRandomCar(List<GameObject> cars)
        {
            return cars[UnityEngine.Random.Range(0, cars.Count)];
        }
    }
}
