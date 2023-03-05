using UnityEngine;
using UnityEngine.UIElements;
public class MenuController : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _displayedContainer;
    // Start Menu UI
    private VisualElement _settingsContainer;
    private Button _startButton;
    private Button _settingsButton;
    private Button _exitButton;

    //  Settings UI
    [SerializeField] private VisualTreeAsset _settingsUI;
    private Button _settingsBackButton;
    private Toggle _fpsToggle;

    private Toggle _fullScreenToggle;
    private DropdownField _graphicsQualityDropdown;
    private Slider _masterVolumeSlider;
    private Slider _fovSlider;
    private Label _fpsLabel;


    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        // set the displayed container to the start menu
        _displayedContainer = _doc.rootVisualElement.Q<VisualElement>("StartMenuButtons");

        // Start Menu UI
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
        _fullScreenToggle = _settingsContainer.Q<Toggle>("FullscreenToggle");
        _fullScreenToggle.RegisterValueChangedCallback(evt => OnFullScreenToggleChanged(evt.newValue));
        _graphicsQualityDropdown = _settingsContainer.Q<DropdownField>("GraphicsQualityDropdown");
        _graphicsQualityDropdown.RegisterValueChangedCallback(evt => OnGraphicsQualityDropdownChanged(evt.newValue));
        _masterVolumeSlider = _settingsContainer.Q<Slider>("MasterVolumeSlider");
        _masterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeSliderChanged(evt.newValue));
        _fovSlider = _settingsContainer.Q<Slider>("FOVSlider");
        _fovSlider.RegisterValueChangedCallback(evt => OnFOVSliderChanged(evt.newValue));
        _fovSlider = _settingsContainer.Q<Slider>("FOVSlider");

        
    }
    private void StartButtonOnClicked()
    {
        // TODO switch to game scene
        Debug.Log("Start");
    }

    private void SettingsButtonOnClicked()
    {
        // Clear the displayed container and add the settings UI
        _displayedContainer.Clear();
        _displayedContainer.Add(_settingsContainer);
        _fpsToggle.value = PlayerPrefs.GetInt("FPSCounter", 0) == 1;
        _fullScreenToggle.value = Screen.fullScreen;
    }

    private void ExitButtonOnClicked()
    {
        Application.Quit();
    }

    private void OnFPSToggleChanged(bool value)
    {
        PlayerPrefs.SetInt("FPSCounter", value ? 1 : 0);
        _fpsLabel.visible = value;
    }

    private void OnFullScreenToggleChanged(bool value)
    {
        PlayerPrefs.SetInt("FullScreen", value ? 1 : 0);
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
        _displayedContainer.Add(_startButton);
        _displayedContainer.Add(_settingsButton);
        _displayedContainer.Add(_exitButton);
    }

    void Update()
    {
        // FPS counter
        if (PlayerPrefs.GetInt("FPSCounter", 0) == 1)
            _fpsLabel.text = "FPS: " + (1f / Time.unscaledDeltaTime).ToString("F0");
        else
            _fpsLabel.text = "";
    }
}
