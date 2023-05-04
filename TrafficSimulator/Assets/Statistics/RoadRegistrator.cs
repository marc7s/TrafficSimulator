using System.Collections;
using System.Collections.Generic;

using RoadGenerator;
using UnityEngine;
using VehicleBrain;

public class RoadRegistrator : MonoBehaviour
{
    private AutoDrive _autoDrive;
    private Road _currentRoad;
    void Awake()
    {
        _autoDrive = GetComponent<AutoDrive>();
        _autoDrive.OnRoadChanged += OnRoadChanged;
    }
    
    public void OnRoadChanged(Road newRoad)
    {
        if (_currentRoad != null)
        {
            _currentRoad.GetComponent<RoadDataGatherer>().UnregisterVehicle(gameObject);
        }

        _currentRoad = newRoad;
        _currentRoad.GetComponent<RoadDataGatherer>().RegisterVehicle(gameObject);
    }
}