using UnityEngine;
using UnityEngine.UIElements;
using VehicleBrain;

namespace UI
{
    public class StatsController : MonoBehaviour
    {
        private UIDocument _doc;

        // Labels
        private Label _totalDistanceTraveled;
        private Label _fuelConsumed;
        private Label _avgFuelConsumption;

        // Progress Bar
        private ProgressBar _fuelBar;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();

            // Labels
            _totalDistanceTraveled = _doc.rootVisualElement.Q<Label>("TotalDistanceTraveled");
            _fuelConsumed = _doc.rootVisualElement.Q<Label>("FuelConsumed");
            _avgFuelConsumption = _doc.rootVisualElement.Q<Label>("AvgFuelConsumption");

            // Progress Bar
            _fuelBar = _doc.rootVisualElement.Q<ProgressBar>("FuelBar");
            _fuelBar.title = "";
        }
        
        public void UpdateInfo(AutoDrive car, FuelConsumption fuelConsumption)
        {
            if (car == null)
            {
                ResetInfo();
                return;
            }
            float totalDistance = car.TotalDistance/1000;
            float totalFuelConsumed = (float)fuelConsumption._totalFuelConsumed;
            _totalDistanceTraveled.text = totalDistance.ToString("0.00") + " Km";
            _fuelConsumed.text = totalFuelConsumed.ToString("0.00") + " L";
            _avgFuelConsumption.text = (totalFuelConsumed/totalDistance).ToString("0.00") + " L/100km";
            _fuelBar.highValue = fuelConsumption._maxFuelCapacity;
            _fuelBar.value = fuelConsumption._maxFuelCapacity - totalFuelConsumed;
            _fuelBar.tooltip = (fuelConsumption._maxFuelCapacity - totalFuelConsumed).ToString("0.0") + "/" + fuelConsumption._maxFuelCapacity.ToString("0.0") + " L"; // TODO - Fix this
        }

        public void ResetInfo()
        {
            _totalDistanceTraveled.text = "N/A";
            _fuelConsumed.text = "N/A";
            _avgFuelConsumption.text = "N/A";
            _fuelBar.highValue = 0;
            _fuelBar.value = 0;
        }
    }
}