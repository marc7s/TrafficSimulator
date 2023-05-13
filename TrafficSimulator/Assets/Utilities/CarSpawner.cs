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
        private List<float> _lengths = new List<float>();
        private List<float> _ratios = new List<float>();
        private List<int> _indexes = new List<int>();
        private List<int> _maxCarsPerLane = new List<int>();

        private LaneNode _laneNodeCurrent;

        private GameObject _currentCar;

        private int _carCounter = 0;
        private float _offset = 0;
        private float _carLength;

        private bool _spawned = false;

        int notSpawnedCounter = 0;
        int isCarSpawnableCounter = 0;
        
        private void Start()
        {
            _vehicleTypes = _randomVehicleTypes ? new List<GameObject>(){ _sedanPrefab, _sportsCar1Prefab, _sportsCar2Prefab, _suv1Prefab, _suv2Prefab, _van1Prefab, _van2Prefab } : new List<GameObject>(){ _sedanPrefab };

            _carLength = GetLongestCarLength(_vehicleTypes);

            _roadSystem = _roadSystemObject.GetComponent<RoadSystem>();
            _roadSystem.Setup();

            _roads = new List<DefaultRoad>(_roadSystem.DefaultRoads);

            AddLanesToList();
            AddLaneLengths();
            CalculateLaneRatios();
            CalculateLaneIndexes();
            CalculateMaxCarsForLanes();

            //DebugTester();
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

        private void DebugTester()
        {
            /*
            Debug.Log("Total roads: " + _roads.Count);
            Debug.Log("Total lanes: " + _lanes.Count);
            Debug.Log("Total lengths: " + _lengths.Count);
            Debug.Log("Total ratios: " + _ratios.Count);
            Debug.Log("Total indexes: " + _indexes.Count);
            Debug.Log("Total max cars per lane: " + _maxCarsPerLane.Count);
            */

            int maxCarCount = 0;
            
            for (int i = 0; i < _lanes.Count / 10; i++)
            {
                /*
                Debug.Log("--------------------");
                Debug.Log("Lane " + i + " length: " + _lengths[i]);
                Debug.Log("Lane " + i + " ratio: " + _ratios[i]);
                Debug.Log("Lane " + i + " index: " + _indexes[i]);
                Debug.Log("Lane " + i + " max cars: " + _maxCarsPerLane[i]);
                */

                maxCarCount += _maxCarsPerLane[i];
            }

            Debug.Log("Total max cars: " + maxCarCount);
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

        /// <summary>Calculates lane lengths without intersections for each lane</summary>
        private void AddLaneLengths()
        {
            foreach (Lane lane in _lanes)
                _lengths.Add(lane.GetLaneLengthNoIntersections());
        }

        private void AddLaneLengthsTotal(int laneCount)
        {
            for(int i = 0; i < laneCount; i++)
                _lengths.Add(_lanes[i].GetLaneLengthNoIntersections());
        }

        /// <summary>Calculates all lanes corresponding number of cars to spawn</summary>
        private void CalculateLaneRatios()
        {
            float totalLength = _lengths.Sum();

            // Loop through all lane lengths
            foreach (float length in _lengths)
                _ratios.Add(length / totalLength);
        }

        private void CalculateLaneRatiosTotal(int laneCount)
        {
            float totalLength = _lengths.Sum();

            // Loop through all lane lengths
            for(int i = 0; i < laneCount; i++)
                _ratios.Add(_lengths[i] / totalLength);
        }

        /// <summary>Calculates a lane sections corresponding number of cars to spawn</summary>
        private List<float> CalculateSectionRatios(List<float> sections)
        {
            float totalLength = sections.Sum();
            List<float> ratios = new List<float>();

            foreach (float section in sections)
                ratios.Add(section / totalLength);
            return ratios;
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

        /// <summary>Calculates the max capacity of cars for a lane</summary>
        private void CalculateMaxCarsForLanes()
        {
            foreach (float length in _lengths)
                _maxCarsPerLane.Add(Mathf.FloorToInt(length / _carLength));
        }

        private void SpawnCars()
        {
            int toSpawnCounter = 0;
            int loopCounter = _lanes.Count;

            // Check if less cars than lanes should be spawned for total mode
            if (_mode == SpawnMode.Total && TotalCars < _lanes.Count)
            {
                loopCounter = TotalCars;
                _lengths.Clear();
                _ratios.Clear();
                AddLaneLengthsTotal(TotalCars);
                CalculateLaneRatiosTotal(TotalCars);
            }

            for (int i = 0; i < loopCounter; i++)
            {
                _offset = 0;

                // Calculate the number of cars to spawn
                int carsToSpawn = _mode == SpawnMode.Total ? Mathf.CeilToInt(_ratios[i] * TotalCars) : Mathf.CeilToInt(_maxCarsPerLane[i] * LaneCarRatio);
                toSpawnCounter += carsToSpawn;
                // Return if there are no cars to spawn
                if (carsToSpawn == 0 || (carsToSpawn == 1 && toSpawnCounter >= TotalCars))
                {
                    Debug.Log("No cars to spawn");
                    continue;
                }

                Debug.Log("Cars to spawn: " + carsToSpawn);
                // Calculate the offset
                _laneNodeCurrent = _lanes[i].StartNode;

                List<float> sections = DivideLaneIntoSections(_lanes[i]);
                List<float> sectionRatios = CalculateSectionRatios(sections);
                for (int j = 0; j < sections.Count; j++)
                {
                    // Calculate the number of cars to spawn
                    int carsToSpawnInSection = Mathf.CeilToInt(carsToSpawn * sectionRatios[j]);
                    SpawnCarsInSection(_lanes[i], i, carsToSpawnInSection, sections[j]);
                }
            }
            Debug.Log("Total not spawned: " + notSpawnedCounter);
            Debug.Log("isCarSpawnable counter: " + isCarSpawnableCounter);
            Debug.Log("toSpawnCounter: " + toSpawnCounter);
        }

        /// <summary>Spawns cars in a section</summary>
        private void SpawnCarsInSection(Lane lane, int laneIndex, int carsToSpawn, float sectionLength)
        {
            // Check if the current node is an intersection
            if (_laneNodeCurrent.RoadNode.IsIntersection())
                _laneNodeCurrent = _laneNodeCurrent.Next;

            if(_laneNodeCurrent == null)
                return;

            // Add intersection distance to offset
            if (_laneNodeCurrent != lane.StartNode)
                _offset += _laneNodeCurrent.DistanceToPrevNode + _laneNodeCurrent.Prev.DistanceToPrevNode;

            for (int i = 0; i < carsToSpawn; i++)
            {
                // Check if max cars have been spawned
                if (_mode == SpawnMode.Total && _carCounter >= TotalCars)
                    return;

                // Spawn car
                if (IsCarSpawnable(_laneNodeCurrent))
                {
                    SpawnCar(laneIndex);
                    _carCounter++;
                }
                else
                {
                    notSpawnedCounter++;
                }
                
                // Calculate the next spawn node in the section based on distance
                _offset += sectionLength / (carsToSpawn);
                _laneNodeCurrent = CalculateSpawnNode(_offset, lane);
            }
        }

        /// <summary>Divides a lane into sections based on intersections</summary>
        private List<float> DivideLaneIntoSections(Lane lane)
        {
            LaneNode curr = lane.StartNode;
            List<float> sections = new List<float>();
            float sectionLength = 0;

            while (curr != null)
            {
                // Check if the current node is an intersection or if the next node is null to determine the end of a section
                if(curr.RoadNode.IsIntersection())
                {   
                    sections.Add(sectionLength);
                    sectionLength = 0;
                    curr = curr.Next;
                } 
                else
                {
                    sectionLength += curr.DistanceToPrevNode;
                }
                
                curr = curr.Next;
            }
            
            if(!lane.StartNode.Last.Prev.RoadNode.IsIntersection())
                sections.Add(sectionLength);
           
            return sections;
        }

        /// <summary>Checks multiple conditions to determine if a car is able to spawn on node</summary>
        private bool IsCarSpawnable(LaneNode node)
        {
            bool isThreeWayIntersection = true;
            bool temp = false;
            //Debug.Log("Node Type: " + node.RoadNode.Type + " & " + RoadNodeType.End + " & " + node.Next?.IsIntersection() + " & " + node.Prev?.IsIntersection());
            try
            {
                isThreeWayIntersection = node.RoadNode.Type == RoadNodeType.End && (node.Next?.IsIntersection() == true || node.Prev?.IsIntersection() == true);
                temp = !(node.RoadNode.IsIntersection() || node.RoadNode.Type == RoadNodeType.JunctionEdge || node == null || node.Next == null || node.HasVehicle()) && !isThreeWayIntersection && !node.RoadNode.IsNavigationNode;
            } catch (NullReferenceException)
            {
                Debug.Log("Epic error");
            }
            isCarSpawnableCounter++;
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

        /// <summary>Finds next LaneNode in lane after a certain distance</summary>
        private LaneNode CalculateSpawnNode(float targetLength, Lane lane)
        {
            LaneNode curr = lane.StartNode;
            float currentLength = 0;
            
            while(curr != null && currentLength < targetLength)
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
