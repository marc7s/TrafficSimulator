using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
        
    public class StatisticsController : MonoBehaviour
    {
        private UIDocument _doc;

        private OverlayController _overlayController;
        private enum Tabs
        {
            None,
            Stats,
            Info,
            Settings
        }

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

        private void StatsButtonOnClicked()
        {
            _tabs = Tabs.Stats;
            Debug.Log("Stats");
        }
        private void InfoButtonOnClicked()
        {
            _tabs = Tabs.Info;
            Debug.Log("Info");
        }
        private void SettingsButtonOnClicked()
        {
            _tabs = Tabs.Settings;
            Debug.Log("Settings");
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
        }
    }
}