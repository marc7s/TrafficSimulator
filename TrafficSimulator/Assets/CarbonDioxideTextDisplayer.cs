using TMPro;
using UnityEngine;

public class CarbonDioxideTextDisplayer : MonoBehaviour
{
    private enum CO2DisplayTimeSpan
    {
        None,
        AllTime,
        ThreeMinutes,
        ThirtySeconds
    }
    [SerializeField] private InfoField _carbonDioxideEstimateField;
    private WorldDataGatherer _worldDataGatherer;
    private CO2DisplayTimeSpan _currentDisplay = CO2DisplayTimeSpan.None;
    private bool _isSetup = false;

    private void Start()
    {
        _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
        _worldDataGatherer.OnNewStatisticsSample += UpdateCO2;
        _isSetup = true;
    }

    private void UpdateCO2()
    {
        switch(_currentDisplay)
        {
            case CO2DisplayTimeSpan.AllTime:
                DisplayCO2(_worldDataGatherer.Co2EmissionsAllTime);
                break;
            case CO2DisplayTimeSpan.ThreeMinutes:
                DisplayCO2(_worldDataGatherer.Co2EmissionsLast3Min);
                break;
            case CO2DisplayTimeSpan.ThirtySeconds:
                DisplayCO2(_worldDataGatherer.Co2EmissionsLast30Sec);
                break;
        }
    }

    private void DisplayCO2(float co2Amount)
    {
        _carbonDioxideEstimateField.Display(co2Amount, "kg", 3);
    }
    
    public void SetDisplayTimeToNone()
    {
        _currentDisplay = CO2DisplayTimeSpan.None;
    }

    public void DisplayAllTime()
    {
        if (_currentDisplay == CO2DisplayTimeSpan.AllTime) 
            return;

        if(!_isSetup)
            Start();
        
        DisplayCO2(_worldDataGatherer.Co2EmissionsAllTime);
        _currentDisplay = CO2DisplayTimeSpan.AllTime;
    }

    public void Display3Min()
    {
        if (_currentDisplay == CO2DisplayTimeSpan.ThreeMinutes) 
            return;

        if(!_isSetup)
            Start();
        
        DisplayCO2(_worldDataGatherer.Co2EmissionsLast3Min);
        _currentDisplay = CO2DisplayTimeSpan.ThreeMinutes;
    }

    public void Display30Seconds()
    {
        if (_currentDisplay == CO2DisplayTimeSpan.ThirtySeconds) 
            return;

        if(!_isSetup)
            Start();
        
        DisplayCO2(_worldDataGatherer.Co2EmissionsLast30Sec);
        _currentDisplay = CO2DisplayTimeSpan.ThirtySeconds;
    }
}