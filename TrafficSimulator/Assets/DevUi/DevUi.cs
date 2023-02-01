using UnityEngine;
using UnityEngine.UIElements;

public class DevUi : MonoBehaviour
{
    [SerializeField]
    private UIDocument _uiDocument;
    private Button _camera1_Button;
    private Button _camera2_Button;
    private Button _camera_2D_Button;
    private Button _camera_FPV_Button;
    public CinemachineSwitcher Cinemachineswitcher;

    void Start()
    {
        // Get the root element of the UI document
        var RootElement = _uiDocument.rootVisualElement;

        // Find the GameObject with the CinemachineSwitcher script
        GameObject go = GameObject.Find("Cameras"); 
        Cinemachineswitcher = go.GetComponent<CinemachineSwitcher>();

        // Map buttons
        _camera1_Button = RootElement.Q<Button>("Camera1");
        _camera2_Button = RootElement.Q<Button>("Camera2");
        _camera_2D_Button = RootElement.Q<Button>("Camera_2D");
        _camera_FPV_Button = RootElement.Q<Button>("Camera_FPV");

        // Add listeners
        _camera1_Button.clickable.clicked += () => OnButtonClicked(1);
        _camera2_Button.clickable.clicked += () => OnButtonClicked(2);
        _camera_2D_Button.clickable.clicked += () => OnButtonClicked(3);
        _camera_FPV_Button.clickable.clicked += () => OnButtonClicked(4);
    }
 
   private void OnDestroy()
   {
        _camera1_Button.clickable.clicked -= () => OnButtonClicked(1);
        _camera2_Button.clickable.clicked -= () => OnButtonClicked(2);
        _camera_2D_Button.clickable.clicked -= () => OnButtonClicked(3);
        _camera_FPV_Button.clickable.clicked -= () => OnButtonClicked(4);
   }

   private void OnButtonClicked(int button)
    {
        // Call the SwitchCamera method in the CinemachineSwitcher script
        Cinemachineswitcher.SwitchCamera(button);
    }
}