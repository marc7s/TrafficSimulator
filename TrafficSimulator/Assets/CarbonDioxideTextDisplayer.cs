using TMPro;
using UnityEngine;

public class CarbonDioxideTextDisplayer : MonoBehaviour
{
    private const string DefaultText = "Estimated CO<sup>2</sup>: ";
    private TextMeshProUGUI _carbonDioxideEstimateText;
    private WorldDataGatherer _worldDataGatherer;

    private void Start()
    {
        _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
    }

    public void DisplayAllTime()
    {
        _carbonDioxideEstimateText.text = DefaultText + _worldDataGatherer.Co2EmissionsAllTime.ToString("0.0000");
    }

    public void Display3Min()
    {
        _carbonDioxideEstimateText.text = DefaultText + _worldDataGatherer.Co2EmissionsAllTime.ToString("0.0000");
    }

    public void Display30Seconds()
    {
        _carbonDioxideEstimateText.text = DefaultText + _worldDataGatherer.Co2EmissionsAllTime.ToString("0.0000");
    }
}