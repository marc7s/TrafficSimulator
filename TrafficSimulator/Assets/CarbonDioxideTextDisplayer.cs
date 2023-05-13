using TMPro;
using UnityEngine;

public class CarbonDioxideTextDisplayer : MonoBehaviour
{
    private const string DefaultText = "Estimated CO<sup>2</sup>: ";
    private const string Units = " kg";
    private TextMeshProUGUI _carbonDioxideEstimateText;
    private WorldDataGatherer _worldDataGatherer;

    private void Start()
    {
        _carbonDioxideEstimateText = GetComponent<TextMeshProUGUI>();
        _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
    }

    private void DisplayCO2(float co2Amount)
    {
        _carbonDioxideEstimateText.text = DefaultText + co2Amount.ToString("0.0000") + Units;
    }

    public void DisplayAllTime()
    {
        DisplayCO2(_worldDataGatherer.Co2EmissionsAllTime);
    }

    public void Display3Min()
    {
        DisplayCO2(_worldDataGatherer.Co2EmissionsLast3Min);
    }

    public void Display30Seconds()
    {
        DisplayCO2(_worldDataGatherer.Co2EmissionsLast30Sec);
    }
}