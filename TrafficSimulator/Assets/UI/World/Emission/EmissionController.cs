using UnityEngine;
using UnityEngine.UIElements;
using Car;

namespace UI
{
    public class EmissionController : MonoBehaviour
    {
        private UIDocument _doc;

        // Labels
        private Label _carsPercent;
        private Label _carsUnits;
        private Label _busPercent;
        private Label _busUnits;
        private Label _averagePercent;
        private Label _averageUnits;
        
        // Visual Elements
        private VisualElement _graph;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            
            // Labels
            _carsPercent = _doc.rootVisualElement.Q<Label>("CarEmissionPercentage");
            _busPercent = _doc.rootVisualElement.Q<Label>("BusEmissionPercentage");
            _averagePercent = _doc.rootVisualElement.Q<Label>("AverageEmissionPercentage");
            _carsUnits = _doc.rootVisualElement.Q<Label>("CarEmissionUnits");
            _busUnits = _doc.rootVisualElement.Q<Label>("BusEmissionUnits");
            _averageUnits = _doc.rootVisualElement.Q<Label>("AverageEmissionUnits");

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