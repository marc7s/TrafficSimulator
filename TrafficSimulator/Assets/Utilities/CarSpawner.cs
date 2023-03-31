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

    public class CarSpawner : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private GameObject _carPrefab;
        [SerializeField] private GameObject _roadSystemObject;

        [Header("Settings")]
        [SerializeField] private SpawnMode _mode = SpawnMode.Total;

        // Total number of cars to spawn in mode Total
        public int TotalCars = 5;
        
        // Percentage of cars to spawn per lane in mode LaneRatio
        [Range(0, 1)] public float LaneCarRatio = 0.5f; 
        
        // Delay before spawning cars
        public float SpawnDelay = 1f;

        private RoadSystem _roadSystem;
        private List<Road> _roads;

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
            _carLength = _carPrefab.transform.GetChild(1).GetComponent<MeshRenderer>().bounds.size.z;

            _roadSystem = _roadSystemObject.GetComponent<RoadSystem>();
            _roadSystem.Setup();

            _roads = _roadSystem.Roads;

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

        private void AddLanesToList()
        {
            foreach (Road road in _roadSystem.Roads)
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
            foreach (Road road in _roadSystem.Roads)
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
            return !(node.RoadNode.IsIntersection() || node.RoadNode.Type == RoadNodeType.JunctionEdge || node == null || node.Next == null || node.HasVehicle());
        }

        /// <summary>Spawns a car at the current lane node</summary>
        private void SpawnCar(int index)
        {
            _currentCar = Instantiate(_carPrefab, _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);
            _currentCar.GetComponent<AutoDrive>().Road = _lanes[index].Road;
            _currentCar.GetComponent<AutoDrive>().LaneIndex = _indexes[index];

            // If a custom car is being used as a spawn prefab it should be deactivated to not interfere, so activate this car
            _currentCar.SetActive(true);

            _currentCar.GetComponent<AutoDrive>().CustomStartNode = _laneNodeCurrent;
            _currentCar.GetComponent<AutoDrive>().Setup();
        }

        /// <summary>Finds next LaneNode in lane after a certain distance</summary>
        private LaneNode CalculateSpawnNode(float targetLength, Lane lane)
        {
            LaneNode curr = lane.StartNode;
            LaneNode prev = lane.StartNode;
            float currentLength = 0;
            
            while(curr != null && currentLength < targetLength)
            {
                currentLength += curr.DistanceToPrevNode;
                prev = curr;
                curr = curr.Next;
            }
            
            return curr;
        }
    }
}
