using UnityEngine;
using UnityEngine.UIElements;

public class OverlayController : MonoBehaviour
{
    private UIDocument _doc;
    private MenuController _menuController;
    
    // Overlay UI 
    private VisualElement _settingsContainer;
    private Button _menuButton;


    private const string FULLSCREEN = "Fullscreen";


    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _menuController = GameObject.Find("UIMenu").GetComponent<MenuController>();

        // Start Menu UI
        _menuButton = _doc.rootVisualElement.Q<Button>("MenuButton");
        _menuButton.clicked += MenuButtonOnClicked;

        _doc.rootVisualElement.visible = false;
        
    }
    
    private void MenuButtonOnClicked()
    {
        _doc.rootVisualElement.visible = false;
        _menuController.Enable();
    }

    public void Enable()
    {
        _doc.rootVisualElement.visible = true;
    }
}