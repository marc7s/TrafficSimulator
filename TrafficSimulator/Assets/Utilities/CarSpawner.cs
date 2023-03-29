using System.Collections.Generic;
using UnityEngine;
using Car;

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

        public int TotalCars = 5; // Total number of cars to spawn in mode Total
        [Range(0, 1)] public float LaneCarRatio = 0.5f; // Percentage of cars to spawn per lane in mode LaneRatio
        public float SpawnDelay = 3f; // Delay before spawning cars

        private RoadSystem _roadSystem;
        private List<Road> _roads;

        private List<Lane> _lanes = new List<Lane>(); // List of all lanes
        private List<float> _lengths = new List<float>(); // List of all lane lengths
        private List<float> _ratios = new List<float>(); // List of all lane ratios
        private List<int> _indexes = new List<int>(); // List of all lane indexes
        private List<int> _maxCarsPerLane = new List<int>(); // List of all max cars per lane

        private LaneNode _laneNodeCurrent;

        private GameObject _currentCar;

        private int _carCounter = 0;
        private float _offset = 0; // Offset for the lane index
        private float _carLength;

        private bool _spawned = false;

        
        private void Start()
        {
            _carLength = _carPrefab.transform.GetChild(1).GetComponent<MeshRenderer>().bounds.size.z * 3f;

            _roadSystem = _roadSystemObject.GetComponent<RoadSystem>();
            _roadSystem.Setup();

            _roads = _roadSystem.Roads;

            AddLanesToList();
            CalculateLaneLengths();
            CalculateLaneRatios();
            CalculateLaneIndexes();
            CalculateMaxCarsForLanes();
        }

        void Update()
        {
            if (!_spawned) {
                if (Time.time > SpawnDelay) {
                    _spawned = true;
                    SpawnCars();
                    Debug.Log("Total cars spawned: " + _carCounter);
                }
            }
        }

        private void AddLanesToList()
        {
            // Loop through all roads
            for (int i = 0; i < _roadSystem.RoadCount; i++)
            {
                // Loop through all lanes
                for (int j = 0; j < _roads[i].LaneCount; j++)
                    _lanes.Add(_roads[i].Lanes[j]);
            }
        }

        /// <summary>Calculates lane lengths without intersections for each lane</summary>
        private void CalculateLaneLengths()
        {
            foreach (Lane lane in _lanes)
                _lengths.Add(lane.GetLaneLengthNoIntersections());
        }

        /// <summary>Calculates all lanes corresponding number of cars to spawn</summary>
        private void CalculateLaneRatios()
        {
            float totalLength = 0;

            // Loop through all lane lengths
            foreach (float length in _lengths)
                totalLength += length;

            // Loop through all lane lengths
            for (int i = 0; i < _lengths.Count; i++)
                _ratios.Add(_lengths[i] / totalLength);
        }

        /// <summary>Calculates a lane sections corresponding number of cars to spawn</summary>
        private List<float> CalculateSectionRatios(List<float> sections)
        {
            float totalLength = 0;
            List<float> ratios = new List<float>();

            foreach (float length in sections)
                totalLength += length;

            for (int i = 0; i < sections.Count; i++)
                ratios.Add(sections[i] / totalLength);
            return ratios;
        }

        /// <summary>Saves the index of each lane in a list</summary>
        private void CalculateLaneIndexes()
        {
            int laneIndex;

            // Loop through all roads
            foreach (Road road in _roadSystem.Roads)
            {
                laneIndex = 0;

                // Loop through all lanes
                for (int j = 0; j < road.LaneCount; j++)
                {
                    _indexes.Add(laneIndex);
                    laneIndex++;
                }
            }
        }

        /// <summary>Calculates the max capacity of cars for a lane</summary>
        private void CalculateMaxCarsForLanes()
        {
            for (int j = 0; j < _lanes.Count; j++)
                _maxCarsPerLane.Add(Mathf.FloorToInt(_lengths[j] / _carLength)); 
        }

        private void SpawnCars()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                // Calculate the number of cars to spawn
                int carsToSpawn = _mode == SpawnMode.Total ? Mathf.CeilToInt(_ratios[i] * TotalCars) : Mathf.CeilToInt(_maxCarsPerLane[i] * LaneCarRatio);
                // Return if there are no cars to spawn
                if (carsToSpawn == 0)
                    return;

                // Calculate the offset
                _laneNodeCurrent = _lanes[i].StartNode;
                Debug.Log("Lane " + i);

                List<float> sections = DivideLaneToSections(_lanes[i]);
                List<float> sectionRatios = CalculateSectionRatios(sections);
                for (int j = 0; j < sections.Count; j++)
                {
                    // Calculate the number of cars to spawn
                    int carsToSpawnSection = Mathf.CeilToInt(carsToSpawn * sectionRatios[j]);
                    for (int k = 0; k < carsToSpawnSection; k++)
                    {
                        // Check if max cars have been spawned
                        if (_mode == SpawnMode.Total && _carCounter >= TotalCars)
                            return;

                        // Spawn car
                        if(!_laneNodeCurrent.RoadNode.IsIntersection() && !(_laneNodeCurrent.RoadNode.Type == RoadNodeType.JunctionEdge) && !(_laneNodeCurrent == null))
                        {
                            SpawnCar(i);
                            _carCounter++;
                        }

                        // Calculate the next spawn node in the section based on distance
                        _offset += sections[j] / (carsToSpawnSection);
                        _laneNodeCurrent = CalculateSpawnNode(_offset, _lanes[i]);
                    }
                }
                _offset = 0;
            }
        }

        /// <summary>Divides a lane into sections based on intersections</summary>
        private List<float> DivideLaneToSections(Lane lane)
        {
            LaneNode curr = lane.StartNode;
            LaneNode prev = lane.StartNode;
            Debug.Log("Start node: " + curr.DistanceToPrevNode);
            List<float> sections = new List<float>();

            // Determine the direction of the lane
            int direction = curr.DistanceToPrevNode == 0 ? 1 : -1;

            // Determine the start section length based on direction
            float sectionLength = direction == 1 ? curr.Next.DistanceToPrevNode : 0;

            while (curr != null)
            {
                // Check if the current node is an intersection or if the next node is null to determine the end of a section
                if(curr.RoadNode.IsIntersection() || (curr.RoadNode.Type == RoadNodeType.JunctionEdge) || curr.Next == null)
                {
                    sections.Add(sectionLength);
                    Debug.Log("Section length: " + sectionLength);

                    // Determine the start section length based on direction
                    sectionLength = direction == 1 ? (curr.DistanceToPrevNode * 2) : 0;

                    // While the node is an intersection, skip it
                    while(curr.RoadNode.IsIntersection() || (curr.RoadNode.Type == RoadNodeType.JunctionEdge))
                        curr = curr.Next;
                }
                prev = curr;
                curr = curr.Next;

                // Add the distance between the current and previous node to the section length
                if(curr != null)
                    sectionLength += Vector3.Distance(curr.Position, prev.Position);
            }
            Debug.Log("Lane Length without intersections: " + lane.GetLaneLengthNoIntersections());
            return sections;
        }

        /// <summary>Spawns a car at the current lane node</summary>
        private void SpawnCar(int index)
        {
            _currentCar = Instantiate(_carPrefab, _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);
            _currentCar.GetComponent<AutoDrive>().Road = _lanes[index].Road;
            _currentCar.GetComponent<AutoDrive>().LaneIndex = _indexes[index];

            // If a custom car is being used as a spawn prefab it should be deactivated to not interfere, so activate this car
            _currentCar.SetActive(true);

            _currentCar.GetComponent<AutoDrive>().CustomStartNode = _laneNodeCurrent.Next != null ? _laneNodeCurrent.Next : _laneNodeCurrent;
        }

        /// <summary>Finds next LaneNode in lane after a certain distance</summary>
        private LaneNode CalculateSpawnNode(float targetLength, Lane lane)
        {
            LaneNode curr = lane.StartNode;
            LaneNode prev = lane.StartNode;
            float currentLength = 0;
            while(curr != null && currentLength < targetLength)
            {
                currentLength += Vector3.Distance(curr.Position, prev.Position);
                prev = curr;
                curr = curr.Next;
            }
            return curr;
        }
    }
}
