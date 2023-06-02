using Cinemachine;
using UnityEngine;
using User;

namespace Cam
{
    /// <summary>
    /// State manager used to handle cameras. It allows switching between multiple camera states,
    /// with the current active camera defined by an index. The script also handles user input for camera movement, rotation, zoom, and other actions,
    /// and toggling between different cameras based on user input. Additionally, the script contains methods for setting up input actions and subscribing
    /// to user input events.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public enum CustomCameraType
        {
            Freecam = 0,
            Follow = 1,
            Driver = 2
        }
        
        public delegate void CameraChangedEventHandler(CustomCameraType newCustomCameraType);
        public event CameraChangedEventHandler OnCameraChanged;
        
        [SerializeField] private CameraState[] _cameras;
        [SerializeField] private int _currentActiveCameraIndex = 0;
        [SerializeField] private int _previousActiveCameraIndex;
        [SerializeField] public Selectable CameraTarget;
        
        [Header("Initial Position")]
        [SerializeField] private Vector3 _initialCameraPosition;
        [SerializeField] private bool _overrideInitialPosition = false;

        public CameraInputHandler InputHandler { get; private set; }
        private CinemachineBrain _cmBrain;
        private void Start()
        {
            InputHandler = new CameraInputHandler();
            _cmBrain = Camera.main.GetComponent<CinemachineBrain>();
            
            if(_cmBrain == null)
            {
                Debug.LogError("There must be a CinemachineBrain component on the main camera");
                return;
            }
            
            _currentActiveCameraIndex = FindDefaultCameraIndex();
            _cameras[_currentActiveCameraIndex].SetActive(this, _overrideInitialPosition ? _initialCameraPosition : null);
        }

        private void Update()
        {
            if (_cmBrain == null || _cmBrain.IsBlending) 
                return;
            
            _cameras[_currentActiveCameraIndex].Look(InputHandler.LookDelta);
            _cameras[_currentActiveCameraIndex].Move(new Vector3(InputHandler.Movement.x, 0, InputHandler.Movement.y));
            
            if(InputHandler.MouseOrigin != null)
                _cameras[_currentActiveCameraIndex].Rotate(InputHandler.MouseOrigin.Value);
            
            _cameras[_currentActiveCameraIndex].Zoom(InputHandler.Zoom);
        }

        private void OnDestroy()
        {
            InputHandler.Dispose();
        }
        
        public void ToggleFollowCamera()
        {
            SwitchActiveCamera((int) CustomCameraType.Follow);
            OnCameraChanged?.Invoke(CustomCameraType.Follow);
        }

        private void SwitchActiveCamera(int newIndex)
        {
            _previousActiveCameraIndex = _currentActiveCameraIndex;
            _currentActiveCameraIndex = newIndex;

            _cameras[_previousActiveCameraIndex].SetInactive(this);
            _cameras[_currentActiveCameraIndex].SetFollowTransform(CameraTarget.transform);
            _cameras[_currentActiveCameraIndex].SetActive(this);
        }

        private int FindDefaultCameraIndex()
        {
            for (int i = 0; i < _cameras.Length; i++)
            {
                if (_cameras[i].IsDefault) 
                    return i;
            }

            // Set the first camera to default
            Debug.LogWarning("No default camera has been assigned! Setting the camera with index 0 to default.");
            return 0;
        }
        
        public void ToggleDriverCamera()
        {
            SwitchActiveCamera((int) CustomCameraType.Driver);
            OnCameraChanged?.Invoke(CustomCameraType.Driver);
        }

        public void ToggleDefaultCamera()
        {
            SwitchActiveCamera((int) CustomCameraType.Freecam);
            OnCameraChanged?.Invoke(CustomCameraType.Freecam);
        }
    }
}