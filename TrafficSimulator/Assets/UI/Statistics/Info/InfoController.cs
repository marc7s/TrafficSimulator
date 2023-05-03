using UnityEngine;
using UnityEngine.UIElements;
using VehicleBrain;

namespace UI
{
    public class InfoController : MonoBehaviour
    {
        private UIDocument _doc;

        // Labels
        private Label _totalTimeOnRoad;
        private Label _totalDistanceTraveled;

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