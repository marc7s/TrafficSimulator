using UnityEngine;
using UnityEngine.UIElements;
using Car;

namespace UI
{
    public class StatsController : MonoBehaviour
    {
        private UIDocument _doc;

        // Labels
        private Label _fuelConsumption;
        private Label _timeInTraffic;

        void Awake()
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
}