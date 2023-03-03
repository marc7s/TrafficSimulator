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
    private Button _settingsBackButton;

    private Label _fpsLabel;

    private Toggle _FPSToggle;
    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _buttonWrapper = _doc.rootVisualElement.Q<VisualElement>("Buttons");
        _startButton = _doc.rootVisualElement.Q<Button>("StartButton");
        _startButton.clicked += PlayButtonOnClicked;
        _settingsButton = _doc.rootVisualElement.Q<Button>("SettingsButton");
        _settingsButton.clicked += SettingsButtonOnClicked;
        _exitButton = _doc.rootVisualElement.Q<Button>("ExitButton");
        _exitButton.clicked += ExitButtonOnClicked;
        _fpsLabel = _doc.rootVisualElement.Q<Label>("fpsLabel");


        _settingsButtons = _settingsUI.CloneTree();
        _settingsBackButton = _settingsButtons.Q<Button>("BackButton");
        _settingsBackButton.clicked += SettingsBackButtonOnClicked;
        
    }
    private void PlayButtonOnClicked()
    {
        // TODO switch to game scene
        Debug.Log("Play");
    }

    private void SettingsButtonOnClicked()
    {
        _buttonWrapper.Clear();
        _buttonWrapper.Add(_settingsButtons);
        _FPSToggle = _doc.rootVisualElement.Q<Toggle>("FPSToggle");
        _FPSToggle.value = PlayerPrefs.GetInt("FPSCounter", 0) == 1;
        _FPSToggle.RegisterValueChangedCallback(evt => OnFPSToggleChanged(evt.newValue));
        Debug.Log(_FPSToggle);
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

    private void SettingsBackButtonOnClicked()
    {
        _buttonWrapper.Clear();
        _buttonWrapper.Add(_startButton);
        _buttonWrapper.Add(_settingsButton);
        _buttonWrapper.Add(_exitButton);
    }

    void Update()
    {
        if (PlayerPrefs.GetInt("FPSCounter", 0) == 1)
        {
            _fpsLabel.text = "FPS: " + (1f / Time.unscaledDeltaTime).ToString("F0");
        }
        else
        {
            _fpsLabel.text = "";
        }
    }
}
