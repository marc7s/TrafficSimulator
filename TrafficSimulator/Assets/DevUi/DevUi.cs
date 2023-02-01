using UnityEngine;
using UnityEngine.UIElements;

public class DevUi : MonoBehaviour
{
    [SerializeField]
    private UIDocument _uiDocument;
    private Button _camera1Button;
    private Button _camera2Button;
    private Button _camera2DButton;
    private Button _cameraFpvButton;
    public CinemachineSwitcher Cinemachineswitcher;

    void Start()
    {
        // Get the root element of the UI document
        var RootElement = _uiDocument.rootVisualElement;

        // Find the GameObject with the CinemachineSwitcher script
        GameObject go = GameObject.Find("Cameras"); 
        Cinemachineswitcher = go.GetComponent<CinemachineSwitcher>();

        // Map buttons
        _camera1Button = RootElement.Q<Button>("Camera1");
        _camera2Button = RootElement.Q<Button>("Camera2");
        _camera2DButton = RootElement.Q<Button>("Camera_2D");
        _cameraFpvButton = RootElement.Q<Button>("Camera_FPV");

        // Add listeners
        _camera1Button.clickable.clicked += () => OnButtonClicked(1);
        _camera2Button.clickable.clicked += () => OnButtonClicked(2);
        _camera2DButton.clickable.clicked += () => OnButtonClicked(3);
        _cameraFpvButton.clickable.clicked += () => OnButtonClicked(4);
    }
 
   private void OnDestroy()
   {
        _camera1Button.clickable.clicked -= () => OnButtonClicked(1);
        _camera2Button.clickable.clicked -= () => OnButtonClicked(2);
        _camera2DButton.clickable.clicked -= () => OnButtonClicked(3);
        _cameraFpvButton.clickable.clicked -= () => OnButtonClicked(4);
   }

   private void OnButtonClicked(int button)
    {
        // Call the SwitchCamera method in the CinemachineSwitcher script
        Cinemachineswitcher.SwitchCamera(button);
    }
}