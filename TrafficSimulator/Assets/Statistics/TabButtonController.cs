using UnityEngine;
using UnityEngine.UI;
using RoadGenerator;

namespace Statistics
{
    public class TabButtonController : MonoBehaviour
    {
        [SerializeField] private Toggle _congestionColorToggle;
        [SerializeField] private Toggle _emissionColorToggle;
        
        private RoadSystem _roadSystem;
        private RoadColorManager _roadColorManager;

        private void Start()
        {
            GameObject roadSystemObject = GameObject.Find("RoadSystem");
            _roadSystem = roadSystemObject.GetComponent<RoadSystem>();
            _roadColorManager = roadSystemObject.GetComponent<RoadColorManager>();
        }

        public void SetGatheredStatisticsNone()
        {
            _roadSystem.CurrentStatisticsGathered = StatisticsType.None;
            _emissionColorToggle.isOn = false;
            _congestionColorToggle.isOn = false;
            _roadColorManager.UpdateRoadColorEmissionType();
        }

        public void SetGatheredStatisticsEmissions()
        {
            _roadSystem.CurrentStatisticsGathered = StatisticsType.Emissions;
            _emissionColorToggle.isOn = _roadColorManager.ShowRoadColors;
            _roadColorManager.UpdateRoadColorEmissionType();
        }

        public void SetGatheredStatisticsCongestion()
        {
            _roadSystem.CurrentStatisticsGathered = StatisticsType.Congestion;
            _congestionColorToggle.isOn = _roadColorManager.ShowRoadColors;
            _roadColorManager.UpdateRoadColorEmissionType();
        }
    }
}
