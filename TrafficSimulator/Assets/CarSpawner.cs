using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vehicle = DataModel.Vehicle;
using AutoDrive = Car.AutoDrive;

namespace RoadGenerator
{
    public class CarSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject carPrefab;
        [SerializeField] private GameObject roadSystem;

        [SerializeField] protected bool Mode = false; // True = Spawn cars with ratio, False = Spawn cars with percentage

        public int MaxCarsRatio = 5; // Number of cars to spawn per lane
        [Range(0, 1)]
        public float MaxCarsPercentage = 0.5f; // Percentage of cars to spawn per lane
        public float Distance = 10f; // Distance between cars
        public float CarLength = 4f; // Not exact value. Should get imported from car prefab somehow.
        public float SpawnDelay = 3f; // Delay before spawning cars

        private RoadSystem _roadSystem;
        private List<Road> _roads;
        private int _maxCars;

        private List<Lane> _lanes = new List<Lane>(); // List of all lanes
        private List<float> _lengths = new List<float>(); // List of all lane lengths
        private List<float> _ratios = new List<float>(); // List of all lane ratios
        private List<int> _indexes = new List<int>(); // List of all lane indexes
        private List<int> _maxCarsPerLane = new List<int>(); // List of all max cars per lane

        private LaneNode _laneNodeCurrent;
        private LaneNode _laneNodeNext;
        private LaneNode _laneNodeTemp;

        public Vehicle Vehicle;
        private GameObject _currentCar;

        private int _offset; // Offset for the lane index
        private int _carCounter = 0;

        private bool _spawned = false;

        
        private void Start()
        {
            _roadSystem = roadSystem.GetComponent<RoadSystem>();
            _roadSystem.Setup();

            _roads = _roadSystem.Roads;
            Vehicle = new Vehicle("Car", carPrefab);

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
                    if (Mode) {
                        Debug.Log("Spawning cars with ratio");
                        SpawnCarsWithRatio(MaxCarsRatio);
                    } else {
                        Debug.Log("Spawning cars with percentage");
                        SpawnCarsWithPercentage(MaxCarsPercentage);
                    }
                    Debug.Log("Total cars spawned: " + _carCounter);
                }
            }
        }

        private void SetNodeUnderVehicle()
        {
            // Get the length of the car
            float CarLength = carPrefab.GetComponentInChildren<MeshCollider>().bounds.size.z;
            _laneNodeNext = _laneNodeCurrent;

            // Set the first node under the car as occupied
            _laneNodeCurrent.SetVehicle(Vehicle);

            // Set the rest of the nodes under the car as occupied
            while (CarLength > Vector3.Distance(_laneNodeCurrent.Position, _laneNodeNext.Position))
            {
                _laneNodeNext = _laneNodeNext.Next;
                _laneNodeNext.SetVehicle(Vehicle);
            }
        }

        private void AddLanesToList()
        {
            // Loop through all roads
            for (int i = 0; i < _roadSystem.RoadCount; i++)
            {
                _roads[i].OnChange();

                // Loop through all lanes
                for (int j = 0; j < _roads[i].LaneCount; j++)
                {
                    _lanes.Add(_roads[i].Lanes[j]);
                }
            }
        }

        private void CalculateLaneLengths()
        {
            LaneNode _laneNodeTemp;
            float laneLength;

            // Loop through all lanes
            for (int j = 0; j < _lanes.Count; j++)
            {
                laneLength = 0;
                _laneNodeTemp = _lanes[j].StartNode;

                // Loop through all lane nodes
                for (int k = 0; k < _lanes[j].StartNode.Count - 1; k++)
                {
                    // Calculate the length of the lane
                    laneLength = laneLength + Vector3.Distance(_laneNodeTemp.Position, _laneNodeTemp.Next.Position);
                    _laneNodeTemp = _laneNodeTemp.Next;
                }
                _lengths.Add(laneLength);
            }
        }

        private void CalculateLaneRatios()
        {
            float totalLength = 0;

            // Loop through all lane lengths
            for (int i = 0; i < _lengths.Count; i++)
            {
                totalLength = totalLength + _lengths[i];
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
            for (int i = 0; i < _roadSystem.RoadCount; i++)
            {
                _roads[i].OnChange();
                laneIndex = 0;

                // Loop through all lanes
                for (int j = 0; j < _roads[i].LaneCount; j++)
                {
                    _indexes.Add(laneIndex);
                    laneIndex = laneIndex + 1;
                }
            }
        }

        private void CalculateMaxCarsForLanes()
        {
            // Loop through all lanes
            for (int j = 0; j < _lanes.Count; j++)
            {
                // Calculate the max cars for the lane
                _maxCarsPerLane.Add((int)(_lengths[j] / CarLength));
            }
        }

        private void SpawnCarsWithPercentage(float MaxCarsPercentage) 
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                // Calculate the number of cars to spawn
                int carsToSpawn = (int)Mathf.Ceil(_maxCarsPerLane[i] * MaxCarsPercentage);
                Debug.Log("Cars to spawn: " + carsToSpawn + " on lane " + i + " with " + _maxCarsPerLane[i] + " max cars");

                // Calculate the _offset
                _offset = _lanes[i].StartNode.Count / carsToSpawn;
                _laneNodeCurrent = _lanes[i].StartNode;

                // Spawn cars
                for (int j = 0; j < carsToSpawn; j++)
                {
                    // Spawn individual car at current node
                    SpawnCar(i);

                    _carCounter = _carCounter + 1;

                    Set_Offset(_offset);
                }
            }
        }

        private void SpawnCarsWithRatio(int maxCars)
        {
            int cars_Spawned = 0;
            for (int i = 0; i < _lanes.Count; i++)
            {
                // Calculate the number of cars to spawn for this lane
                int carsToSpawn = (int)Mathf.Ceil(_ratios[i] * maxCars);
                Debug.Log("Cars to spawn: " + carsToSpawn + " on lane " + i);

                // Calculate the _offset
                _offset = _lanes[i].StartNode.Count / carsToSpawn;

                _laneNodeCurrent = _lanes[i].StartNode;

                // Spawn cars
                for (int j = 0; j < carsToSpawn; j++)
                {
                    // Check if max cars have been _spawned
                    if (cars_Spawned == maxCars) {
                        return;
                    }

                    // Spawn individual car at current node
                    SpawnCar(i);

                    cars_Spawned = cars_Spawned + 1;
                    _carCounter = _carCounter + 1;

                    Set_Offset(_offset);
                }
            }
        }

        private void Set_Offset(int _offset) 
        {
            for (int i = 0; i < _offset; i++)
                {
                    _laneNodeCurrent = _laneNodeCurrent.Next;
                }
        }

        private void SpawnCar(int index)
        {
            _currentCar = Instantiate(carPrefab, _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);

            _currentCar.GetComponent<AutoDrive>()._road = _lanes[index]._road;
            _currentCar.GetComponent<AutoDrive>()._laneIndex = _indexes[index];
            _currentCar.GetComponent<AutoDrive>().CustomStartNode = _laneNodeCurrent.Next.Next;
            _currentCar.GetComponent<AutoDrive>().Start();
        }
    }
}
