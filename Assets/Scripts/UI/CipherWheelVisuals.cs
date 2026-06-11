using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds visual enhancements to the CipherWheel drum:
///  - Highlight bar behind the active (center) letter
///  - Top and bottom gradient fade masks (dark → transparent)
///  - Thin separator lines between letter slots
///  - Optional outer frame / window lines
///
/// Attach to the same GameObject as CipherWheel.
/// Works in Edit Mode too (ExecuteAlways).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(CipherWheel))]
public class CipherWheelVisuals : MonoBehaviour
{
    // ── Highlight bar ─────────────────────────────────────────────────────────
    [Header("Center Highlight")]
    public bool showHighlight = true;
    [Tooltip("Width of the highlight bar (should match the wheel container width)")]
    public float highlightWidth = 80f;
    [Tooltip("Height of the highlight bar (roughly one letter-slot height)")]
    public float highlightHeight = 40f;
    public Color highlightColor = new Color(0.55f, 0.25f, 0.08f, 0.55f);  // warm amber glow
    [Tooltip("Color of the top and bottom border lines of the highlight")]
    public Color highlightBorderColor = new Color(0.85f, 0.60f, 0.20f, 0.90f); // gold
    public float highlightBorderThickness = 2f;

    // ── Top / bottom gradient fade ─────────────────────────────────────────────
    [Header("Gradient Fade (top & bottom)")]
    public bool showFade = true;
    [Tooltip("Total height of each fade zone (top and bottom)")]
    public float fadeHeight = 55f;
    [Tooltip("Full drum height (should match the wheel RectTransform height)")]
    public float drumHeight = 160f;
    public Color fadeColor = new Color(0.07f, 0.06f, 0.05f, 1f);  // near-black matching bg

    // ── Separator lines ────────────────────────────────────────────────────────
    [Header("Separator Lines")]
    public bool showSeparators = false; // optional, can be too busy
    public int separatorCount = 4;      // lines between visible letters
    public float separatorWidth = 60f;
    public float separatorThickness = 1f;
    public Color separatorColor = new Color(0.50f, 0.40f, 0.20f, 0.30f);

    // ── Frame / window ────────────────────────────────────────────────────────
    [Header("Outer Frame Lines")]
    public bool showFrame = true;
    public float frameWidth = 80f;
    public float frameHeight = 160f;
    public float frameThickness = 2.5f;
    public Color frameColor = new Color(0.55f, 0.45f, 0.25f, 0.85f); // gold frame

    // ─────────────────────────────────────────────────────────────────────────
    private Material _mat;

    void OnEnable()   => Rebuild();
    void OnValidate() => Rebuild();

    void Rebuild()
    {
        // Clean up old visual children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.name.StartsWith("__CWV_"))
            {
#if UNITY_EDITOR
                DestroyImmediate(c.gameObject);
#else
                Destroy(c.gameObject);
#endif
            }
        }

        if (_mat == null) _mat = Canvas.GetDefaultCanvasMaterial();

        int order = 0;

        // Highlight bar (rendered BEHIND the letters, so insert at start of sibling order)
        if (showHighlight)
        {
            MakeQuad("__CWV_Highlight", 0, 0, highlightWidth, highlightHeight, highlightColor, order++);
            // Top border line
            MakeQuad("__CWV_HLBorderTop",    0,  highlightHeight * 0.5f - highlightBorderThickness * 0.5f,
                      highlightWidth, highlightBorderThickness, highlightBorderColor, order++);
            // Bottom border line
            MakeQuad("__CWV_HLBorderBot",    0, -highlightHeight * 0.5f + highlightBorderThickness * 0.5f,
                      highlightWidth, highlightBorderThickness, highlightBorderColor, order++);
        }

        // Separator lines
        if (showSeparators && separatorCount > 0)
        {
            float spacing = drumHeight / (separatorCount + 1f);
            for (int i = 1; i <= separatorCount; i++)
            {
                float y = -drumHeight * 0.5f + spacing * i;
                MakeQuad($"__CWV_Sep{i}", 0, y, separatorWidth, separatorThickness, separatorColor, order);
            }
            order++;
        }

