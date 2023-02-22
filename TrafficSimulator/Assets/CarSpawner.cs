using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vehicle = DataModel.Vehicle;


namespace RoadGenerator
{
    public class CarSpawner : MonoBehaviour
    {
        
        [SerializeField] private GameObject carPrefab;
        [SerializeField] private GameObject roadSystem;

        public int maxCars = 5; // Number of cars to spawn per lane
        public float distance = 10f; // Distance between cars

        private RoadSystem _roadSystem;
        private List<Road> _roads;

        private LaneNode _laneNodeCurrent;
        private LaneNode _laneNodeNext;

        private LaneNode _laneNodeTemp;

        public Vehicle _vehicle;

        
        private void Start()
        {
            _roadSystem = roadSystem.GetComponent<RoadSystem>();
            _roadSystem.Setup();

            _roads = _roadSystem.Roads;
            _vehicle = new Vehicle("Car", carPrefab);

            SpawnCars(maxCars);
        }

        private void SpawnCars(int maxCars)
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
                    _laneNodeCurrent = lane.StartNode;

                    // Spawn cars
                    for (int k = 0; k < maxCars; k++)
                    {
                        // Spawn individual car at current node
                        Instantiate(carPrefab, _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);

                        // Tell the nodes under car that they are occupied
                        setNodeUnderVehicle();

                        // Calculate next spawning node
                        calculateOffset(distance);
                    }
                }
            }
        }

        private void setNodeUnderVehicle()
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

        private void calculateOffset(float distance)
        {
            _laneNodeTemp = _laneNodeCurrent;

            // Get the next node that is not occupied
            while (_laneNodeTemp.Vehicle != null)
            {
                _laneNodeTemp = _laneNodeTemp.Next;
            }
            _laneNodeNext = _laneNodeTemp;

            // Get the next node that is far enough away from the first unoccupied node
            while (distance > Vector3.Distance(_laneNodeTemp.Position, _laneNodeNext.Position))
            {
                _laneNodeNext = _laneNodeNext.Next;
            }

            // Set the current node to that node
            _laneNodeCurrent = _laneNodeNext;
        }
    }
}