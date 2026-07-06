using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class GlobalPaperBend : MonoBehaviour
{
    [Range(-200f, 200f)]
    public float bendAmount = 0f;
    [Range(0f, 1f)]
    public float bendCenter = 0.5f;

    [Tooltip("Кол-во сегментов для самого фона бумаги (текст не нарезается)")]
    public int segments = 12;

    [HideInInspector] public Vector2 dropShadowDistance = Vector2.zero;
    [HideInInspector] public float dropShadowAlpha = 0f;
    [HideInInspector] public float foldShadowAlpha = 0f;

    private RectTransform paperRect;
    private float lastBendAmount;
    
    // List of child graphic elements
    private List<PaperBendChild> childrenModifiers = new List<PaperBendChild>();
    private List<TMP_Text> tmpTexts = new List<TMP_Text>();

    void Awake()
    {
        paperRect = GetComponent<RectTransform>();
    }

    void Start()
    {
        // Find all Graphic components (pictures, backgrounds and regular Text)
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            // We skip TextMeshPro as it is processed separately
            if (g is TMP_Text) continue;

            var modifier = g.gameObject.GetComponent<PaperBendChild>();
            if (modifier == null)
            {
                modifier = g.gameObject.AddComponent<PaperBendChild>();
            }
            modifier.paperBend = this;
            childrenModifiers.Add(modifier);
        }

        // Finding all TextMeshPro elements
        tmpTexts.AddRange(GetComponentsInChildren<TMP_Text>(true));
    }

    void Update()
    {
        // Update the geometry (threshold reduced for smoothness)
        if (Mathf.Abs(bendAmount - lastBendAmount) > 0.01f)
        {
            lastBendAmount = bendAmount;
            
            // 1. Update regular UI elements (Image and old Text)
            foreach (var mod in childrenModifiers)
            {
                if (mod != null)
                {
                    Graphic g = mod.GetComponent<Graphic>();
                    if (g != null) g.SetVerticesDirty();
                }
            }

            // 2. Warp TextMeshPro directly
            foreach (var tmp in tmpTexts)
            {
                if (tmp != null && tmp.isActiveAndEnabled)
                {
                    tmp.ForceMeshUpdate(); // Generating a smooth mesh
                    TMP_TextInfo textInfo = tmp.textInfo;

                    // Modifying the vertices of each symbol
                    for (int i = 0; i < textInfo.characterCount; i++)
                    {
                        if (!textInfo.characterInfo[i].isVisible) continue;

                        int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                        int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                        Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                        // Each symbol has 4 vertices
                        for (int vIndex = 0; vIndex < 4; vIndex++)
                        {
                            Vector3 origPos = sourceVertices[vertexIndex + vIndex];
                            
                            // Packed in UIVertex for compatibility with our function
                            UIVertex tempV = new UIVertex();
                            tempV.position = origPos;
                            
                            ApplyBendToVertex(ref tempV, tmp.rectTransform, false);
                            
                            sourceVertices[vertexIndex + vIndex] = tempV.position;
                        }
                    }

                    // Updating the TMP mesh
                    for (int i = 0; i < textInfo.materialCount; i++)
                    {
                        if (textInfo.meshInfo[i].mesh != null)
                        {
                            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                            tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                        }
                    }
                }
            }
        }
    }

    // This function is called by each child element when generating the mesh
    public void ApplyBendToVertex(ref UIVertex v, RectTransform childRect, bool isPaperBackground)
    {
        // 1. Convert the local coordinate of the vertex child to the local coordinate of paper
        Vector3 worldPos = childRect.TransformPoint(v.position);
        Vector3 localToPaper = paperRect.InverseTransformPoint(worldPos);

        // 2. Calculate the bend (mathematics of a parabola)
        float width = paperRect.rect.width;
        if (width == 0) width = 100f; // Divide by zero protection

        // Normalized X coordinate on paper (0 - left, 1 - right)
        float normalizedX = (localToPaper.x - paperRect.rect.xMin) / width;
        float dist = normalizedX - bendCenter;

        float bend = dist * dist * bendAmount;

        // 3. If this is a paper background, add a shadow (change the color of the vertices)
        if (isPaperBackground)
        {
            float shadowStrength = 0.15f;
            // The shadow now depends on the fact of raising (foldShadowAlpha), and not on the force of deflection, 
            // so that it does not disappear abruptly when passing through 0.
            float shadowMask = 1.0f - Mathf.Clamp01(Mathf.Abs(dist) * 3f) * shadowStrength * foldShadowAlpha;
            
            Color32 c = v.color;
            c.r = (byte)(c.r * shadowMask);
            c.g = (byte)(c.g * shadowMask);
            c.b = (byte)(c.b * shadowMask);
            v.color = c;
        }

        // 4. Apply a bend
        localToPaper.y += bend;
        localToPaper.z -= Mathf.Abs(bend) * 0.02f; // A little volume

        // 5. Return child back to local coordinates
        Vector3 newWorldPos = paperRect.TransformPoint(localToPaper);
        v.position = childRect.InverseTransformPoint(newWorldPos);
    }
}
