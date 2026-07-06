using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DeskLampFlicker : MonoBehaviour
{
    [Tooltip("Список источников света. Если оставить пустым, скрипт автоматически найдет все Light2D на этом объекте и внутри него.")]
    public Light2D[] lamps;

    [Header("Настройки мерцания")]
    [Tooltip("Насколько сильно меняется яркость (например, 0.1 означает колебания ±10%)")]
    public float flickerIntensity = 0.1f;
    
    [Tooltip("Скорость плавного мерцания")]
    public float flickerSpeed = 2.0f;

    [Header("Случайные сбои (необязательно)")]
    [Tooltip("Включить редкие резкие скачки/моргания света")]
    public bool enableGlitches = true;
    
    [Tooltip("Шанс сбоя в секунду")]
    public float glitchChancePerSecond = 0.5f;
    
    [Tooltip("Длительность сбоя")]
    public float glitchDuration = 0.05f;
    
    private float noiseOffset;
    private float glitchTimer = 0f;
    private float[] baseIntensities;

    void Awake()
    {
        // Single noise shift for the entire group (so they flicker in sync)
        noiseOffset = Random.Range(0f, 1000f); 
        
        // If you forgot to drag with handles, look for all Light2D on the object and its children
        if (lamps == null || lamps.Length == 0)
        {
            lamps = GetComponentsInChildren<Light2D>();
        }

        // We remember the initial brightness of each source
        if (lamps != null && lamps.Length > 0)
        {
            baseIntensities = new float[lamps.Length];
            for (int i = 0; i < lamps.Length; i++)
            {
                if (lamps[i] != null)
                {
                    baseIntensities[i] = lamps[i].intensity;
                }
            }
        }
    }

    void Update()
    {
        if (lamps == null || lamps.Length == 0) return;

        // 1. Calculate the TOTAL flicker multiplier for all lamps
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, 0f);
        // The multiplier will fluctuate, for example, from 0.9 to 1.1 (with flickerIntensity = 0.1)
        float multiplier = 1f + ((noise - 0.5f) * 2f * flickerIntensity);

        // 2. We calculate failures (also the same for everyone, so that they blink synchronously)
        if (enableGlitches)
        {
            if (glitchTimer > 0)
            {
                glitchTimer -= Time.deltaTime;
                // Sharp drop in brightness during a crash (from 30% to 70%)
                multiplier *= Random.Range(0.3f, 0.7f); 
            }
            else
            {
                // Chance of failure
                if (Random.value < glitchChancePerSecond * Time.deltaTime)
                {
                    glitchTimer = glitchDuration;
                }
            }
        }

        // 3. Apply synchronously to all lamps, taking into account their native brightness
        for (int i = 0; i < lamps.Length; i++)
        {
            if (lamps[i] != null)
            {
                lamps[i].intensity = Mathf.Max(0f, baseIntensities[i] * multiplier);
            }
        }
    }
}
