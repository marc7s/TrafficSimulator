using UnityEngine;
using UnityEngine.UI;

public class ShowPanelOnClick : MonoBehaviour
{
    public GameObject panel;

    private bool isPanelVisible;

    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(TogglePanel);
        isPanelVisible = panel.activeSelf;
    }

    void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        panel.SetActive(isPanelVisible);
    }
}
