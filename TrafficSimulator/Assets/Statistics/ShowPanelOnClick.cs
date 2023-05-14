using UnityEngine;
using UnityEngine.UI;

public class ShowPanelOnClick : MonoBehaviour
{ 
    [SerializeField] private GameObject _panel;

    private bool _isPanelVisible = false;

    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(TogglePanel);
        // TODO: When finished with the graph tab, the panel should be inactive at start (set from editor)
        _isPanelVisible = _panel.activeSelf;
    }

    void TogglePanel()
    {
        _isPanelVisible = !_isPanelVisible;
        _panel.SetActive(_isPanelVisible);
    }
}
