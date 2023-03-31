using UnityEngine;
using UnityEngine.UIElements;
using User;
using Car;

public class StatsController : MonoBehaviour
{
    private UIDocument _doc;

    // Labels
    private Label _fuelConsumption;
    private Label _timeInTraffic;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();


        
    }
    
    public void UpdateInfo(AutoDrive car)
    {
        if (car == null)
        {
            ResetInfo();
            return;
        }

    }

    public void ResetInfo()
    {

    }
}
