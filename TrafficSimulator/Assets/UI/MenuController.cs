using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System;

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

        // Loading Screen UI
        [SerializeField] private VisualTreeAsset _loadingScreenUI;
        private VisualElement _loadingScreenContainer;
        private ProgressBar _loadingBar;

        // Scene Selection UI
        private VisualElement _sceneSelectionContainer;
        private VisualElement _currentSceneContainer;
        [SerializeField] private VisualTreeAsset _sceneSelectionUI;
        [SerializeField] private VisualTreeAsset _sceneTemplate;
        private Button _sceneSelectionButton;
        private Button _sceneSelectionBackButton;
        private SceneLoader.SceneInfo _currentSceneInfo;
        private VisualElement _currentSceneElement;

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
            _startMenuContainer = _doc.rootVisualElement.Q<VisualElement>("StartMenuContainer");

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

            // Loading Screen UI
            _loadingScreenContainer = _loadingScreenUI.CloneTree();

            _loadingBar = _loadingScreenContainer.Q<ProgressBar>("LoadingBar");

            // Input
            _carSpawnerInput = _doc.rootVisualElement.Q<TextField>("CarSpawnerInput");

            // Load settings
            _showFPS = PlayerPrefsGetBool(FPS_COUNTER);
            SetFPSVisibility(_showFPS);
            _clickSound.volume = PlayerPrefs.GetFloat("MasterVolume");
            _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume") * 100;

            // Load scenes
            _sceneSelectionContainer = _sceneSelectionUI.CloneTree();
            
            // Add the current scene container to the start menu
            _currentSceneContainer = _startMenuContainer.Q<VisualElement>("SelectedScene");
            _currentSceneElement = _sceneTemplate.CloneTree();
            _currentSceneContainer.Add(_currentSceneElement);

            // Connect the scene selection button
            _sceneSelectionButton = _doc.rootVisualElement.Q<Button>("SceneSelectionButton");
            _sceneSelectionButton.clicked += SceneSelectionButtonOnClicked;

            // Connect the scene selection back button
            _sceneSelectionBackButton = _sceneSelectionContainer.Q<Button>("BackButton");
            _sceneSelectionBackButton.clicked += SceneSelectionBackButtonOnClicked;
            
            // Load the scenes and connect the callback when a scene is switched
            SceneLoader.LoadScenes(_sceneSelectionContainer, _sceneTemplate);
            SceneLoader.OnSceneSwitch += ChangeSelectedScene;
            
            // Load the default scene
            Action onClick = () => SceneSelectionButtonOnClicked();
            SceneLoader.SetSceneInfo(ref _currentSceneElement, SceneLoader.DefaultScene, onClick);
            _currentSceneInfo = SceneLoader.DefaultScene;
        }

        private void ChangeSelectedScene(SceneLoader.SceneInfo sceneInfo)
        {
            _currentSceneInfo = sceneInfo;

            // Add an onclick event to the scene, so clicking the current scene takes you to the scene selection menu
            Action onClick = () => SceneSelectionButtonOnClicked();
            SceneLoader.SetSceneInfo(ref _currentSceneElement, _currentSceneInfo, onClick);
            
            // Go back to the start menu
            SceneSelectionBackButtonOnClicked();
        }

        // Hide Menu UI and activate Overlay UI
        private void StartButtonOnClicked()
        {
            PlayClickSound();
            
            if (_carSpawnerInput.text.Equals("Number of cars to simulate..."))
                return;
            
            PlayerPrefs.SetInt("CarsToSpawn", int.Parse(_carSpawnerInput.text));
            // ------------------------------------- TODO CHANGE TO GAME SCENE ------------------------------------- //
            StartCoroutine(LoadSceneAsync("Scenes/Build/" + _currentSceneInfo.SceneName));
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

        private void SceneSelectionButtonOnClicked()
        {
            PlayClickSound();
            
            // Clear the displayed container and add the scene selection UI
            _displayedContainer.Clear();
            _displayedContainer.Add(_sceneSelectionContainer);
            
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

        private void SceneSelectionBackButtonOnClicked()
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
            {
                _carSpawnerInput.SetValueWithoutNotify(Regex.Replace(_carSpawnerInputText, @"[^0-9]", ""));
                RestoreButton(_startButton);
            }
            else if (!isplaceholderText || isStartingZero)
            {
               _carSpawnerInput.SetValueWithoutNotify(placeholderText);
                GreyOutButton(_startButton); 
            }
        }

        private void PlayClickSound()
        {
            _clickSound.Play();
        }

        private void GreyOutButton(Button button)
        {
            button.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            button.style.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            button.SetEnabled(false);
        }

        private void RestoreButton(Button button)
        {
            button.style.backgroundColor = StyleKeyword.Null;
            button.style.color = StyleKeyword.Null;
            button.SetEnabled(true);
        }

        void Update()
        {
            // Filter input
            FilterInput();

            // FPS counter
            if(_showFPS && Time.time >= _fpsLastUpdateTime + 1f / _fpsUpdateFrequency)
                DisplayFPS(1f / Time.unscaledDeltaTime);
        }

        IEnumerator LoadSceneAsync(string sceneName)
        {
            // Empty root VisualElement and add loading UI
            _displayedContainer.Clear();
            _displayedContainer.Add(_loadingScreenContainer);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress) * 100;
                
                _loadingBar.value = progress;
                _loadingBar.title = progress + "%";

                yield return null;
            }
        }
    }
}