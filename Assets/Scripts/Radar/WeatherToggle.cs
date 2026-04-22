using UnityEngine;
using UnityEngine.UI;

public class WeatherToggle : MonoBehaviour
{
    public static WeatherToggle Instance;

    [Header("Settings")]
    public GameObject stormZoneObject;
    private RawImage stormVisual;      

    private bool isWeatherVisible = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (stormZoneObject != null)
        {
            stormVisual = stormZoneObject.GetComponent<RawImage>();
            if (stormVisual != null) stormVisual.enabled = false;
        }
    }

    public void ToggleWeather()
    {
        if (stormVisual == null) return;

        isWeatherVisible = !isWeatherVisible;
        stormVisual.enabled = isWeatherVisible;
    }
}