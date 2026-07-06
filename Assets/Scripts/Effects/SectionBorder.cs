using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a thin border around a section (ARRIVALS / TRANSITS / DEPARTURES)
/// through 4 thin Image strips. No texture needed.
/// 
/// Add sections to the root object (ArrivalsSection, etc.).
/// Stripes are created automatically as child objects BELOW all content.
/// </summary>
public class SectionBorder : MonoBehaviour
{
    [Header("Внешний вид")]
    public Color borderColor = new Color(0f, 1f, 0.314f, 0.22f); // thin green
    public int   thickness   = 1;                                  // 1 pixel

    void Awake()
    {
        CreateStrip("_BorderTop",    AnchorPreset.TopStretch,    0, thickness);
        CreateStrip("_BorderBottom", AnchorPreset.BottomStretch, 0, thickness);
        CreateStrip("_BorderLeft",   AnchorPreset.LeftStretch,   thickness, 0);
        CreateStrip("_BorderRight",  AnchorPreset.RightStretch,  thickness, 0);
    }

    enum AnchorPreset { TopStretch, BottomStretch, LeftStretch, RightStretch }

    void CreateStrip(string name, AnchorPreset anchor, int offsetH, int offsetV)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        Image img = go.GetComponent<Image>();
        img.color = borderColor;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();

        switch (anchor)
        {
            case AnchorPreset.TopStretch:
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0, thickness);
                rt.anchoredPosition = Vector2.zero;
                break;

            case AnchorPreset.BottomStretch:
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0, thickness);
                rt.anchoredPosition = Vector2.zero;
                break;

            case AnchorPreset.LeftStretch:
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot     = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(thickness, 0);
                rt.anchoredPosition = Vector2.zero;
                break;

            case AnchorPreset.RightStretch:
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(1f, 0.5f);
                rt.sizeDelta = new Vector2(thickness, 0);
                rt.anchoredPosition = Vector2.zero;
                break;
        }
    }
}
