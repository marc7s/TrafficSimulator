using Cam;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Simulation;
using User;
using System;
using System.Collections.Generic;

namespace UI 
{
    public enum ManualCarInputMode
    {
        Keyboard,
        Controller,
        FanatecWheel
    }

    public class OverlayController : MonoBehaviour
    {
        private UIDocument _doc;
        private AudioSource _clickSound;
        private CameraManager _cameraManager;
        private GameObject _userManager;
        private GameObject _uiGraphs;
        private GameObject _mainCamera;
        
        // Overlay button
        private Button _menuButton;

        // Manual car
        private Button _driveButton;
        private bool _isDrivingManually = false;
        [SerializeField] private GameObject _manualCarKeyboardPrefab;
        [SerializeField] private GameObject _manualCarControllerPrefab;
        [SerializeField] private GameObject _manualCarFanatecWheelPrefab;
        [SerializeField] private Vector3 _manualCarSpawnPosition = Vector3.zero;
        [SerializeField] [Range(0, 360)] private float _manualCarSpawnRotation = 0;

        // Camera buttons
        private StyleSheet _cameraButtonStyles;
        private VisualElement _cameraButtonContainer;
        private Button _freecamCameraButton;
        private Button _followCameraButton;
        private Button _driverCameraButton;
        private Button _currentlyHighlightedCameraButton;

        // Clock buttons
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

        public int CarsToSpawn => _carsToSpawn;

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
            FindUserManager();
            FindUIGraphs();
            FindMainCamera();

            // Buttons
            _menuButton = _doc.rootVisualElement.Q<Button>("MenuButton");
            _menuButton.clicked += MenuButtonOnClicked;

            _driveButton = _doc.rootVisualElement.Q<Button>("DriveButton");
            _driveButton.clicked += DriveButtonOnClicked;

            _cameraButtonContainer = _doc.rootVisualElement.Q<VisualElement>("CameraAndModeButtons");

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
        }

        private ManualCarInputMode GetManualCarInputMode()
        {
            switch(PlayerPrefs.GetString("ManualCarInputMode"))
            {
                case "Keyboard":
                    return ManualCarInputMode.Keyboard;
                case "Controller":
                    return ManualCarInputMode.Controller;
                case "Fanatec Wheel":
                    return ManualCarInputMode.FanatecWheel;
                default:
                    return ManualCarInputMode.Keyboard;
            }
        }

        private GameObject GetManualCarPrefab(ManualCarInputMode inputMode)
        {
            switch(inputMode)
            {
                case ManualCarInputMode.Keyboard:
                    return _manualCarKeyboardPrefab;
                case ManualCarInputMode.Controller:
                    return _manualCarControllerPrefab;
                case ManualCarInputMode.FanatecWheel:
                    return _manualCarFanatecWheelPrefab;
                default:
                    return _manualCarKeyboardPrefab;
            }
        }

        private void DeactivateCarSpawner()
        {
            OnSimulationStop?.Invoke();
        }

        private void ToggleManualDrive()
        {
            _isDrivingManually = !_isDrivingManually;
            _driveButton.text = _isDrivingManually ? "Exit" : "Drive";
            _cameraButtonContainer.visible = !_isDrivingManually;

            if(_isDrivingManually)
            {
                GameObject manualCar = GameObject.Instantiate(GetManualCarPrefab(GetManualCarInputMode()));
                manualCar.transform.position = _manualCarSpawnPosition;
                manualCar.transform.rotation = Quaternion.Euler(0, _manualCarSpawnRotation, 0);
                
                ChangeActiveState(new List<GameObject> { _cameraManager.gameObject, _userManager, _uiGraphs, _mainCamera }, false);
            }
            else
            {
                foreach(GameObject manualCar in GameObject.FindGameObjectsWithTag("ManualCar"))
                    Destroy(manualCar);

                ChangeActiveState(new List<GameObject> { _cameraManager.gameObject, _userManager, _uiGraphs, _mainCamera }, true);
                
                if(_userManager != null)
                    _userManager.GetComponent<UserSelectManager>()?.Start();

                if(_cameraManager != null)
                    _cameraManager.Start();
            }
        }

        private void ChangeActiveState(List<GameObject> gameObjects, bool newState)
        {
            foreach (GameObject go in gameObjects)
                go?.SetActive(newState);
        }
        
        private void FindCameraManager()
        {
            GameObject go = GameObject.FindGameObjectWithTag("CameraManager");
            if (go != null)
                _cameraManager = go.GetComponent<CameraManager>();
        }

        private void FindUserManager()
        {
            GameObject go = GameObject.FindGameObjectWithTag("UserManager");
            if (go != null)
                _userManager = go;
        }

        private void FindUIGraphs()
        {
            GameObject go = GameObject.FindGameObjectWithTag("UIGraphs");
            if (go != null)
                _uiGraphs = go;
        }

        private void FindMainCamera()
        {
            GameObject go = GameObject.FindGameObjectWithTag("MainCamera");
            if (go != null)
                _mainCamera = go;
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
            
            // Return to camera mode if driving manually when pressing menu button
            if(_isDrivingManually)
                ToggleManualDrive();
            
            SceneManager.LoadScene(0,  LoadSceneMode.Single);
        }

        private void DriveButtonOnClicked()
        {
            PlayClickSound();
            ToggleManualDrive();
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