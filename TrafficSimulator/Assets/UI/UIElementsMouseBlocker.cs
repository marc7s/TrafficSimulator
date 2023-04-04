using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using User;

namespace UI
{
    public class UIElementsMouseBlocker : MonoBehaviour
    {
        // variable to hold the panel ui element
        private readonly List<VisualElement> _visualElements = new List<VisualElement>();

        private void Start()
        {
            // query and store the panel ui element
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            // Supply the name of the root element whose children you want to add
            string rootElementName = "CameraAndModeButtons";
            VisualElement targetRootElement = root.Q<VisualElement>(rootElementName);

            if (targetRootElement != null)
            {
                // Add all child VisualElements of the target root element to the list
                foreach (VisualElement child in targetRootElement.Children())
                {
                    if (child != null)
                    {
                        _visualElements.Add(child);
                    }
                }

                // Register callbacks for MouseEnter and MouseLeave events on the panel elements
                RegisterMouseCallbacksForElements();
            }
            else
            {
                Debug.LogWarning($"Root element with name {rootElementName} not found.");
            }
        }


        // Callback to be called when the Mouse enters the panel.
        private void MouseEnterCallback(MouseEnterEvent evt)
        {
            // Check if the target of the event is one of the visual elements in the list.
            if (_visualElements.Contains(evt.target as VisualElement))
            {
                UserSelectManager.Instance.IsHoveringUIElement = true;
            }
        }

        // Callback to be called when the Mouse leaves the panel.
        private void MouseLeaveCallback(MouseLeaveEvent evt)
        {
            // Check if the target of the event is one of the visual elements in the list.
            if (_visualElements.Contains(evt.target as VisualElement))
            {
                UserSelectManager.Instance.IsHoveringUIElement = false;
            }
        }

        private void RegisterMouseCallbacksForElements()
        {
            foreach (VisualElement visualElement in _visualElements)
            {
                visualElement.RegisterCallback<MouseEnterEvent>(MouseEnterCallback);
                visualElement.RegisterCallback<MouseLeaveEvent>(MouseLeaveCallback);
            }
        }
    }
}