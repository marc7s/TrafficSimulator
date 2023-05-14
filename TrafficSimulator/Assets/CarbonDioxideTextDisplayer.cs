using TMPro;
using UnityEngine;

public class CarbonDioxideTextDisplayer : MonoBehaviour
{
    private enum CO2DisplayTimeSpan
    {
        AllTime,
        ThreeMinutes,
        ThirtySeconds
    }

    private const string DefaultText = "Estimated CO<sup>2</sup>: ";
    private const string Units = " kg";
    private TextMeshProUGUI _carbonDioxideEstimateText;
    private WorldDataGatherer _worldDataGatherer;
    private CO2DisplayTimeSpan _currentDisplay;

    private void Start()
    {
        _carbonDioxideEstimateText = GetComponent<TextMeshProUGUI>();
        _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
        DisplayAllTime();
    }

    private void DisplayCO2(float co2Amount)
    {
        _carbonDioxideEstimateText.text = DefaultText + co2Amount.ToString("0.0000") + Units;
    }

    public void DisplayAllTime()
    {
        if (_currentDisplay == CO2DisplayTimeSpan.AllTime) return;
        DisplayCO2(_worldDataGatherer.Co2EmissionsAllTime);
        _currentDisplay = CO2DisplayTimeSpan.AllTime;
    }

    public void Display3Min()
    {
        if (_currentDisplay == CO2DisplayTimeSpan.ThreeMinutes) return;
        DisplayCO2(_worldDataGatherer.Co2EmissionsLast3Min);
        _currentDisplay = CO2DisplayTimeSpan.ThreeMinutes;
    }

    public void Display30Seconds()
    {
        if (_currentDisplay == CO2DisplayTimeSpan.ThirtySeconds) return;
        DisplayCO2(_worldDataGatherer.Co2EmissionsLast30Sec);
        _currentDisplay = CO2DisplayTimeSpan.ThirtySeconds;
    }
}