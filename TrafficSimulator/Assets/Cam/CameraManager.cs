using Cinemachine;
using UnityEngine;
using User;
using CameraState = Cam.CameraState;

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
        [SerializeField] private CameraState[] _cameras;
        [SerializeField] private int _currentActiveCameraIndex = 0;
        [SerializeField] private int _previousActiveCameraIndex;
        [SerializeField] public Selectable CameraTarget;

        public CameraInputHandler InputHandler { get; private set; }
        private CinemachineBrain _cmBrain;
        private void Start()
        {
            InputHandler = new CameraInputHandler();
            _cmBrain = Camera.main.GetComponent<CinemachineBrain>();
            _cameras[FindDefaultCameraIndex()].SetActive(this);
        }

        private void Update()
        {
            if (_cmBrain.IsBlending) return;
            _cameras[_currentActiveCameraIndex].Look(InputHandler.LookDelta);
            _cameras[_currentActiveCameraIndex].Move(InputHandler.Movement);
            _cameras[_currentActiveCameraIndex].RotateHorizontal(InputHandler.Rotation);
            _cameras[_currentActiveCameraIndex].Zoom(InputHandler.Zoom);
        }

        private void OnDestroy()
        {
            InputHandler.Dispose();
        }
        
        public void ToggleThirdPersonCamera()
        {
            SwitchActiveCamera(1);
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
                if (_cameras[i].IsDefault) return i;
            }

            // Set the first camera to default
            Debug.LogWarning("No default camera has been assigned! Setting the camera with index 0 to default.");
            return 0;
        }
        
        public void ToggleFirstPersonDriverCamera()
        {
            SwitchActiveCamera(2);
        }

        public void ToggleDefaultCamera()
        {
            SwitchActiveCamera(0);
        }
    }
}