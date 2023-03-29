using UnityEngine;
using DataModel;
using CustomProperties;

namespace RoadGenerator
{
    public abstract class InfoScript<T> : MonoBehaviour
    {
        [HideInInspector] private T _reference;
        [HideInInspector] public bool _isInitialized = false;

        public bool IsInitialized {
            get => _isInitialized;
        }
        /// <summary> Sets the reference this script will display info for </summary>
        public void SetReference(T reference)
        {
            SetInfoFromReference(reference);

            _isInitialized = true;
        }
        protected virtual void SetInfoFromReference(T reference) {}
    }
}