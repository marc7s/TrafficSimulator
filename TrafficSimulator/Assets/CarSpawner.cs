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

        public int MaxCars = 5; // Number of cars to spawn per lane
        public float Distance = 10f; // Distance between cars
        public float carLength = 4f; // Not exact value. Should get imported from car prefab somehow.


        private RoadSystem _roadSystem;
        private List<Road> _roads;
        private int _maxCars;

        private LaneNode _laneNodeCurrent;
        private LaneNode _laneNodeNext;
        private LaneNode _laneNodeTemp;

        public Vehicle _vehicle;
        private GameObject _currentCar;

        private int offsetCounter = 0;
        private int offset;
        private int carCounter = 0;

        public float spawnDelay = 3f;
        private bool spawned = false;

        
        private void Start()
        {
            _roadSystem = roadSystem.GetComponent<RoadSystem>();
            _roadSystem.Setup();

            _roads = _roadSystem.Roads;
            _vehicle = new Vehicle("Car", carPrefab);

            Debug.Log(CalculateMaxCarsForLanes());
        }

        void Update()
        {
            if (!spawned) {
                if (Time.time > spawnDelay) {
                    spawned = true;
                    SpawnCars(MaxCars);
                    Debug.Log("Cars spawned: " + carCounter);
                }
            }
        }

        private void SpawnCars(int MaxCars)
        {
            // Loop through all roads
            for (int i = 0; i < _roadSystem.RoadCount; i++)
            {
                _roads[i].OnChange();
                
                // Loop through all lanes
                for (int j = 0; j < _roads[i].LaneCount; j++)
                {
                    // Get the start node of the lane
                    Lane lane = _roads[i].Lanes[j];
                    Debug.Log("LaneNode count: " + lane.StartNode.Count);
                    _laneNodeCurrent = lane.StartNode;
                    offset = lane.StartNode.Count / MaxCars;
                    Debug.Log("Offset: " + offset);

                    // Spawn cars
                    for (int k = 0; k < MaxCars; k++)
                    {
                        // Spawn individual car at current node
                        _currentCar = Instantiate(carPrefab, _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);

                        carCounter = carCounter + 1;

                        _currentCar.GetComponent<AutoDrive>()._road = _roads[i];
                        _currentCar.GetComponent<AutoDrive>()._laneIndex = j;
                        _currentCar.GetComponent<AutoDrive>().CustomStartNode = _laneNodeCurrent.Next.Next;
                        _currentCar.GetComponent<AutoDrive>().Start();
                        

                        // Tell the nodes under car that they are occupied
                        //SetNodeUnderVehicle();

                        // Calculate next spawning node
                        //CalculateOffset(Distance);

                        // Offset node for next car
                        for (int l = 0; l < offset; l++)
                        {
                            _laneNodeCurrent = _laneNodeCurrent.Next;
                            offsetCounter = offsetCounter + 1;
                        }
                    }
                }
            }
        }

        private void SetNodeUnderVehicle()
        {
            // Get the length of the car
            float carLength = carPrefab.GetComponentInChildren<MeshCollider>().bounds.size.z;
            _laneNodeNext = _laneNodeCurrent;

            // Set the first node under the car as occupied
            _laneNodeCurrent.SetVehicle(_vehicle);

            // Set the rest of the nodes under the car as occupied
            while (carLength > Vector3.Distance(_laneNodeCurrent.Position, _laneNodeNext.Position))
            {
                _laneNodeNext = _laneNodeNext.Next;
                _laneNodeNext.SetVehicle(_vehicle);
            }
        }

        private void CalculateOffset(float Distance)
        {
            _laneNodeTemp = _laneNodeCurrent;

            // Get the next node that is not occupied
            while (_laneNodeTemp.Vehicle != null)
            {
                _laneNodeTemp = _laneNodeTemp.Next;
            }
            _laneNodeNext = _laneNodeTemp;

            // Get the next node that is far enough away from the first unoccupied node
            while (Distance > Vector3.Distance(_laneNodeTemp.Position, _laneNodeNext.Position))
            {
                _laneNodeNext = _laneNodeNext.Next;
            }

            // Set the current node to that node
            _laneNodeCurrent = _laneNodeNext;
        }

        private int CalculateMaxCarsForLanes()
        {
            // Get the length of the car
            int _maxCarsTemp = 0;
            _maxCars = 100000;

            // Loop through all roads
            for (int i = 0; i < _roadSystem.RoadCount; i++)
            {
                _roads[i].OnChange();
                float laneLength;
                LaneNode _laneNodeTemp1;
                LaneNode _laneNodeTemp2;
                
                // Loop through all lanes
                for (int j = 0; j < _roads[i].LaneCount; j++)
                {
                    _laneNodeTemp1 = _roads[i].Lanes[j].StartNode;
                    _laneNodeTemp2 = _roads[i].Lanes[j].StartNode.Next;
                    laneLength = 0;

                    // Loop through all lane nodes
                    for (int k = 0; k < _roads[i].Lanes[j].StartNode.Count - 1; k++)
                    {
                        // Calculate the length of the lane
                        laneLength = laneLength + Vector3.Distance(_laneNodeTemp1.Position, _laneNodeTemp2.Position);
                        _laneNodeTemp1 = _laneNodeTemp1.Next;
                        _laneNodeTemp2 = _laneNodeTemp2.Next;
                    }
                    _maxCarsTemp = ((int)laneLength / (int)carLength);
                    _maxCars = Mathf.Min(_maxCarsTemp, _maxCars);
                }
            }
            return _maxCars;
        }
    }
}
