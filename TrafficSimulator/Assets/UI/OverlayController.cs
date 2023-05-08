using Cam;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Simulation;
using User;

namespace UI 
{
    public class OverlayController : MonoBehaviour
    {
        private UIDocument _doc;
        
        // Overlay Button
        private Button _menuButton;

        // Car statistics UI
        private VisualElement _statisticsUI;
        public bool _isStatisticsOpen = false;

        // World statistics UI
        private VisualElement _worldUI;
        public bool _isWorldOpen = false;

        // Camera Buttons
        private StyleSheet _cameraButtonStyles;
        private Button _defaultCameraButton;
        private Button _focusedCameraButton;
        private Button _fpvButton;
        private Button _currentlyHighlightedCameraButton;

        // Mode Buttons
        private Button _statisticsButton;
        private Button _worldOptionButton;
        private Button _editorButton;

        // Clock Buttons
        private Button _rewindButton;
        private Button _pauseButton;
        private VisualElement _pauseIcon;
        private VisualElement _playIcon;
        private Button _fastForwardButton;
        public bool _isPaused = false;

        private Label _clockLabel;

        private const string FULLSCREEN = "Fullscreen";

        private CameraManager _cameraManager;
        
        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _cameraButtonStyles = Resources.Load<StyleSheet>("CameraButtonStyle");
            _doc.rootVisualElement.styleSheets.Add(_cameraButtonStyles);

            // Labels
            _clockLabel = _doc.rootVisualElement.Q<Label>("Clock");
            _clockLabel.text = "0000:00:00:00:00:00";

            
            FindCameraManager();
            // Buttons
            _menuButton = _doc.rootVisualElement.Q<Button>("MenuButton");
            _menuButton.clicked += MenuButtonOnClicked;

            _defaultCameraButton = _doc.rootVisualElement.Q<Button>("Default");
            _defaultCameraButton.AddToClassList("button");
            _defaultCameraButton.clicked += DefaultCameraButtonClicked;

            _focusedCameraButton = _doc.rootVisualElement.Q<Button>("Focus");
            _focusedCameraButton.AddToClassList("button");
            _focusedCameraButton.clicked += FocusedCameraButtonOnClicked;
            GreyOutButton(_focusedCameraButton);

            _fpvButton = _doc.rootVisualElement.Q<Button>("FPV");
            _fpvButton.AddToClassList("button");
            _fpvButton.clicked += FPVButtonOnClicked;
            GreyOutButton(_fpvButton);

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

            _pauseIcon = _doc.rootVisualElement.Q<VisualElement>("Pause-icon");
            _playIcon = _doc.rootVisualElement.Q<VisualElement>("Play-icon");

            _fastForwardButton = _doc.rootVisualElement.Q<Button>("Fastforward");
            _fastForwardButton.clicked += FastForwardButtonOnClicked;

            _doc.rootVisualElement.visible = false;
        }

        private void Start()
        {
            UserSelectManager.Instance.OnSelectedGameObject += selectedGameObject =>
            {
                if (selectedGameObject)
                {
                    RestoreButton(_fpvButton);
                    RestoreButton(_focusedCameraButton);
                }
                else
                {
                    GreyOutButton(_fpvButton);
                    GreyOutButton(_focusedCameraButton);
                }
            };
            _cameraManager.OnCameraChanged += type =>
            {
                // Reset all camera buttons to normal state
                RemoveCameraButtonHighlight(_defaultCameraButton);
                RemoveCameraButtonHighlight(_focusedCameraButton);
                RemoveCameraButtonHighlight(_fpvButton);

                // Highlight the correct camera button based on the camera type
                switch (type)
                {
                    case CameraManager.CustomCameraType.Default:
                        HighlightCameraButton(_defaultCameraButton);
                        break;
                    case CameraManager.CustomCameraType.Focus:
                        HighlightCameraButton(_focusedCameraButton);
                        break;
                    case CameraManager.CustomCameraType.FirstPersonDriver:
                        HighlightCameraButton(_fpvButton);
                        break;
                    default:
                        Debug.LogWarning("Unknown camera type.");
                        break;
                }
            };
        }
        
        private void FindCameraManager()
        {
            GameObject go = GameObject.Find("CameraManager");
            if (go != null)
            {
                _cameraManager = go.GetComponent<CameraManager>();
            }
        }
        
        private void GreyOutButton(Button button)
        {
            button.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            button.style.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            button.SetEnabled(false);
        }

        private void RestoreButton(Button button)
        {
            button.style.backgroundColor = StyleKeyword.Null;
            button.style.color = StyleKeyword.Null;
            button.SetEnabled(true);
        }

        private void HighlightCameraButton(Button buttonToHighlight)
        {
            if (_currentlyHighlightedCameraButton != null)
            {
                RemoveCameraButtonHighlight(_currentlyHighlightedCameraButton);
            }

            AddButtonHighlight(buttonToHighlight);
            _currentlyHighlightedCameraButton = buttonToHighlight;
        }

        private void AddButtonHighlight(Button button)
        {
            button.AddToClassList("button-highlighted");
        }

        private void RemoveCameraButtonHighlight(Button button)
        {
            button.RemoveFromClassList("button-highlighted");
        }


        private void MenuButtonOnClicked()
        {
            _statisticsUI.visible = false;
            _worldUI.visible = false;
            _doc.rootVisualElement.visible = false;
            SceneManager.LoadScene("StartMenu",  LoadSceneMode.Single);
        }

        private void DefaultCameraButtonClicked()
        {
            _cameraManager.ToggleDefaultCamera();
        }

        private void FocusedCameraButtonOnClicked()
        {
            _cameraManager.ToggleFocusCamera();
        }

        private void FPVButtonOnClicked()
        {
            _cameraManager.ToggleFirstPersonDriverCamera();
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
            SceneManager.LoadScene("Martin", LoadSceneMode.Single);
        }

        private void RewindButtonOnClicked()
        {
            TimeManager.Instance.SetModeRewind();
        }

        private void PauseButtonOnClicked()
        {
            TimeManager.Instance.SetModePause();
            _isPaused = !_isPaused;
            _pauseIcon.visible = !_isPaused;
            _playIcon.visible = _isPaused;
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