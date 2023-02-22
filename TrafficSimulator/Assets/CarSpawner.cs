using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vehicle = DataModel.Vehicle;


namespace RoadGenerator
{
    public class CarSpawner : MonoBehaviour
    {
        
        [SerializeField] private GameObject carPrefab;
        //[SerializeField] private GameObject roadSystem;
        [SerializeField] private Road road;

        public int maxCars = 5;
        public float distance = 10f;

        private RoadSystem _roadSystem;

        private LaneNode _laneNodeStart;
        private LaneNode _laneNodeCurrent;
        private LaneNode _laneNodeNext;

        private LaneNode _laneNodeTemp;

        public Vehicle _vehicle;

        

        private void Start()
        {
            road.OnChange();
            Lane lane = road.Lanes[0];
            _laneNodeStart = lane.StartNode;
            _laneNodeCurrent = _laneNodeStart;
            _vehicle = new Vehicle("Car", carPrefab);

            SpawnCars(maxCars);
        }

        private void SpawnCars(int maxCars)
        {
            for (int i = 0; i < maxCars; i++)
            {
                Instantiate(carPrefab, _laneNodeCurrent.Position, _laneNodeCurrent.Rotation);
                setNodeUnderVehicle();
                calculateOffset(distance);
            }
        }

        private void setNodeUnderVehicle()
        {
            float carLength = carPrefab.GetComponentInChildren<MeshCollider>().bounds.size.z;
            _laneNodeNext = _laneNodeCurrent;

            _laneNodeCurrent.SetVehicle(_vehicle);

            while (carLength > Vector3.Distance(_laneNodeCurrent.Position, _laneNodeNext.Position))
            {
                _laneNodeNext = _laneNodeNext.Next;
                _laneNodeNext.SetVehicle(_vehicle);
            }
        }

        private void calculateOffset(float distance)
        {
            _laneNodeTemp = _laneNodeCurrent;
            while (_laneNodeTemp.Vehicle != null)
            {
                _laneNodeTemp = _laneNodeTemp.Next;
            }
            _laneNodeNext = _laneNodeTemp;
            while (distance > Vector3.Distance(_laneNodeTemp.Position, _laneNodeNext.Position))
            {
                _laneNodeNext = _laneNodeNext.Next;
            }
            _laneNodeCurrent = _laneNodeNext;
        }
    }
}