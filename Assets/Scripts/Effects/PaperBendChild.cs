using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class PaperBendChild : BaseMeshEffect
{
    [HideInInspector]
    public GlobalPaperBend paperBend;
    private RectTransform rectTransform;

    protected override void Awake()
    {
        base.Awake();
        rectTransform = GetComponent<RectTransform>();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || paperBend == null || vh.currentVertCount == 0) return;

        bool isPaperBackground = (gameObject == paperBend.gameObject);

        // If it's a paper background, we ALWAYS completely rebuild the mesh (ignoring 9-slice or old shadows)
        if (isPaperBackground)
        {
            SubdivideQuad(vh);
        }

        // We pull out all the vertices (text, icons or already cut background)
        List<UIVertex> verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        // Apply bending math to each vertex
        for (int i = 0; i < verts.Count; i++)
        {
            UIVertex v = verts[i];
            paperBend.ApplyBendToVertex(ref v, rectTransform, isPaperBackground);
            verts[i] = v;
        }

        // Fill it back into the mesh
        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }

    // ── Cutting a quad into a fine mesh + shadow generation ──
    private void SubdivideQuad(VertexHelper vh)
    {
        // 1. Extract the color and UV from the original vertices (taking the first one available)
        List<UIVertex> origVerts = new List<UIVertex>();
        vh.GetUIVertexStream(origVerts);
        if (origVerts.Count == 0) return;

        Color32 color = origVerts[0].color;

        // UV coordinates for a standard Image are usually from 0 to 1, unless it is an atlas.
        // For simplicity, we take the basic boundaries of the RectTransform itself:
        Rect rect = rectTransform.rect;
        Vector2 min = new Vector2(rect.xMin, rect.yMin);
        Vector2 max = new Vector2(rect.xMax, rect.yMax);
        
        Vector2 uvMin = new Vector2(0, 0);
        Vector2 uvMax = new Vector2(1, 1);

        // If the image is simple (4 vertices), use its original UVs
        if (origVerts.Count == 6) // 6 vertices = 2 triangles = 1 quad
        {
            uvMin = origVerts[0].uv0;
            uvMax = origVerts[0].uv0;
            for (int i = 1; i < origVerts.Count; i++)
            {
                uvMin = Vector2.Min(uvMin, origVerts[i].uv0);
                uvMax = Vector2.Max(uvMax, origVerts[i].uv0);
            }
        }

        vh.Clear();

        int segsX = paperBend.segments;
        int segsY = Mathf.Max(2, paperBend.segments / 2);

        // Function to generate mesh (with offset and color)
        void GenerateGrid(Vector2 offset, Color32 gridColor)
        {
            int startIndex = vh.currentVertCount;
            
            for (int y = 0; y <= segsY; y++)
            {
                for (int x = 0; x <= segsX; x++)
                {
                    float tx = (float)x / segsX;
                    float ty = (float)y / segsY;

                    UIVertex v = new UIVertex();
                    v.position = new Vector3(Mathf.Lerp(min.x, max.x, tx) + offset.x, Mathf.Lerp(min.y, max.y, ty) + offset.y, 0f);
                    v.uv0 = new Vector4(Mathf.Lerp(uvMin.x, uvMax.x, tx), Mathf.Lerp(uvMin.y, uvMax.y, ty), 0, 0);
                    v.color = gridColor;
                    vh.AddVert(v);
                }
            }

            int cols = segsX + 1;
            for (int y = 0; y < segsY; y++)
            {
                for (int x = 0; x < segsX; x++)
                {
                    int i = startIndex + y * cols + x;
                    vh.AddTriangle(i, i + cols, i + 1);
                    vh.AddTriangle(i + 1, i + cols, i + cols + 1);
                }
            }
        }

        // If the shadow transparency is greater than 0, first generate a shadow mesh
        if (paperBend.dropShadowAlpha > 0.01f)
        {
            Color32 shadowColor = new Color(0, 0, 0, paperBend.dropShadowAlpha);
            GenerateGrid(paperBend.dropShadowDistance, shadowColor);
        }

        // Then we generate the paper itself (without offset)
        GenerateGrid(Vector2.zero, color);
    }
}
