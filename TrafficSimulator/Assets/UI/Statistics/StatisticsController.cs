using UnityEngine;
using UnityEngine.UIElements;
using User;
using Car;

namespace UI
{
    public class StatisticsController : MonoBehaviour
    {
        private enum Tabs
        {
            None,
            Stats,
            Info,
            Settings
        }

        private UIDocument _doc;
        private UserSelectManager _userSelectManager;
        private Selectable _selectedCar;
        [SerializeField] private StatsController _statsController;
        [SerializeField] private InfoController _infoController;
        [SerializeField] private SettingsController _settingsController;

        private OverlayController _overlayController;

        private Tabs _tabs;

        // Tab Buttons
        private Button _statsButton;
        private Button _infoButton;
        private Button _settingsButton;

        // Panels
        private VisualElement _statsPanel;
        private VisualElement _infoPanel;
        private VisualElement _settingsPanel;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _overlayController = GameObject.Find("UIOverlay").GetComponent<OverlayController>();

            // Buttons
            _statsButton = _doc.rootVisualElement.Q<Button>("StatsButton");
            _statsButton.clicked += StatsButtonOnClicked;

            _infoButton = _doc.rootVisualElement.Q<Button>("InfoButton");
            _infoButton.clicked += InfoButtonOnClicked;

            _settingsButton = _doc.rootVisualElement.Q<Button>("SettingsButton");
            _settingsButton.clicked += SettingsButtonOnClicked;

            // Panels
            _statsPanel = _doc.rootVisualElement.Q<VisualElement>("StatsPanel");
            _infoPanel = _doc.rootVisualElement.Q<VisualElement>("InfoPanel");
            _settingsPanel = _doc.rootVisualElement.Q<VisualElement>("SettingsPanel");
            // Set the default tab
            _tabs = Tabs.None;

        }

        void Start()
        {
            _userSelectManager = UserSelectManager.Instance;
        }

        private void StatsButtonOnClicked()
        {
            _tabs = Tabs.Stats;
        }

        private void InfoButtonOnClicked()
        {
            _tabs = Tabs.Info;
        }
        
        private void SettingsButtonOnClicked()
        {
            _tabs = Tabs.Settings;
        }

        private void UpdatePanels()
        {
            switch(_tabs)
            {
                case Tabs.None:
                    _statsPanel.visible = false;
                    _infoPanel.visible = false;
                    _settingsPanel.visible = false;
                    break;
                case Tabs.Stats:
                    _statsPanel.visible = true;
                    _infoPanel.visible = false;
                    _settingsPanel.visible = false;
                    break;
                case Tabs.Info:
                    _statsPanel.visible = false;
                    _infoPanel.visible = true;
                    _settingsPanel.visible = false;
                    break;
                case Tabs.Settings:
                    _statsPanel.visible = false;
                    _infoPanel.visible = false;
                    _settingsPanel.visible = true;
                    break;
            }
        }

        private void UpdateCarInfo()
        {
            // Get the selected car (returns null if no car is selected)
            _selectedCar = _userSelectManager.SelectedGameObject;
            AutoDrive car = _selectedCar?.gameObject.GetComponent<AutoDrive>();
            FuelConsumption fuelConsumption = _selectedCar?.gameObject.GetComponent<FuelConsumption>();

            _statsController.UpdateInfo(car, fuelConsumption);
        }

        // Update is called once per frame
        void Update()
        {
            if(!_overlayController._isStatisticsOpen)
            {
                _tabs = Tabs.None;
            } else if (_overlayController._isStatisticsOpen && _tabs == Tabs.None)
            {
                _tabs = Tabs.Stats;
            }

            UpdatePanels();
            UpdateCarInfo();
        }
    }
}