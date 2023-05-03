using UnityEngine;
using UnityEngine.UIElements;
using VehicleBrain;

namespace UI
{
    public class SettingsController : MonoBehaviour
    {
        private UIDocument _doc;

        // Labels
        private Label _mode;
        private Label _maxSpeed;

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