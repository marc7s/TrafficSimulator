using UnityEngine;
using UnityEngine.UIElements;

public class DevUi : MonoBehaviour
{
    [SerializeField]
    private UIDocument uiDocument;
    private Button camera1_Button;
    private Button camera2_Button;
    private Button camera_2D_Button;
    private Button camera_FPV_Button;
    public CinemachineSwitcher Cinemachineswitcher;

    void Start()
    {
        // Get the root element of the UI document
        var RootElement = uiDocument.rootVisualElement;

        // Find the GameObject with the CinemachineSwitcher script
        GameObject go = GameObject.Find("Cameras"); 
        Cinemachineswitcher = go.GetComponent<CinemachineSwitcher>();

        // Map buttons
        camera1_Button = RootElement.Q<Button>("Camera1");
        camera2_Button = RootElement.Q<Button>("Camera2");
        camera_2D_Button = RootElement.Q<Button>("Camera_2D");
        camera_FPV_Button = RootElement.Q<Button>("Camera_FPV");

        // Add listeners
        camera1_Button.clickable.clicked += () => OnButtonClicked(1);
        camera2_Button.clickable.clicked += () => OnButtonClicked(2);
        camera_2D_Button.clickable.clicked += () => OnButtonClicked(3);
        camera_FPV_Button.clickable.clicked += () => OnButtonClicked(4);
    }
 
   private void OnDestroy()
   {
        camera1_Button.clickable.clicked -= () => OnButtonClicked(1);
        camera2_Button.clickable.clicked -= () => OnButtonClicked(2);
        camera_2D_Button.clickable.clicked -= () => OnButtonClicked(3);
        camera_FPV_Button.clickable.clicked -= () => OnButtonClicked(4);
   }

   private void OnButtonClicked(int button)
    {
        // Call the SwitchCamera method in the CinemachineSwitcher script
        Cinemachineswitcher.SwitchCamera(button);
    }
}