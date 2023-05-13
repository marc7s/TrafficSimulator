using RoadGenerator;
using UnityEngine;
using VehicleBrain;

namespace Statistics
{
    [RequireComponent(typeof(AutoDrive))]
    public class RoadRegistrator : MonoBehaviour
    {
        private AutoDrive _autoDrive;
        private Road _currentRoad;
        
        void Start()
        {
            _autoDrive = GetComponent<AutoDrive>();
            _autoDrive.RoadChanged += OnRoadChanged;
            OnRoadChanged();
        }
        
        public void OnRoadChanged()
        {
            if (_currentRoad != null)
                _currentRoad.GetComponent<RoadDataGatherer>().UnregisterVehicle(gameObject);

            _currentRoad = _autoDrive.Agent.Context.CurrentRoad;

            if(_currentRoad != null)
                _currentRoad.GetComponent<RoadDataGatherer>().RegisterVehicle(gameObject);
        }
    }
}