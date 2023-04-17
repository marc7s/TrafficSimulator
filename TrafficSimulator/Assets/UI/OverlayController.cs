using UnityEngine;
using UnityEngine.UIElements;
using Simulation;

namespace UI 
{
    public class OverlayController : MonoBehaviour
    {
        private UIDocument _doc;
        private MenuController _menuController;
        
        // Overlay Button
        private Button _menuButton;

        // Car statistics UI
        private VisualElement _statisticsUI;
        public bool _isStatisticsOpen = false;

        // World statistics UI
        private VisualElement _worldUI;
        public bool _isWorldOpen = false;

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
        private Button _pauseButton;
        private Button _fastForwardButton;

        private Label _clockLabel;

        private const string FULLSCREEN = "Fullscreen";


        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _menuController = GameObject.Find("UIMenu").GetComponent<MenuController>();

            // Labels
            _clockLabel = _doc.rootVisualElement.Q<Label>("Clock");
            _clockLabel.text = "0000:00:00:00:00:00";

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

            // Get car statistics UI visual element
            _statisticsUI = _doc.rootVisualElement.Q<VisualElement>("StatisticsWindow");
            _statisticsUI.pickingMode = PickingMode.Position;
            _statisticsUI.visible = false;
            // Make draggable
            _statisticsUI.AddManipulator(new DragManipulator());
            _statisticsUI.RegisterCallback<DropEvent>(evt => Debug.Log($"{evt.target} dropped on {evt.droppable}"));

            // Get world statistics UI visual element
            _worldUI = _doc.rootVisualElement.Q<VisualElement>("WorldWindow");
            _worldUI.pickingMode = PickingMode.Position;
            _worldUI.visible = false;
            // Make draggable
            _worldUI.AddManipulator(new DragManipulator());
            _worldUI.RegisterCallback<DropEvent>(evt => Debug.Log($"{evt.target} dropped on {evt.droppable}"));

            _worldOptionButton = _doc.rootVisualElement.Q<Button>("WorldOptions");
            _worldOptionButton.clicked += WorldOptionButtonOnClicked;

            _editorButton = _doc.rootVisualElement.Q<Button>("Editor");
            _editorButton.clicked += EditorButtonOnClicked;

            _rewindButton = _doc.rootVisualElement.Q<Button>("Rewind");
            _rewindButton.clicked += RewindButtonOnClicked;

            _pauseButton = _doc.rootVisualElement.Q<Button>("Pause");
            _pauseButton.clicked += PauseButtonOnClicked;

            _fastForwardButton = _doc.rootVisualElement.Q<Button>("Fastforward");
            _fastForwardButton.clicked += FastForwardButtonOnClicked;

            _doc.rootVisualElement.visible = false;
        }

        private void MenuButtonOnClicked()
        {
            _statisticsUI.visible = false;
            _worldUI.visible = false;
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
            _isStatisticsOpen = !_isStatisticsOpen;
            _statisticsUI.visible = _isStatisticsOpen;
        }

        private void WorldOptionButtonOnClicked()
        {
            _isWorldOpen = !_isWorldOpen;
            _worldUI.visible = _isWorldOpen;
        }

        private void EditorButtonOnClicked()
        {
            Debug.Log("Editor");
        }

        private void RewindButtonOnClicked()
        {
            TimeManager.Instance.SetModeRewind();
        }

        private void PauseButtonOnClicked()
        {
            TimeManager.Instance.SetModePause();
        }

        private void FastForwardButtonOnClicked()
        {
            TimeManager.Instance.SetModeFastForward();
        }

        void Update()
        {
            _clockLabel.text = TimeManager.Instance.Timestamp;
        }

        public void Enable()
        {
            _doc.rootVisualElement.visible = true;
        }
    }
}