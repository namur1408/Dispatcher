using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// Управляет реальным качеством графики в игре.
/// LOW: отключает дождь, молнию, CRT-шум, снижает качество Unity.
/// HIGH: включает всё обратно.
/// Добавь этот компонент на тот же объект что и MainMenuController,
/// ИЛИ на отдельный DontDestroyOnLoad-объект.
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
        // Восстанавливаем сохранённую настройку
        int savedLevel = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.names.Length - 1);
        bool savedLow = savedLevel == 0;
        ApplyQuality(savedLow);
    }

    /// <summary>Вызови с true для LOW, false для HIGH</summary>
    public void ApplyQuality(bool low)
    {
        isLowQuality = low;

        if (low)
        {
            // --- Unity Quality Level ---
            QualitySettings.SetQualityLevel(0, true); // Very Low
            PlayerPrefs.SetInt("GraphicsQuality", 0);

            // --- Тени ---
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
        }
        else
        {
            // --- Unity Quality Level ---
            int highLevel = QualitySettings.names.Length - 1;
            QualitySettings.SetQualityLevel(highLevel, true);
            PlayerPrefs.SetInt("GraphicsQuality", highLevel);

            // --- Тени ---
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 50f;
        }

        PlayerPrefs.Save();

        // Применяем к эффектам в сцене
        StartCoroutine(ApplyToSceneEffects(low));

        Debug.Log($"[Graphics] Качество установлено: {(low ? "LOW" : "HIGH")}");
    }

    private IEnumerator ApplyToSceneEffects(bool low)
    {
        // Ждём кадр чтобы сцена точно была загружена
        yield return null;

        // --- Дождь / Шторм ---
        DynamicStorm[] storms = FindObjectsByType<DynamicStorm>(FindObjectsSortMode.None);
        foreach (var s in storms)
        {
            s.gameObject.SetActive(!low);
        }

        // --- Молния ---
        AdvancedStormLightning[] lightnings = FindObjectsByType<AdvancedStormLightning>(FindObjectsSortMode.None);
        foreach (var l in lightnings)
        {
            l.gameObject.SetActive(!low);
        }

        // --- CRT / Noise эффект ---
        CRTNoiseEffect[] crts = FindObjectsByType<CRTNoiseEffect>(FindObjectsSortMode.None);
        foreach (var c in crts)
        {
            c.gameObject.SetActive(!low);
        }

        // --- WindowLightning (вспышки в окне) ---
        WindowLightning[] windowLights = FindObjectsByType<WindowLightning>(FindObjectsSortMode.None);
        foreach (var w in windowLights)
        {
            w.gameObject.SetActive(!low);
        }

        // --- Мерцание лампы ---
        DeskLampFlicker[] lamps = FindObjectsByType<DeskLampFlicker>(FindObjectsSortMode.None);
        foreach (var lamp in lamps)
        {
            lamp.enabled = !low;
        }

        // --- Particle Systems (любые) ---
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
