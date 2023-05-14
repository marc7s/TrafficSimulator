using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

namespace UI
{
    public class MenuController : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _displayedContainer;
        private AudioSource _clickSound;

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
        private DropdownField _simulationModeDropdown;
        private Slider _masterVolumeSlider;
        private Slider _fovSlider;
        private Label _fpsLabel;
        private bool _showFPS = false;

        // Car Spawner Input
        private TextField _carSpawnerInput;
        private string _carSpawnerInputText = "";

        private Label _errorLabel;

        private const int _fpsUpdateFrequency = 15;
        private float _fpsLastUpdateTime = 0;

        private const string FPS_COUNTER = "FPSCounter";
        private const string FULLSCREEN = "Fullscreen";


        void Awake()
        {
            _doc = GetComponent<UIDocument>();

            // Get the audio source
            _clickSound = GetComponent<AudioSource>();

            // Set the displayed container to the start menu
            _displayedContainer = _doc.rootVisualElement.Q<VisualElement>("MenuDisplay");

            // Start Menu UI
            _startMenuContainer = _doc.rootVisualElement.Q<VisualElement>("StartMenuButtons");

            _startButton = _doc.rootVisualElement.Q<Button>("StartButton");
            _startButton.clicked += StartButtonOnClicked;
            
            _settingsButton = _doc.rootVisualElement.Q<Button>("SettingsButton");
            _settingsButton.clicked += SettingsButtonOnClicked;
            
            _exitButton = _doc.rootVisualElement.Q<Button>("ExitButton");
            _exitButton.clicked += ExitButtonOnClicked;
            
            _fpsLabel = _doc.rootVisualElement.Q<Label>("FPSLabel");
            _errorLabel = _doc.rootVisualElement.Q<Label>("ErrorLabel");

            // Settings UI
            _settingsContainer = _settingsUI.CloneTree();
            
            _settingsBackButton = _settingsContainer.Q<Button>("BackButton");
            _settingsBackButton.clicked += SettingsBackButtonOnClicked;
            
            _fpsToggle = _settingsContainer.Q<Toggle>("FPSToggle");
            _fpsToggle.RegisterValueChangedCallback(evt => OnFPSToggleChanged(evt.newValue));
            
            _fullscreenToggle = _settingsContainer.Q<Toggle>("FullscreenToggle");
            _fullscreenToggle.RegisterValueChangedCallback(evt => OnFullscreenToggleChanged(evt.newValue));
            
            _simulationModeDropdown = _settingsContainer.Q<DropdownField>("SimulationModeDropDown");
            _simulationModeDropdown.RegisterValueChangedCallback(evt => OnSimulationModeDropdownChanged(evt.newValue));
            
            _masterVolumeSlider = _settingsContainer.Q<Slider>("MasterVolumeSlider");
            _masterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeSliderChanged(evt.newValue/100));
            
            _fovSlider = _settingsContainer.Q<Slider>("FOVSlider");
            _fovSlider.RegisterValueChangedCallback(evt => OnFOVSliderChanged(evt.newValue));

            // Input
            _carSpawnerInput = _doc.rootVisualElement.Q<TextField>("CarSpawnerInput");

            // Load settings
            _showFPS = PlayerPrefsGetBool(FPS_COUNTER);
            SetFPSVisibility(_showFPS);
            _clickSound.volume = PlayerPrefs.GetFloat("MasterVolume");
            _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume")*100;
        }

        // Hide Menu UI and activate Overlay UI
        private void StartButtonOnClicked()
        {
            PlayClickSound();
            if (_carSpawnerInput.text.Equals("Number of cars to simulate..."))
            {
                return;
            }
            PlayerPrefs.SetInt("CarsToSpawn", int.Parse(_carSpawnerInput.text));
            _doc.rootVisualElement.visible = false;
            // ------------------------------------- TODO CHANGE TO GAME SCENE ------------------------------------- //
            SceneManager.LoadScene((SceneManager.GetActiveScene().buildIndex + 1), LoadSceneMode.Single);
        }

        private void SettingsButtonOnClicked()
        {
            PlayClickSound();
            // Clear the displayed container and add the settings UI
            _displayedContainer.Clear();
            _displayedContainer.Add(_settingsContainer);
            
            _fpsToggle.value = PlayerPrefsGetBool(FPS_COUNTER);
            
            _fullscreenToggle.value = Screen.fullScreen;
        }

        private void ExitButtonOnClicked()
        {
            PlayClickSound();
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
        private void OnSimulationModeDropdownChanged(string value)
        {
            //TODO implement quality settings
            PlayerPrefsSetBool("SimulationMode", value.Equals("Quality"));
            PlayClickSound();
        }

        private void OnMasterVolumeSliderChanged(float value)
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            _clickSound.volume = value;
            PlayClickSound();
        }

        private void OnFOVSliderChanged(float value)
        {
            //TODO implement FOV settings
        }

        private void SettingsBackButtonOnClicked()
        {
            PlayClickSound();
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

        private void FilterInput()
        {
            const string placeholderText = "Number of cars to simulate...";
            bool isplaceholderText = _carSpawnerInput.text.Equals(placeholderText);
            bool isStartingZero = _carSpawnerInput.text.StartsWith("0");
            _carSpawnerInputText = _carSpawnerInput.text;
            
            if (!_carSpawnerInputText.Equals("") && !isplaceholderText && !isStartingZero)
                _carSpawnerInput.SetValueWithoutNotify(Regex.Replace(_carSpawnerInputText, @"[^0-9]", ""));
            else if (!isplaceholderText || isStartingZero)
                _carSpawnerInput.SetValueWithoutNotify(placeholderText);
        }

        private void PlayClickSound()
        {
            _clickSound.Play();
        }

        void Update()
        {
            // Filter input
            FilterInput();

            // FPS counter
            if(_showFPS && Time.time >= _fpsLastUpdateTime + 1f / _fpsUpdateFrequency)
                DisplayFPS(1f / Time.unscaledDeltaTime);
        }
    }
}