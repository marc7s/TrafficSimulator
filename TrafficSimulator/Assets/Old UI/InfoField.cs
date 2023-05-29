using TMPro;
using UnityEngine;

public class InfoField : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _header;
    [SerializeField] private TextMeshProUGUI _value;

    public void Hide()
    {
        _header.enabled = false;
        _value.text = "";
    }

    public void Display(string valueText)
    {
        _header.enabled = true;
        _value.text = valueText;
    }

    public void Display(float value, string unit = null, int decimals = 2)
    {
        string format = $"F{decimals}";
        string valueText = value.ToString(format);
        Display(unit == null ? valueText : $"{valueText} {unit}");
    }

    public void DisplayNoHeader(string valueText)
    {
        _header.enabled = false;
        _value.text = valueText;
    }
}