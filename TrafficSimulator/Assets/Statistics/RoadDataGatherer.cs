using System.Collections.Generic;
using UnityEngine;
using VehicleBrain;
using CustomProperties;

public class RoadDataGatherer : MonoBehaviour
{
    [Header("Connections")]
    [SerializeField][ReadOnly] private float _totalFuelConsumption;
    
    // A list to store the registered vehicles
    private List<GameObject> _registeredVehicles;
    [SerializeField]public float CurrentFuelConsumption { get; private set; }
    
    private void Awake()
    {
        _registeredVehicles = new List<GameObject>();
    }

    private void Update()
    {
        CurrentFuelConsumption = 0;
        foreach (GameObject vehicle in _registeredVehicles)
            CurrentFuelConsumption += vehicle.GetComponent<FuelConsumption>().FuelConsumedSinceLastFrame;

        _totalFuelConsumption += CurrentFuelConsumption;
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