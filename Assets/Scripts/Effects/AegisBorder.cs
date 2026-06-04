using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Добавь этот компонент на любую панель-фон.
/// Рисует рамку в стиле AEGIS (тонкие зелёные линии по краям) без лишних объектов.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class AegisBorder : MonoBehaviour
{
    public Color   borderColor = new Color(0f, 1f, 0.314f, 0.3f);
    public Color   fillColor   = new Color(0.012f, 0.055f, 0.024f, 0.9f);
    public int     borderWidth = 1;
    [Range(8, 512)] public int texRes = 64;

    private RawImage img;

    void Awake()
    {
        img = GetComponent<RawImage>();
        img.raycastTarget = false;
        Rebuild();
    }

    public void Rebuild()
    {
        int s = texRes;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            bool border = x < borderWidth || x >= s - borderWidth ||
                          y < borderWidth || y >= s - borderWidth;
            tex.SetPixel(x, y, border ? borderColor : fillColor);
        }
        tex.Apply();

        img.texture = tex;
        img.color   = Color.white;

        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
