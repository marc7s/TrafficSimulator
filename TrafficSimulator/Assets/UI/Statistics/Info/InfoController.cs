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
        private Label _location;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();

            // Labels
            _location = _doc.rootVisualElement.Q<Label>("Location");
            
        }
        
        public void UpdateInfo(AutoDrive car)
        {
            if (car == null)
            {
                ResetInfo();
                return;
            }
            _location.text = "Location: " + car.Agent.Context.CurrentRoad.name;
        }

        public void ResetInfo()
        {
            _location.text = "Location: N/A";
        }
    }
}