using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class MenuController : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _displayedContainer;
        private OverlayController _overlayController;

        // Start Menu UI
        private VisualElement _settingsContainer;
        private VisualElement _startMenuContainer;
        private Button _startButton;
        private Button _settingsButton;
        private Button _exitButton;

        //  Settings UI
        [SerializeField] private VisualTreeAsset _settingsUI;
        private Button _settingsBackButton;
        private Toggle _fpsToggle;

        private Toggle _fullscreenToggle;
        private DropdownField _graphicsQualityDropdown;
        private Slider _masterVolumeSlider;
        private Slider _fovSlider;
        private Label _fpsLabel;
        
        private bool _showFPS = false;

        private const int _fpsUpdateFrequency = 15;
        private float _fpsLastUpdateTime = 0;

        private const string FPS_COUNTER = "FPSCounter";
        private const string FULLSCREEN = "Fullscreen";


        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            // Set the displayed container to the start menu
            _displayedContainer = _doc.rootVisualElement.Q<VisualElement>("MenuDisplay");

            // Get the overlay controller
            _overlayController = GameObject.Find("UIOverlay").GetComponent<OverlayController>();

            // Start Menu UI
            _startMenuContainer = _doc.rootVisualElement.Q<VisualElement>("StartMenuButtons");

            _startButton = _doc.rootVisualElement.Q<Button>("StartButton");
            _startButton.clicked += StartButtonOnClicked;
            
            _settingsButton = _doc.rootVisualElement.Q<Button>("SettingsButton");
            _settingsButton.clicked += SettingsButtonOnClicked;
            
            _exitButton = _doc.rootVisualElement.Q<Button>("ExitButton");
            _exitButton.clicked += ExitButtonOnClicked;
            
            _fpsLabel = _doc.rootVisualElement.Q<Label>("FPSLabel");

            // Settings UI
            _settingsContainer = _settingsUI.CloneTree();
            
            _settingsBackButton = _settingsContainer.Q<Button>("BackButton");
            _settingsBackButton.clicked += SettingsBackButtonOnClicked;
            
            _fpsToggle = _settingsContainer.Q<Toggle>("FPSToggle");
            _fpsToggle.RegisterValueChangedCallback(evt => OnFPSToggleChanged(evt.newValue));
            
            _fullscreenToggle = _settingsContainer.Q<Toggle>("FullscreenToggle");
            _fullscreenToggle.RegisterValueChangedCallback(evt => OnFullscreenToggleChanged(evt.newValue));
            
            _graphicsQualityDropdown = _settingsContainer.Q<DropdownField>("GraphicsQualityDropdown");
            _graphicsQualityDropdown.RegisterValueChangedCallback(evt => OnGraphicsQualityDropdownChanged(evt.newValue));
            
            _masterVolumeSlider = _settingsContainer.Q<Slider>("MasterVolumeSlider");
            _masterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeSliderChanged(evt.newValue));
            
            _fovSlider = _settingsContainer.Q<Slider>("FOVSlider");
            _fovSlider.RegisterValueChangedCallback(evt => OnFOVSliderChanged(evt.newValue));

            // Load settings
            _showFPS = PlayerPrefsGetBool(FPS_COUNTER);
            SetFPSVisibility(_showFPS);
        }

        // Hide Menu UI and activate Overlay UI
        private void StartButtonOnClicked()
        {
            _doc.rootVisualElement.visible = false;
            _overlayController.Enable();
        }

        private void SettingsButtonOnClicked()
        {
            // Clear the displayed container and add the settings UI
            _displayedContainer.Clear();
            _displayedContainer.Add(_settingsContainer);
            
            _fpsToggle.value = PlayerPrefsGetBool(FPS_COUNTER);
            
            _fullscreenToggle.value = Screen.fullScreen;
        }

        private void ExitButtonOnClicked()
        {
            Application.Quit();
        }

        private void OnFPSToggleChanged(bool value)
        {
            PlayerPrefsSetBool(FPS_COUNTER, value);
            _showFPS = value;
            SetFPSVisibility(value);   
        }

        private void OnFullscreenToggleChanged(bool value)
        {
            PlayerPrefsSetBool(FULLSCREEN, value);
            Screen.fullScreen = value;
        }
        private void OnGraphicsQualityDropdownChanged(string value)
        {
            //TODO implement quality settings
        }

        private void OnMasterVolumeSliderChanged(float value)
        {
            //TODO implement volume settings
        }

        private void OnFOVSliderChanged(float value)
        {
            //TODO implement FOV settings
        }

        private void SettingsBackButtonOnClicked()
        {
            // Clear the displayed container and add the start menu UI
            _displayedContainer.Clear();
            _displayedContainer.Add(_startMenuContainer);
        }

        /// <summary>Wrapper to allow setting bools in PlayerPrefs</summary>
        private void PlayerPrefsSetBool(string name, bool value)
        {
            PlayerPrefs.SetInt(name, value ? 1 : 0);
        }

        /// <summary>Wrapper to allow getting bools from PlayerPrefs</summary>
        private bool PlayerPrefsGetBool(string name)
        {
            return PlayerPrefs.GetInt(name, 0) == 1;
        }

        private void DisplayFPS(float fps)
        {
            _fpsLabel.text = "FPS: " + fps.ToString("F0");
            _fpsLastUpdateTime = Time.time;
        }

        private void SetFPSVisibility(bool visible)
        {
            _fpsLabel.visible = visible;
        }

        void Update()
        {
            // FPS counter
            if(_showFPS && Time.time >= _fpsLastUpdateTime + 1f / _fpsUpdateFrequency)
                DisplayFPS(1f / Time.unscaledDeltaTime);
        }

        public void Enable()
        {
            _doc.rootVisualElement.visible = true;
        }
    }
}