using UnityEngine;
using TMPro;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Draws tick marks (насечки) and arranges letters in a circle for the CaesarDisk.
/// Works in Edit Mode without Play — add to the same GameObject as CaesarDisk.
/// </summary>
[ExecuteAlways]
public class CaesarDiskRenderer : MonoBehaviour
{
    [Header("Letters Settings")]
    [Tooltip("Prefab for the letters (TextMeshProUGUI)")]
    public GameObject textPrefab;
    public string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    [Tooltip("Radius at which the letters are placed")]
    public float letterRadius = 80f;
    [Tooltip("Rotate letters so their bottom faces the center?")]
    public bool rotateLettersRadially = true;

    [Header("Tick Ring Radii")]
    [Tooltip("Distance from center where ticks END")]
    public float tickOuterRadius = 108f;
    [Tooltip("Distance from center where major ticks START")]
    public float majorTickInnerRadius = 92f;
    [Tooltip("Distance from center where minor ticks START")]
    public float minorTickInnerRadius = 100f;

    [Header("Major Ticks")]
    public int majorTickCount = 26;
    public float majorTickWidth = 2.5f;
    public Color majorTickColor = new Color(0.85f, 0.72f, 0.38f, 1f);

    [Header("Minor Ticks")]
    public int minorTicksPerMajor = 3;
    public float minorTickWidth = 1.2f;
    public Color minorTickColor = new Color(0.45f, 0.40f, 0.28f, 0.85f);

    [Header("Appearance")]
    public float startAngleOffset = -90f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private CanvasRenderer _cr;
    private Mesh _mesh;
    private Material _mat;

    private List<GameObject> _spawnedLetters = new List<GameObject>();

    void OnEnable()  => Rebuild();
    void OnValidate() => Rebuild(); // live update in Inspector

    public void Rebuild()
    {
        EnsureComponents();
        BuildMesh();
        BuildLetters();
    }

    void EnsureComponents()
    {
        if (_cr == null) _cr = gameObject.GetComponent<CanvasRenderer>();
        if (_cr == null) _cr = gameObject.AddComponent<CanvasRenderer>();
        if (_mat == null) _mat = Canvas.GetDefaultCanvasMaterial();
        _cr.SetMaterial(_mat, null);
    }

    void BuildMesh()
    {
        int totalTicks = majorTickCount * (1 + minorTicksPerMajor);
        int vertCount = totalTicks * 4;
        int idxCount  = totalTicks * 6;

        Vector3[] verts = new Vector3[vertCount];
        int[]     tris  = new int[idxCount];
        Color[]   cols  = new Color[vertCount];

        int vi = 0, ti = 0;

        float majorStep = 360f / majorTickCount;
        float minorStep = majorStep / (minorTicksPerMajor + 1);

        for (int m = 0; m < majorTickCount; m++)
        {
            float baseAngle = startAngleOffset + m * majorStep;

            // Major tick
            AddTick(verts, tris, cols, ref vi, ref ti,
                    baseAngle, majorTickInnerRadius, tickOuterRadius,
                    majorTickWidth, majorTickColor);

            // Minor ticks
            for (int k = 1; k <= minorTicksPerMajor; k++)
            {
                float a = baseAngle + k * minorStep;
                AddTick(verts, tris, cols, ref vi, ref ti,
                        a, minorTickInnerRadius, tickOuterRadius,
                        minorTickWidth, minorTickColor);
            }
        }

        if (_mesh == null) _mesh = new Mesh { name = "DiskTicks" };
        else _mesh.Clear();

        _mesh.vertices  = verts;
        _mesh.triangles = tris;
        _mesh.colors    = cols;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _cr.SetMesh(_mesh);
    }

    void BuildLetters()
    {
        if (textPrefab == null) return;

        // Ensure we have exactly the right number of children
        while (_spawnedLetters.Count < alphabet.Length)
        {
            GameObject go = Instantiate(textPrefab, transform);
            go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            _spawnedLetters.Add(go);
        }
        while (_spawnedLetters.Count > alphabet.Length)
        {
            int last = _spawnedLetters.Count - 1;
            if (_spawnedLetters[last] != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_spawnedLetters[last]);
#else
                Destroy(_spawnedLetters[last]);
#endif
            }
            _spawnedLetters.RemoveAt(last);
        }

        // Clean up any stray children that aren't in our list (e.g. from script reload)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (!_spawnedLetters.Contains(c.gameObject))
            {
#if UNITY_EDITOR
                DestroyImmediate(c.gameObject);
#else
                Destroy(c.gameObject);
#endif
            }
        }

        float step = 360f / alphabet.Length;
        for (int i = 0; i < alphabet.Length; i++)
        {
            GameObject go = _spawnedLetters[i];
            if (go == null) continue;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = alphabet[i].ToString();
                tmp.alignment = TextAlignmentOptions.Center;
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                
                float angle = startAngleOffset + i * step;
                float rad = angle * Mathf.Deg2Rad;
                
                rt.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * letterRadius;
                
                if (rotateLettersRadially)
                {
                    // Rotate so the letter points outwards
                    rt.localRotation = Quaternion.Euler(0, 0, angle - 90f);
                }
                else
                {
                    rt.localRotation = Quaternion.identity;
                }
            }
        }
    }

    static void AddTick(Vector3[] verts, int[] tris, Color[] cols,
                        ref int vi, ref int ti,
                        float angleDeg, float innerR, float outerR, float width, Color c)
    {
        float rad  = angleDeg * Mathf.Deg2Rad;
        Vector2 dir  = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 perp = new Vector2(-dir.y, dir.x) * (width * 0.5f);

        verts[vi+0] = (Vector3)(dir * innerR - perp);
        verts[vi+1] = (Vector3)(dir * innerR + perp);
        verts[vi+2] = (Vector3)(dir * outerR + perp);
        verts[vi+3] = (Vector3)(dir * outerR - perp);

        for (int v = 0; v < 4; v++) cols[vi + v] = c;

        tris[ti+0] = vi+0; tris[ti+1] = vi+1; tris[ti+2] = vi+2;
        tris[ti+3] = vi+0; tris[ti+4] = vi+2; tris[ti+5] = vi+3;

        vi += 4;
        ti += 6;
    }

    void OnDisable()
    {
        if (_cr != null) _cr.SetMesh(null);
    }
}
