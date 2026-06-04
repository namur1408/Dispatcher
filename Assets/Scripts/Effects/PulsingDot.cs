using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Добавь на Image-объект — он будет пульсировать как "ONLINE"-точка в AEGIS.
/// </summary>
public class PulsingDot : MonoBehaviour
{
    [Header("Colors")]
    public Color onColor  = new Color(0f, 1f, 0.314f, 1f);     // ярко-зелёный
    public Color offColor = new Color(0f, 1f, 0.314f, 0.2f);   // тусклый

    [Header("Timing")]
    public float period   = 2f;   // секунд на полный цикл
    public float glowSize = 5f;   // радиус свечения

    private Image img;
    private float t;

    void Awake()
    {
        img = GetComponent<Image>();
        img.type = Image.Type.Simple;
        img.preserveAspect = false;

        // Размер НЕ меняем — задаётся в редакторе Unity
        img.sprite = CreateCircleSprite(32);
    }

    void Update()
    {
        t += Time.deltaTime / period * Mathf.PI * 2f;
        float alpha = Mathf.Lerp(0.25f, 1f, (Mathf.Sin(t) + 1f) * 0.5f);
        img.color = Color.Lerp(offColor, onColor, alpha);
    }

    static Sprite CreateCircleSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = res / 2f;
        float rad    = center - 1f;

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            float a    = Mathf.Clamp01(1f - (dist - rad));
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f);
    }
}
