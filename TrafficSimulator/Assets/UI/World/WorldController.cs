using UnityEngine;
using UnityEngine.UIElements;
using User;
using Car;

namespace UI
{
    public class WorldController : MonoBehaviour
    {
        private enum Tabs
        {
            None,
            TrafficFlow,
            Emission,
            Vehicles
        }

        private UIDocument _doc;
        //private UserSelectManager _userSelectManager;
        //private Selectable _selectedCar;
        [SerializeField] private TrafficFlowController _trafficFlowController;
        [SerializeField] private EmissionController _emissionController;
        [SerializeField] private VehiclesController _vehiclesController;

        private OverlayController _overlayController;

        private Tabs _tabs;

        // Tab Buttons
        private Button _trafficFlowButton;
        private Button _emissionButton;
        private Button _vehiclesButton;

        // Panels
        private VisualElement _trafficFlowPanel;
        private VisualElement _emissionPanel;
        private VisualElement _vehiclesPanel;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _overlayController = GameObject.Find("UIOverlay").GetComponent<OverlayController>();

            // Buttons
            _trafficFlowButton = _doc.rootVisualElement.Q<Button>("TrafficFlowButton");
            _trafficFlowButton.clicked += TrafficFlowButtonOnClicked;

            _emissionButton = _doc.rootVisualElement.Q<Button>("EmissionButton");
            _emissionButton.clicked += EmissionButtonOnClicked;

            _vehiclesButton = _doc.rootVisualElement.Q<Button>("VehiclesButton");
            _vehiclesButton.clicked += VehiclesButtonOnClicked;

            // Panels
            _trafficFlowPanel = _doc.rootVisualElement.Q<VisualElement>("TrafficFlowPanel");
            _emissionPanel = _doc.rootVisualElement.Q<VisualElement>("EmissionPanel");
            _vehiclesPanel = _doc.rootVisualElement.Q<VisualElement>("VehiclesPanel");
            // Set the default tab
            _tabs = Tabs.None;

        }

        void Start()
        {
            //_userSelectManager = UserSelectManager.Instance;
        }

        private void TrafficFlowButtonOnClicked()
        {
            _tabs = Tabs.TrafficFlow;
        }

        private void EmissionButtonOnClicked()
        {
            _tabs = Tabs.Emission;
        }
        
        private void VehiclesButtonOnClicked()
        {
            _tabs = Tabs.Vehicles;
        }

        private void UpdatePanels()
        {
            Length MinHeight = new Length(25, LengthUnit.Pixel);
            Length MaxHeight = new Length(30, LengthUnit.Pixel);
            _trafficFlowButton.style.height = MinHeight;
            _emissionButton.style.height = MinHeight;
            _vehiclesButton.style.height = MinHeight;
            _trafficFlowButton.RemoveFromClassList("panel-button-selected");
            _emissionButton.RemoveFromClassList("panel-button-selected");
            _vehiclesButton.RemoveFromClassList("panel-button-selected");
            switch(_tabs)
            {
                case Tabs.None:
                    _trafficFlowPanel.visible = false;
                    _emissionPanel.visible = false;
                    _vehiclesPanel.visible = false;
                    break;
                case Tabs.TrafficFlow:
                    _trafficFlowPanel.visible = true;
                    _trafficFlowButton.style.height = MaxHeight;
                    _trafficFlowButton.AddToClassList("panel-button-selected");
                    _emissionPanel.visible = false;
                    _vehiclesPanel.visible = false;
                    break;
                case Tabs.Emission:
                    _trafficFlowPanel.visible = false;
                    _emissionPanel.visible = true;
                    _emissionButton.style.height = MaxHeight;
                    _emissionButton.AddToClassList("panel-button-selected");
                    _vehiclesPanel.visible = false;
                    break;
                case Tabs.Vehicles:
                    _trafficFlowPanel.visible = false;
                    _emissionPanel.visible = false;
                    _vehiclesPanel.visible = true;
                    _vehiclesButton.style.height = MaxHeight;
                    _vehiclesButton.AddToClassList("panel-button-selected");
                    break;
            }
        }


        // Update is called once per frame
        void Update()
        {
            if(!_overlayController._isWorldOpen)
            {
                _tabs = Tabs.None;
            } else if (_overlayController._isWorldOpen && _tabs == Tabs.None)
            {
                _tabs = Tabs.TrafficFlow;
            }

            UpdatePanels();
        }
    }
}