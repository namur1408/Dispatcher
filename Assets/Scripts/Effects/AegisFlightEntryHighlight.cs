using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Add this component to Image in each line of the flight list.
/// When hovering/selecting, it lights up with a green frame like in AEGIS.
/// Works with Button or via SelectFlightEntry.Select().
/// </summary>
[RequireComponent(typeof(Image))]
public class AegisFlightEntryHighlight : MonoBehaviour
{
    [Header("Colors")]
    public Color normalColor    = new Color(0f,    1f, 0.314f, 0.03f);
    public Color hoverColor     = new Color(0f,    1f, 0.314f, 0.08f);
    public Color selectedColor  = new Color(0f,    1f, 0.314f, 0.12f);

    [Header("Left accent bar")]
    public bool  showAccentBar  = true;
    public float accentWidth    = 2f;

    private Image    bg;
    private Image    accent;
    private bool     _selected  = false;
    private bool     _hovered   = false;

    void Awake()
    {
        bg = GetComponent<Image>();
        bg.color = normalColor;

        if (showAccentBar)
            accent = CreateAccent();
    }

    Image CreateAccent()
    {
        GameObject go = new GameObject("_AccentBar", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();

        Image img = go.GetComponent<Image>();
        img.color = new Color(0f, 1f, 0.314f, 0f); // transparent by default
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(accentWidth, 0);
        rt.anchoredPosition = Vector2.zero;

        return img;
    }

    public void SetSelected(bool value)
    {
        _selected = value;
        Refresh();
    }

    public void OnPointerEnter() { _hovered = true;  Refresh(); }
    public void OnPointerExit()  { _hovered = false; Refresh(); }

    void Refresh()
    {
        if (_selected)
        {
            bg.color = selectedColor;
            if (accent) accent.color = new Color(0f, 1f, 0.314f, 1f);
        }
        else if (_hovered)
        {
            bg.color = hoverColor;
            if (accent) accent.color = new Color(0f, 1f, 0.314f, 0.6f);
        }
        else
        {
            bg.color = normalColor;
            if (accent) accent.color = new Color(0f, 1f, 0.314f, 0f);
        }
    }
}
