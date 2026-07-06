using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Add a background to the Image of any panel.
/// Draws a dark green mesh procedurally - no textures needed.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class GridBackground : MonoBehaviour
{
    [Header("Grid settings")]
    public Color backgroundColor = new Color(0.012f, 0.055f, 0.024f, 1f); // #030e06
    public Color lineColor        = new Color(0f, 1f, 0.314f, 0.05f);      // green, very transparent
    public int cellSize  = 32;  // pixels per cell
    public int lineWidth = 1;   // line width

    private RawImage img;

    void Awake()
    {
        img = GetComponent<RawImage>();
        img.raycastTarget = false;

        BuildTexture();

        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void BuildTexture()
    {
        int size = cellSize;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode  = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool onLine = (x < lineWidth) || (y < lineWidth);
            tex.SetPixel(x, y, onLine ? lineColor : backgroundColor);
        }
        tex.Apply();

        img.texture = tex;
        img.uvRect  = new Rect(0, 0, 40, 40); // tailing
        img.color   = Color.white;
    }
}
