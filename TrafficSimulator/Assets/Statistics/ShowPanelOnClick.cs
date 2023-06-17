using UnityEngine;
using UnityEngine.UI;

public class ShowPanelOnClick : MonoBehaviour
{ 
    [SerializeField] private GameObject _panel;
    [SerializeField] private Texture2D _hoverCursor;
    [SerializeField] private Vector2 _cursorHotspot = new Vector2(16, 0);
    private AudioSource _clickSound;

    private bool _isPanelVisible = false;

    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(TogglePanel);
        _isPanelVisible = _panel.activeSelf;

        _clickSound = GetComponent<AudioSource>();
        _clickSound.volume = PlayerPrefs.GetFloat("MasterVolume");
    }

    void TogglePanel()
    {
        PlayClickSound();
        _isPanelVisible = !_isPanelVisible;
        _panel.SetActive(_isPanelVisible);
    }

    private void PlayClickSound()
    {
        _clickSound.Play();
    }

    public void OnMouseEnter()
    {
        Cursor.SetCursor(_hoverCursor, _cursorHotspot, CursorMode.Auto);
    }

    public void OnMouseExit()
    {
        Cursor.SetCursor(null, _cursorHotspot, CursorMode.Auto);
    }
}
