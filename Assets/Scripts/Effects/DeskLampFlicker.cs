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
        // Единый сдвиг шума для всей группы (чтобы они мерцали синхронно)
        noiseOffset = Random.Range(0f, 1000f); 
        
        // Если забыли перетащить ручками, ищем все Light2D на объекте и его детях
        if (lamps == null || lamps.Length == 0)
        {
            lamps = GetComponentsInChildren<Light2D>();
        }

        // Запоминаем изначальную яркость каждого источника
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

        // 1. Считаем ОБЩИЙ множитель мерцания для всех ламп
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, 0f);
        // Множитель будет плавать, например, от 0.9 до 1.1 (при flickerIntensity = 0.1)
        float multiplier = 1f + ((noise - 0.5f) * 2f * flickerIntensity);

        // 2. Рассчитываем сбои (тоже одни на всех, чтобы моргало синхронно)
        if (enableGlitches)
        {
            if (glitchTimer > 0)
            {
                glitchTimer -= Time.deltaTime;
                // Резкое падение яркости во время сбоя (от 30% до 70%)
                multiplier *= Random.Range(0.3f, 0.7f); 
            }
            else
            {
                // Шанс сбоя
                if (Random.value < glitchChancePerSecond * Time.deltaTime)
                {
                    glitchTimer = glitchDuration;
                }
            }
        }

        // 3. Применяем синхронно ко всем лампам с учетом их родной яркости
        for (int i = 0; i < lamps.Length; i++)
        {
            if (lamps[i] != null)
            {
                lamps[i].intensity = Mathf.Max(0f, baseIntensities[i] * multiplier);
            }
        }
    }
}
