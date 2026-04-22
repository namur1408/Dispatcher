using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WXButtonController : MonoBehaviour
{
    [Header("Background Colors")]
    public Color offColor = new Color(0.1f, 0.2f, 0.1f);
    public Color onColor = new Color(0.2f, 0.8f, 0.2f);

    [Header("Text Colors")]
    public Color textOffColor = new Color(0.5f, 0.7f, 0.5f, 0.5f); 
    public Color textOnColor = Color.green;                      

    private Image btnImage;
    private TextMeshProUGUI btnText; 
    private bool isWXEnabled = false;

    void Awake()
    {
        btnImage = GetComponent<Image>();
        btnText = GetComponentInChildren<TextMeshProUGUI>();
        if (btnImage != null) btnImage.color = offColor;
        if (btnText != null) btnText.color = textOffColor;
    }

    public void ToggleWX()
    {
        isWXEnabled = !isWXEnabled;

        if (btnImage != null)
        {
            btnImage.color = isWXEnabled ? onColor : offColor;
        }

        if (btnText != null)
        {
            btnText.color = isWXEnabled ? textOnColor : textOffColor;
        }

        if (WeatherToggle.Instance != null)
            WeatherToggle.Instance.ToggleWeather();
    }
}