using UnityEngine;
using UnityEngine.UI;

public class RadarGlow : MonoBehaviour
{
    [Header("Настройки неонового свечения")]
    public Color baseColor = Color.green;

    [Range(1f, 10f)]
    public float glowIntensity = 4f;

    void Start()
    {
        // Ищем все картинки
        Image[] radarImages = GetComponentsInChildren<Image>();

        foreach (Image img in radarImages)
        {
            // ВАЖНО: Пропускаем сам объект RadarGrid (наш фон), чтобы он не ослеплял нас
            if (img.gameObject == this.gameObject)
                continue;

            // Запоминаем оригинальную альфу (прозрачность) линии, чтобы сетка оставалась аккуратной
            float originalAlpha = img.color.a;

            // Применяем свечение только к цветам (RGB), а прозрачность возвращаем на место
            img.color = new Color(baseColor.r * glowIntensity, baseColor.g * glowIntensity, baseColor.b * glowIntensity, originalAlpha);
        }
    }
}