        // Gradient fades (top and bottom) — drawn on top of letters
        if (showFade)
        {
            MakeGradientQuad("__CWV_FadeTop",
                              0, drumHeight * 0.5f - fadeHeight * 0.5f,
                              highlightWidth + 10f, fadeHeight,
                              new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f),   // top opaque
                              new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f),   // bottom transparent
                              order++);

            MakeGradientQuad("__CWV_FadeBot",
                              0, -drumHeight * 0.5f + fadeHeight * 0.5f,
                              highlightWidth + 10f, fadeHeight,
                              new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f),   // top transparent
                              new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f),   // bottom opaque
                              order++);
        }

        // Outer frame lines (left, right, top, bottom)
        if (showFrame)
        {
            float hw = frameWidth  * 0.5f;
            float hh = frameHeight * 0.5f;
            float t  = frameThickness;

            // top
            MakeQuad("__CWV_FrTop",  0,  hh - t*0.5f, frameWidth, t, frameColor, order);
            // bottom
            MakeQuad("__CWV_FrBot",  0, -hh + t*0.5f, frameWidth, t, frameColor, order);
            // left
            MakeQuad("__CWV_FrL",  -hw + t*0.5f, 0, t, frameHeight, frameColor, order);
            // right
            MakeQuad("__CWV_FrR",   hw - t*0.5f, 0, t, frameHeight, frameColor, order);
            order++;
        }
    }

    // ── Flat solid quad ────────────────────────────────────────────────────────
    void MakeQuad(string name, float cx, float cy, float w, float h, Color col, int siblingOrder)
    {
        float hw = w * 0.5f, hh = h * 0.5f;
        Vector3[] v = {
            new Vector3(cx - hw, cy - hh, 0),
            new Vector3(cx + hw, cy - hh, 0),
            new Vector3(cx + hw, cy + hh, 0),
            new Vector3(cx - hw, cy + hh, 0),
        };
        int[]   t = { 0,1,2, 0,2,3 };
        Color[] c = { col, col, col, col };
        MakeLayer(name, v, t, c, siblingOrder);
    }

    // ── Vertically gradient quad (top color → bottom color) ───────────────────
    void MakeGradientQuad(string name, float cx, float cy, float w, float h,
                          Color topCol, Color botCol, int siblingOrder)
    {
        float hw = w * 0.5f, hh = h * 0.5f;
        // Unity mesh: verts in CCW, y+ = up
        // bot-left, bot-right, top-right, top-left
        Vector3[] v = {
            new Vector3(cx - hw, cy - hh, 0),  // bot-left
            new Vector3(cx + hw, cy - hh, 0),  // bot-right
            new Vector3(cx + hw, cy + hh, 0),  // top-right
            new Vector3(cx - hw, cy + hh, 0),  // top-left
        };
        int[]   t = { 0,1,2, 0,2,3 };
        Color[] c = { botCol, botCol, topCol, topCol };
        MakeLayer(name, v, t, c, siblingOrder);
    }

    // ── Low-level: create child with CanvasRenderer + mesh ────────────────────
    void MakeLayer(string name, Vector3[] verts, int[] tris, Color[] cols, int siblingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        CanvasRenderer cr = go.AddComponent<CanvasRenderer>();

        Mesh mesh = new Mesh { name = name + "_m" };
        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.colors    = cols;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        cr.SetMesh(mesh);
        cr.SetMaterial(_mat, null);

        go.transform.SetSiblingIndex(Mathf.Min(siblingOrder, transform.childCount - 1));
    }

    void OnDisable()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.name.StartsWith("__CWV_"))
            {
#if UNITY_EDITOR
                DestroyImmediate(c.gameObject);
#else
                Destroy(c.gameObject);
#endif
            }
        }
    }
}
