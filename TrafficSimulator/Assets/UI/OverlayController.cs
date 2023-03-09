using UnityEngine;
using UnityEngine.UIElements;

public class OverlayController : MonoBehaviour
{
    private UIDocument _doc;
    private MenuController _menuController;
    
    // Overlay Button
    private Button _menuButton;

    // Camera Buttons
    private Button _freeCamButton;
    private Button _twoDButton;
    private Button _fpvButton;

    // Mode Buttons
    private Button _statisticsButton;
    private Button _worldOptionButton;
    private Button _editorButton;

    // Clock Buttons
    private Button _rewindButton;
    private Button _pausButton;
    private Button _fastForwardButton;


    private Label _clockLabel;


    private const string FULLSCREEN = "Fullscreen";


    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _menuController = GameObject.Find("UIMenu").GetComponent<MenuController>();

        // Labels
        _clockLabel = _doc.rootVisualElement.Q<Label>("Clock");
        _clockLabel.text = "00:00";

        // Buttons
        _menuButton = _doc.rootVisualElement.Q<Button>("MenuButton");
        _menuButton.clicked += MenuButtonOnClicked;

        _freeCamButton = _doc.rootVisualElement.Q<Button>("FreeCam");
        _freeCamButton.clicked += FreeCamButtonOnClicked;

        _twoDButton = _doc.rootVisualElement.Q<Button>("2D");
        _twoDButton.clicked += TwoDButtonOnClicked;

        _fpvButton = _doc.rootVisualElement.Q<Button>("FPV");
        _fpvButton.clicked += FPVButtonOnClicked;

        _statisticsButton = _doc.rootVisualElement.Q<Button>("Statistics");
        _statisticsButton.clicked += StatisticsButtonOnClicked;

        _worldOptionButton = _doc.rootVisualElement.Q<Button>("WorldOptions");
        _worldOptionButton.clicked += WorldOptionButtonOnClicked;

        _editorButton = _doc.rootVisualElement.Q<Button>("Editor");
        _editorButton.clicked += EditorButtonOnClicked;

        _doc.rootVisualElement.visible = false;
        
    }

    private void MenuButtonOnClicked()
    {
        _doc.rootVisualElement.visible = false;
        _menuController.Enable();
    }

    private void FreeCamButtonOnClicked()
    {
        Debug.Log("FreeCam");
    }

    private void TwoDButtonOnClicked()
    {
        Debug.Log("2D");
    }

    private void FPVButtonOnClicked()
    {
        Debug.Log("FPV");
    }

    private void StatisticsButtonOnClicked()
    {
        Debug.Log("Statistics");
    }

    private void WorldOptionButtonOnClicked()
    {
        Debug.Log("WorldOptions");
    }

    private void EditorButtonOnClicked()
    {
        Debug.Log("Editor");
    }

    void Update()
    {
        _clockLabel.text = System.DateTime.Now.ToString("HH:mm");
    }

    public void Enable()
    {
        _doc.rootVisualElement.visible = true;
    }
}