using System.Collections.Generic;
using CustomProperties;
using UnityEngine;
using VehicleBrain;

namespace Statistics
{
    public class RoadDataGatherer : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField][ReadOnly] private float _totalFuelConsumption;
    
        private List<GameObject> _registeredVehicles;

        private WorldDataGatherer _worldDataGatherer;
        public float CurrentFuelConsumption { get; private set; }
    
        private void Awake()
        {
            _registeredVehicles = new List<GameObject>();
        }

        private void Start()
        {
            _worldDataGatherer = GetComponentInParent<WorldDataGatherer>();
        }

        private void Update()
        {
            CurrentFuelConsumption = 0;
            foreach (GameObject vehicle in _registeredVehicles)
                CurrentFuelConsumption += vehicle.GetComponent<FuelConsumption>().FuelConsumedSinceLastFrame;

            _totalFuelConsumption += CurrentFuelConsumption;
            _worldDataGatherer.AddFuelConsumed(CurrentFuelConsumption);
            _worldDataGatherer.TotalFuelConsumed += CurrentFuelConsumption;
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