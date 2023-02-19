using UnityEngine;

namespace Cam
{
    /// <summary>
    /// Singleton user input manager. Every input pooling should be communicated via this class to enable easy transitioning
    /// between different input schemes and correctly synchronizing which object that receives input events.   
    /// </summary>
    public class UserInputManager : MonoBehaviour
    {
        private PlayerInputActions _playerInputActions;
        public static UserInputManager Instance { get; private set; }
        public static PlayerInputActions PlayerInputActions => Instance._playerInputActions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
                Instance = this;
            
            _playerInputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            if (this != null) _playerInputActions.Default.Enable();
        }
    }
}