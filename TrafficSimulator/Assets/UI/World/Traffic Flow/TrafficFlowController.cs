using UnityEngine;
using UnityEngine.UIElements;
using VehicleBrain;

namespace UI
{
    public class TrafficFlowController : MonoBehaviour
    {
        private UIDocument _doc;

        // Labels
        private Label _cars;
        private Label _bus;
        private Label _average;

        // Visual Elements
        private VisualElement _graph;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            
            // Labels
            _cars = _doc.rootVisualElement.Q<Label>("CarPercentage");
            _bus = _doc.rootVisualElement.Q<Label>("BusPercentage");
            _average = _doc.rootVisualElement.Q<Label>("AveragePercentage");

            // Visual Elements
            _graph = _doc.rootVisualElement.Q<VisualElement>("Graph");
            
        }
        
        public void UpdateInfo() // Parameter should get information from the statistics interface
        {
            if (true)
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