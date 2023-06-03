using UnityEngine;

namespace Old_UI
{
    public class CarbonDioxideTextDisplayer : MonoBehaviour
    {
        [SerializeField] private GraphChartFeed _emissionGraphChartFeed;
        [SerializeField] private InfoField _carbonDioxideEstimateField;
        private WorldDataGatherer _worldDataGatherer;

        private void Start()
        {
            _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
            _worldDataGatherer.OnNewStatisticsSample += UpdateCO2;
            _emissionGraphChartFeed.OnTimeSpanChanged += UpdateCO2;
        }

        private void UpdateCO2()
        {
            switch(_emissionGraphChartFeed.CurrentTimeSpan)
            {
                case TimeSpan.AllTime:
                    DisplayCO2(_worldDataGatherer.Co2EmissionsAllTime);
                    break;
                case TimeSpan.ThreeMinutes:
                    DisplayCO2(_worldDataGatherer.Co2EmissionsLast3Min);
                    break;
                case TimeSpan.ThirtySeconds:
                    DisplayCO2(_worldDataGatherer.Co2EmissionsLast30Sec);
                    break;
            }
        }

        private void DisplayCO2(float co2Amount)
        {
            _carbonDioxideEstimateField.Display(co2Amount, "kg", 3);
        }
    }
}