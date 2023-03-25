using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AutoDrive = Car.AutoDrive;

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
        public float CarLength = 4f; // Not exact value. Should get imported from car prefab somehow.
        public float SpawnDelay = 3f; // Delay before spawning cars

        private RoadSystem _roadSystem;
        private List<Road> _roads;

        private List<Lane> _lanes = new List<Lane>(); // List of all lanes
        private List<float> _lengths = new List<float>(); // List of all lane lengths
        private List<float> _ratios = new List<float>(); // List of all lane ratios
        private List<int> _indexes = new List<int>(); // List of all lane indexes
        private List<int> _maxCarsPerLane = new List<int>(); // List of all max cars per lane

        private LaneNode _laneNodeCurrent;
        private LaneNode _laneNodeNext;

        private GameObject _currentCar;

        private float _offset = 0; // Offset for the lane index
        private int _carCounter = 0;

        private bool _spawned = false;

        
        private void Start()
        {
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
                {
                    _lanes.Add(_roads[i].Lanes[j]);
                }
            }
        }

        private void CalculateLaneLengths()
        {
            // Loop through all lanes
            foreach (Lane lane in _lanes)
            {
                _lengths.Add(lane.GetLaneLengthNoIntersections());
            }
        }

        private void CalculateLaneRatios()
        {
            float totalLength = 0;

            // Loop through all lane lengths
            foreach (float length in _lengths)
            {
                totalLength = totalLength + length;
            }

            // Loop through all lane lengths
            for (int i = 0; i < _lengths.Count; i++)
            {
                _ratios.Add(_lengths[i] / totalLength);
            }
        }

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

        private void CalculateMaxCarsForLanes()
        {
            // Loop through all lanes
            for (int j = 0; j < _lanes.Count; j++)
            {
                // Calculate the max cars for the lane
                _maxCarsPerLane.Add(Mathf.FloorToInt(_lengths[j] / CarLength));
            }
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

                // Spawn cars
                for (int j = 0; j < carsToSpawn; j++)
                {
                    // Check if max cars have been spawned
                    if (_mode == SpawnMode.Total && _carCounter >= TotalCars)
                        return;

                    // Spawn individual car at current node
                    if(!_laneNodeCurrent.RoadNode.IsIntersection() && !(_laneNodeCurrent.RoadNode.Type == RoadNodeType.JunctionEdge))
                    {
                        SpawnCar(i);
                        _carCounter++;
                    } else
                    {
                        carsToSpawn++;
                    }
                
                    _offset += _lanes[i].Length / carsToSpawn;
                    _laneNodeCurrent = CalculateSpawnNode(_offset, _lanes[i]);
                }
                _offset = 0;
            }
        }

        private void SpawnCar(int index)
        {
            _currentCar = Instantiate(_carPrefab, _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);
            _currentCar.GetComponent<AutoDrive>().Road = _lanes[index].Road;
            _currentCar.GetComponent<AutoDrive>().LaneIndex = _indexes[index];

            // If a custom car is being used as a spawn prefab it should be deactivated to not interfere, so activate this car
            _currentCar.SetActive(true);

            _currentCar.GetComponent<AutoDrive>().CustomStartNode = _laneNodeCurrent.Next != null ? _laneNodeCurrent.Next : _laneNodeCurrent;
        }

        // Finds next LaneNode in lane after a certain distance
        private LaneNode CalculateSpawnNode(float targetLength, Lane lane)
        {
            float currentLength = 0;
            LaneNode curr = lane.StartNode;
            while(curr != null && currentLength < targetLength)
            {
                currentLength += curr.DistanceToPrevNode;
                curr = curr.Next;
            }
            return curr;
        }
    }
}
