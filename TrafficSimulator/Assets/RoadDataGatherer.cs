using System;
using System.Collections.Generic;
using UnityEngine;
using VehicleBrain;

public class RoadDataGatherer : MonoBehaviour
{
    // A list to store the registered vehicles
    private List<GameObject> _registeredVehicles;
    [field: SerializeField] public float CurrentFuelConsumption { get; private set; }
    [SerializeField] private float _totalFuelConsumption;
    

    private void Awake()
    {
        _registeredVehicles = new List<GameObject>();
    }

    public void RegisterVehicle(GameObject vehicle)
    {
        if (!_registeredVehicles.Contains(vehicle))
        {
            _registeredVehicles.Add(vehicle);
            Debug.Log("Vehicle registered: " + vehicle.name);
        }
    }

    public void UnregisterVehicle(GameObject vehicle)
    {
        if (_registeredVehicles.Contains(vehicle))
        {
            _registeredVehicles.Remove(vehicle);
            Debug.Log("Vehicle unregistered: " + vehicle.name);
        }
    }

    private void Update()
    {
        CurrentFuelConsumption = 0;
        foreach (GameObject vehicle in _registeredVehicles)
        {
            CurrentFuelConsumption += vehicle.GetComponent<FuelConsumption>().FuelConsumedSinceLastFrame;
        }

        _totalFuelConsumption += CurrentFuelConsumption;
    }
}