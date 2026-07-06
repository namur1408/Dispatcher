using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// Controls the actual graphics quality in the game.
/// LOW: disables rain, lightning, CRT noise, reduces Unity quality.
/// HIGH: turns everything back on.
/// Add this component to the same object as MainMenuController,
/// OR to a separate DontDestroyOnLoad object.
/// </summary>
public class GraphicsQualityManager : MonoBehaviour
{
    public static GraphicsQualityManager Instance;

    [Header("Ключ LOW = true, HIGH = false")]
    [SerializeField] private bool isLowQuality = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Restoring a saved setting
        int savedLevel = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.names.Length - 1);
        bool savedLow = savedLevel == 0;
        ApplyQuality(savedLow);
    }

    /// <summary>Call with true for LOW, false for HIGH</summary>
    public void ApplyQuality(bool low)
    {
        isLowQuality = low;

        if (low)
        {
            // --- Unity Quality Level ---
            QualitySettings.SetQualityLevel(0, true); // Very Low
            PlayerPrefs.SetInt("GraphicsQuality", 0);

            // --- Shadows ---
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
        }
        else
        {
            // --- Unity Quality Level ---
            int highLevel = QualitySettings.names.Length - 1;
            QualitySettings.SetQualityLevel(highLevel, true);
            PlayerPrefs.SetInt("GraphicsQuality", highLevel);

            // --- Shadows ---
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 50f;
        }

        PlayerPrefs.Save();

        // Apply to effects in the scene
        StartCoroutine(ApplyToSceneEffects(low));

        Debug.Log($"[Graphics] Качество установлено: {(low ? "LOW" : "HIGH")}");
    }

    private IEnumerator ApplyToSceneEffects(bool low)
    {
        // We are waiting for the frame so that the scene is definitely loaded
        yield return null;

        // ---Rain/Storm ---
        DynamicStorm[] storms = FindObjectsByType<DynamicStorm>(FindObjectsSortMode.None);
        foreach (var s in storms)
        {
            s.gameObject.SetActive(!low);
        }

        // --- Lightning ---
        AdvancedStormLightning[] lightnings = FindObjectsByType<AdvancedStormLightning>(FindObjectsSortMode.None);
        foreach (var l in lightnings)
        {
            l.gameObject.SetActive(!low);
        }

        // --- CRT/Noise effect ---
        CRTNoiseEffect[] crts = FindObjectsByType<CRTNoiseEffect>(FindObjectsSortMode.None);
        foreach (var c in crts)
        {
            c.gameObject.SetActive(!low);
        }

        // --- WindowLightning (flashes in the window) ---
        WindowLightning[] windowLights = FindObjectsByType<WindowLightning>(FindObjectsSortMode.None);
        foreach (var w in windowLights)
        {
            w.gameObject.SetActive(!low);
        }

        // --- Lamp flickering ---
        DeskLampFlicker[] lamps = FindObjectsByType<DeskLampFlicker>(FindObjectsSortMode.None);
        foreach (var lamp in lamps)
        {
            lamp.enabled = !low;
        }

        // --- Particle Systems (any) ---
        ParticleSystem[] particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in particles)
        {
            if (low)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else
                ps.Play();
        }

        Debug.Log($"[Graphics] Эффекты сцены {(low ? "отключены" : "включены")}: " +
                  $"storms={storms.Length}, lightnings={lightnings.Length}, crts={crts.Length}");
    }
}
