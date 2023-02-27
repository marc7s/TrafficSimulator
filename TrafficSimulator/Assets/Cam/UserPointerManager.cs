using UnityEngine;

namespace Cam
{
    public class UserPointerManager : MonoBehaviour
    {
        private static UserPointerManager _instance;
        
        public static UserPointerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UserPointerManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = nameof(UserPointerManager);
                        _instance = obj.AddComponent<UserPointerManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
    }
}