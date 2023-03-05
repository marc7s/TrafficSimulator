using UnityEngine;
using UnityEngine.UIElements;
public class MenuController : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _buttonWrapper;
    private VisualElement _settingsButtons;
    [SerializeField] private VisualTreeAsset _settingsUI;
    private Button _startButton;
    private Button _settingsButton;
    private Button _exitButton;

    //  Settings UI
    private Button _settingsBackButton;
    private Toggle _FPSToggle;

    private Toggle _FullScreenToggle;
    private DropdownField _GraphicsQualityDropdown;
    private Slider _MasterVolumeSlider;
    private Slider _FOVSlider;
    private Label _fpsLabel;


    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _buttonWrapper = _doc.rootVisualElement.Q<VisualElement>("Buttons");

        // Start Menu UI
        _startButton = _doc.rootVisualElement.Q<Button>("StartButton");
        _startButton.clicked += StartButtonOnClicked;
        _settingsButton = _doc.rootVisualElement.Q<Button>("SettingsButton");
        _settingsButton.clicked += SettingsButtonOnClicked;
        _exitButton = _doc.rootVisualElement.Q<Button>("ExitButton");
        _exitButton.clicked += ExitButtonOnClicked;
        _fpsLabel = _doc.rootVisualElement.Q<Label>("FPSLabel");

        // Settings UI
        _settingsButtons = _settingsUI.CloneTree();
        _settingsBackButton = _settingsButtons.Q<Button>("BackButton");
        _settingsBackButton.clicked += SettingsBackButtonOnClicked;
        _FPSToggle = _settingsButtons.Q<Toggle>("FPSToggle");
        _FPSToggle.RegisterValueChangedCallback(evt => OnFPSToggleChanged(evt.newValue));
        _FullScreenToggle = _settingsButtons.Q<Toggle>("FullscreenToggle");
        _FullScreenToggle.RegisterValueChangedCallback(evt => OnFullScreenToggleChanged(evt.newValue));
        _GraphicsQualityDropdown = _settingsButtons.Q<DropdownField>("GraphicsQualityDropdown");
        _GraphicsQualityDropdown.RegisterValueChangedCallback(evt => OnGraphicsQualityDropdownChanged(evt.newValue));
        _MasterVolumeSlider = _settingsButtons.Q<Slider>("MasterVolumeSlider");
        _MasterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeSliderChanged(evt.newValue));
        _FOVSlider = _settingsButtons.Q<Slider>("FOVSlider");
        _FOVSlider.RegisterValueChangedCallback(evt => OnFOVSliderChanged(evt.newValue));
        _FOVSlider = _settingsButtons.Q<Slider>("FOVSlider");

        
    }
    private void StartButtonOnClicked()
    {
        // TODO switch to game scene
        Debug.Log("Start");
    }

    private void SettingsButtonOnClicked()
    {
        _buttonWrapper.Clear();
        _buttonWrapper.Add(_settingsButtons);
        _FPSToggle.value = PlayerPrefs.GetInt("FPSCounter", 0) == 1;
        _FullScreenToggle.value = Screen.fullScreen;
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
        _buttonWrapper.Clear();
        _buttonWrapper.Add(_startButton);
        _buttonWrapper.Add(_settingsButton);
        _buttonWrapper.Add(_exitButton);
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
