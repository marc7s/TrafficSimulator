using Cam;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Simulation;
using User;
using RoadGenerator;
using System;

namespace UI 
{
    public class OverlayController : MonoBehaviour
    {
        private UIDocument _doc;
        private AudioSource _clickSound;
        
        // Overlay Button
        private Button _menuButton;

        // Camera Buttons
        private StyleSheet _cameraButtonStyles;
        private Button _freecamCameraButton;
        private Button _followCameraButton;
        private Button _driverCameraButton;
        private Button _currentlyHighlightedCameraButton;

        // Clock Buttons
        private Button _rewindButton;
        private Button _pauseButton;
        private VisualElement _pauseIcon;
        private VisualElement _playIcon;
        private Button _fastForwardButton;
        private bool _isPaused = false;

        // Labels
        private Label _clockLabel;
        private Label _fpsLabel;
        private bool _showFPS = false;

        private bool _qualityMode = false;
        private int _carsToSpawn = 0;

        private const int _fpsUpdateFrequency = 15;
        private float _fpsLastUpdateTime = 0;

        private const string FPS_COUNTER = "FPSCounter";
        private const string FULLSCREEN = "Fullscreen";

        private CameraManager _cameraManager;

        public int CarsToSpawn => _carsToSpawn;

        public Action OnSimulationStart;
        public Action OnSimulationStop;

        
        void Awake()
        {
            _doc = GetComponent<UIDocument>();

            // Get the audio source
            _clickSound = GetComponent<AudioSource>();

            _cameraButtonStyles = Resources.Load<StyleSheet>("CameraButtonStyle");
            _doc.rootVisualElement.styleSheets.Add(_cameraButtonStyles);

            // Labels
            _clockLabel = _doc.rootVisualElement.Q<Label>("Clock");
            _clockLabel.text = "0000-00-00 00:00:00";
            _fpsLabel = _doc.rootVisualElement.Q<Label>("FPSLabel");
            
            FindCameraManager();

            // Buttons
            _menuButton = _doc.rootVisualElement.Q<Button>("MenuButton");
            _menuButton.clicked += MenuButtonOnClicked;

            _freecamCameraButton = _doc.rootVisualElement.Q<Button>("Freecam");
            _freecamCameraButton.AddToClassList("button");
            _freecamCameraButton.clicked += FreecamCameraButtonClicked;

            _followCameraButton = _doc.rootVisualElement.Q<Button>("Follow");
            _followCameraButton.AddToClassList("button");
            _followCameraButton.clicked += FollowCameraButtonOnClicked;
            GreyOutButton(_followCameraButton);

            _driverCameraButton = _doc.rootVisualElement.Q<Button>("Driver");
            _driverCameraButton.AddToClassList("button");
            _driverCameraButton.clicked += DriverCameraButtonOnClicked;
            GreyOutButton(_driverCameraButton);
            
            _rewindButton = _doc.rootVisualElement.Q<Button>("Rewind");
            _rewindButton.clicked += RewindButtonOnClicked;

            _pauseButton = _doc.rootVisualElement.Q<Button>("Pause");
            _pauseButton.clicked += PauseButtonOnClicked;

            _pauseIcon = _doc.rootVisualElement.Q<VisualElement>("Pause-icon");
            _playIcon = _doc.rootVisualElement.Q<VisualElement>("Play-icon");

            _fastForwardButton = _doc.rootVisualElement.Q<Button>("Fastforward");
            _fastForwardButton.clicked += FastForwardButtonOnClicked;

            // Load settings
            _showFPS = PlayerPrefsGetBool(FPS_COUNTER);
            _clickSound.volume = PlayerPrefs.GetFloat("MasterVolume");
            _qualityMode = PlayerPrefsGetBool("SimulationMode");
            _carsToSpawn = PlayerPrefs.GetInt("CarsToSpawn");
        }

        private void Start()
        {
            // Set FPS visibility
            _fpsLabel.visible = PlayerPrefsGetBool(FPS_COUNTER);

            // Subscribe to events
            UserSelectManager.Instance.OnSelectedGameObject += selectedGameObject =>
            {
                if (selectedGameObject)
                {
                    RestoreButton(_driverCameraButton);
                    RestoreButton(_followCameraButton);
                }
                else
                {
                    GreyOutButton(_driverCameraButton);
                    GreyOutButton(_followCameraButton);
                }
            };
            
            _cameraManager.OnCameraChanged += type =>
            {
                // Reset all camera buttons to normal state
                RemoveCameraButtonHighlight(_freecamCameraButton);
                RemoveCameraButtonHighlight(_followCameraButton);
                RemoveCameraButtonHighlight(_driverCameraButton);

                // Highlight the correct camera button based on the camera type
                switch (type)
                {
                    case CameraManager.CustomCameraType.Freecam:
                        HighlightCameraButton(_freecamCameraButton);
                        break;
                    case CameraManager.CustomCameraType.Follow:
                        HighlightCameraButton(_followCameraButton);
                        break;
                    case CameraManager.CustomCameraType.Driver:
                        HighlightCameraButton(_driverCameraButton);
                        break;
                    default:
                        Debug.LogWarning("Unknown camera type.");
                        break;
                }
            };
            
            // Set the default camera button to highlighted
            HighlightCameraButton(_freecamCameraButton);
            ActivateCarSpawner();
        }

        private void ActivateCarSpawner()
        {
            OnSimulationStart?.Invoke();
        }

        private void DeactivateCarSpawner()
        {
            OnSimulationStop?.Invoke();
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
            PlayClickSound();
            DeactivateCarSpawner();
            _doc.rootVisualElement.visible = false;
            SceneManager.LoadScene(0,  LoadSceneMode.Single);
        }

        private void FreecamCameraButtonClicked()
        {
            PlayClickSound();
            _cameraManager.ToggleDefaultCamera();
        }

        private void FollowCameraButtonOnClicked()
        {
            PlayClickSound();
            _cameraManager.ToggleFollowCamera();
        }

        private void DriverCameraButtonOnClicked()
        {
            PlayClickSound();
            _cameraManager.ToggleDriverCamera();
        }

        private void RewindButtonOnClicked()
        {
            PlayClickSound();
            TimeManager.Instance.SetModeRewind();
        }

        private void PauseButtonOnClicked()
        {
            PlayClickSound();
            TimeManager.Instance.SetModePause();
            _isPaused = !_isPaused;
            _pauseIcon.visible = !_isPaused;
            _playIcon.visible = _isPaused;
        }

        private void FastForwardButtonOnClicked()
        {
            PlayClickSound();
            TimeManager.Instance.SetModeFastForward();
        }

        void Update()
        {
            _clockLabel.text = TimeManager.Instance.Timestamp;

            // FPS counter
            if(_showFPS && Time.time >= _fpsLastUpdateTime + 1f / _fpsUpdateFrequency)
                DisplayFPS(1f / Time.unscaledDeltaTime);
        }

        private void PlayClickSound()
        {
            _clickSound.Play();
        }

        private void DisplayFPS(float fps)
        {
            _fpsLabel.text = "FPS: " + fps.ToString("F0");
            _fpsLastUpdateTime = Time.time;
        }

        /// <summary>Wrapper to allow getting bools from PlayerPrefs</summary>
        private bool PlayerPrefsGetBool(string name)
        {
            return PlayerPrefs.GetInt(name, 0) == 1;
        }
    }
}