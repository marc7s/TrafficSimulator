using UnityEngine;
using UnityEngine.UIElements;
using VehicleBrain;

namespace UI
{
    public class VehiclesController : MonoBehaviour
    {
        private UIDocument _doc;

        // Labels
        private Label _carsPercent;
        private Label _carsUnits;
        private Label _busPercent;
        private Label _busUnits;
        private Label _totalUnits;
        
        // Visual Elements
        private VisualElement _graph;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            
            // Labels
            _carsPercent = _doc.rootVisualElement.Q<Label>("CarPercentage");
            _busPercent = _doc.rootVisualElement.Q<Label>("BusPercentage");
            _carsUnits = _doc.rootVisualElement.Q<Label>("CarUnits");
            _busUnits = _doc.rootVisualElement.Q<Label>("BusUnits");
            _totalUnits = _doc.rootVisualElement.Q<Label>("TotalUnits");

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