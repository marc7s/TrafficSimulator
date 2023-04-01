using UnityEngine;
using UnityEngine.UIElements;
using Car;

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

            // Labels
            _totalTimeOnRoad = _doc.rootVisualElement.Q<Label>("TotalTimeOnRoad");
            _totalDistanceTraveled = _doc.rootVisualElement.Q<Label>("TotalDistanceTraveled");
            
        }
        
        public void UpdateInfo(AutoDrive car)
        {
            if (car == null)
            {
                ResetInfo();
                return;
            }
            _totalDistanceTraveled.text = car.TotalDistance.ToString("0.00") + " m";
        }

        public void ResetInfo()
        {
            _totalTimeOnRoad.text = "N/A";
            _totalDistanceTraveled.text = "N/A";
        }
    }
}