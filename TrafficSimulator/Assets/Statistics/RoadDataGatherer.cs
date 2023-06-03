using System.Collections.Generic;
using CustomProperties;
using UnityEngine;
using VehicleBrain;
using RoadGenerator;

namespace Statistics
{
    public class RoadDataGatherer : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField][ReadOnly] private float _totalFuelConsumption;
    
        private List<GameObject> _registeredVehicles;
        private Road _road;
        private float _totalRoadLength;

        private WorldDataGatherer _worldDataGatherer;
        public float CurrentFuelConsumption { get; private set; }
        public float CurrentFuelConsumptionRatio { get; private set; }
        public float CurrentCongestionRatio { get; private set; }

        // The average distance between vehicles for the road to be considered fully congested
        // Note that this distance is in each lane
        private static int _congestionRatioCoef = 20;

        // The average fuel consumption for the road to be considered consuming fuel at full capacity
        // The calculation is based on a fully congested road, where each vehicle consumes 0.9 L/h
        // For reference, an idling vehicle consumes around 0.8 L/h
        private static float _fuelConsumptionRatioCoef = _congestionRatioCoef * 0.9f * 3600;
    
        private void Awake()
        {
            _registeredVehicles = new List<GameObject>();
        }

        private void Start()
        {
            _worldDataGatherer = GetComponentInParent<WorldDataGatherer>();
            _road = GetComponent<Road>();

            if(_road != null)
                _totalRoadLength = _road.Length * (int)_road.LaneAmount * 2;
        }

        private void Update()
        {
            CurrentFuelConsumption = 0;
            
            foreach (GameObject vehicle in _registeredVehicles)
                CurrentFuelConsumption += vehicle.GetComponent<FuelConsumption>().FuelConsumedSinceLastFrame;

            CurrentFuelConsumptionRatio = Mathf.Clamp01(CurrentFuelConsumption * (1 / Time.deltaTime) *  _fuelConsumptionRatioCoef / _totalRoadLength);
            CurrentCongestionRatio = Mathf.Clamp01(_registeredVehicles.Count * _congestionRatioCoef / _totalRoadLength);
            
            _totalFuelConsumption += CurrentFuelConsumption;
            _worldDataGatherer.AddFuelConsumed(_road.ID, CurrentFuelConsumption);
            _worldDataGatherer.AddCongestion(_road.ID, _registeredVehicles.Count, _totalRoadLength, _congestionRatioCoef);
        }

        public void RegisterVehicle(GameObject vehicle)
        {
            if (!_registeredVehicles.Contains(vehicle))
                _registeredVehicles.Add(vehicle);
        }

        public void UnregisterVehicle(GameObject vehicle)
        {
            if (_registeredVehicles.Contains(vehicle))
                _registeredVehicles.Remove(vehicle);
        }
    }
}