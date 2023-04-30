using System.Collections.Generic;
using UnityEngine;
using Car;
using System.Linq;

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
        RandomPath
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
        [SerializeField] private bool _randomVehicleTypes = true;
        [SerializeField] private SpawnMode _mode = SpawnMode.Total;
        [SerializeField] private CarSpawnerNavigationMode _navigationMode = CarSpawnerNavigationMode.FollowPrefabs;

        // Total number of cars to spawn in mode Total
        public int TotalCars = 5;
        
        // Percentage of cars to spawn per lane in mode LaneRatio
        [Range(0, 1)] public float LaneCarRatio = 0.5f; 
        
        // Delay before spawning cars
        public float SpawnDelay = 1f;

        private RoadSystem _roadSystem;
        private List<Road> _roads;

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
        
        private void Start()
        {
            _vehicleTypes = _randomVehicleTypes ? new List<GameObject>(){ _sedanPrefab, _sportsCar1Prefab, _sportsCar2Prefab, _suv1Prefab, _suv2Prefab, _van1Prefab, _van2Prefab } : new List<GameObject>(){ _sedanPrefab };

            _carLength = GetLongestCarLength(_vehicleTypes);

            _roadSystem = _roadSystemObject.GetComponent<RoadSystem>();
            _roadSystem.Setup();

            _roads = _roadSystem.DefaultRoads;

            AddLanesToList();
            AddLaneLengths();
            CalculateLaneRatios();
            CalculateLaneIndexes();
            CalculateMaxCarsForLanes();
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
            foreach (Road road in _roadSystem.DefaultRoads)
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

        /// <summary>Calculates all lanes corresponding number of cars to spawn</summary>
        private void CalculateLaneRatios()
        {
            float totalLength = _lengths.Sum();

            // Loop through all lane lengths
            foreach (float length in _lengths)
                _ratios.Add(length / totalLength);
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
            foreach (Road road in _roadSystem.DefaultRoads)
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
            for (int i = 0; i < _lanes.Count; i++)
            {
                _offset = 0;

                // Calculate the number of cars to spawn
                int carsToSpawn = _mode == SpawnMode.Total ? Mathf.CeilToInt(_ratios[i] * TotalCars) : Mathf.CeilToInt(_maxCarsPerLane[i] * LaneCarRatio);
                
                // Return if there are no cars to spawn
                if (carsToSpawn == 0)
                    return;

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
            bool IsThreeWayIntersection = node.RoadNode.Type == RoadNodeType.End && (node.Next?.IsIntersection() == true || node.Prev?.IsIntersection() == true);
            return !(node.RoadNode.IsIntersection() || node.RoadNode.Type == RoadNodeType.JunctionEdge || node == null || node.Next == null || node.HasVehicle()) && !IsThreeWayIntersection && !node.RoadNode.IsNavigationNode;
        }

        /// <summary>Spawns a car at the current lane node</summary>
        private void SpawnCar(int index)
        {
            _currentCar = Instantiate(GetRandomCar(_vehicleTypes), _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);
            AutoDrive autoDrive = _currentCar.GetComponent<AutoDrive>();
            autoDrive.Road = _lanes[index].Road;
            autoDrive.LaneIndex = _indexes[index];

            // If a custom car is being used as a spawn prefab it should be deactivated to not interfere, so activate this car
            _currentCar.SetActive(true);

            autoDrive.CustomStartNode = _laneNodeCurrent;
            
            // Overwrite the navigation mode if a custom one is set
            switch(_navigationMode)
            {
                case CarSpawnerNavigationMode.Random:
                    autoDrive.OriginalNavigationMode = NavigationMode.Random;
                    break;
                case CarSpawnerNavigationMode.RandomPath:
                    autoDrive.OriginalNavigationMode = NavigationMode.RandomNavigationPath;
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
            return cars[Random.Range(0, cars.Count)];
        }
    }
}
