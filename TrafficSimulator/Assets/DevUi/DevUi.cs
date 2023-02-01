using UnityEngine;
using UnityEngine.UIElements;

public class DevUi : MonoBehaviour
{
    [SerializeField]
    private UIDocument UIDocument;
    private Button Camera1_Button;
    private Button Camera2_Button;
    private Button Camera_2D_Button;
    private Button Camera_FPV_Button;
    public CinemachineSwitcher Cinemachineswitcher;

    void Start()
    {
        // Get the root element of the UI document
        var rootElement = UIDocument.rootVisualElement;

        // Find the GameObject with the CinemachineSwitcher script
        GameObject go = GameObject.Find("Cameras"); 
        Cinemachineswitcher = go.GetComponent<CinemachineSwitcher>();

        // Map buttons
        Camera1_Button = rootElement.Q<Button>("Camera1");
        Camera2_Button = rootElement.Q<Button>("Camera2");
        Camera_2D_Button = rootElement.Q<Button>("Camera_2D");
        Camera_FPV_Button = rootElement.Q<Button>("Camera_FPV");

        // Add listeners
        Camera1_Button.clickable.clicked += () => OnButtonClicked(1);
        Camera2_Button.clickable.clicked += () => OnButtonClicked(2);
        Camera_2D_Button.clickable.clicked += () => OnButtonClicked(3);
        Camera_FPV_Button.clickable.clicked += () => OnButtonClicked(4);
    }
 
   private void OnDestroy()
   {
        Camera1_Button.clickable.clicked -= () => OnButtonClicked(1);
        Camera2_Button.clickable.clicked -= () => OnButtonClicked(2);
        Camera_2D_Button.clickable.clicked -= () => OnButtonClicked(3);
        Camera_FPV_Button.clickable.clicked -= () => OnButtonClicked(4);
   }

   private void OnButtonClicked(int Button)
    {
        // Call the SwitchCamera method in the CinemachineSwitcher script
        Cinemachineswitcher.SwitchCamera(Button);
    }
}