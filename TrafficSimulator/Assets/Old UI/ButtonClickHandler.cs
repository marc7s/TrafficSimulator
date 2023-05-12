using TMPro;
using UnityEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Old_UI
{
    [System.Serializable]
    public class ButtonClickEvent : UnityEvent {}

    public class ButtonClickHandler : MonoBehaviour
    {
        public ButtonClickEvent OnClick;

        private void Start()
        {
            // Get the Button component
            var button = GetComponent<Button>();

            // Add a listener to the Button's onClick event
            button.onClick.AddListener(TriggerClickEvent);
        }

        private void TriggerClickEvent()
        {
            // Trigger the custom onClick event
            OnClick.Invoke();
        }
    }
}