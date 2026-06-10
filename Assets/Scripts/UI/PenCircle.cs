using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class PenCircle : Graphic
{
    [Header("Настройки реализма")]
    public float thickness = 5f; 
    public float padding = 20f; // Увеличен отступ от слова
    
    [Header("Форма и искажения")]
    public float roundness = 4.5f; // 2 = эллипс, 4-6 = скругленный прямоугольник (squircle)
    public float roughness = 2f; 
    public float shapeDistortion = 7f; 
    public float spiralAmount = 6f; 
    public int segmentsPerLoop = 60; 

    private float seedOffset;
    private float startAngleOffset;
    private float totalArcLength;
    private float slantAngle; 
    
    private RectTransform paperRect;
    private bool initialized = false;

    private void InitializeRandomness()
    {
        if (initialized) return;
        seedOffset = Random.value * 1000f;
        totalArcLength = Random.Range(340f, 440f); 
        startAngleOffset = Random.Range(30f, 120f); 
        slantAngle = Random.Range(-10f, 10f) * Mathf.Deg2Rad;
        
        FindPaperRect();
        
        initialized = true;
    }

    private void FindPaperRect()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name.ToLower().Contains("doc") || 
                current.name.ToLower().Contains("paper") || 
                current.name.ToLower().Contains("report") ||
                current.name.ToLower().Contains("folder") ||
                current.GetComponent<UnityEngine.EventSystems.EventTrigger>() != null)
            {
                paperRect = current as RectTransform;
                return;
            }
            
            Image img = current.GetComponent<Image>();
            if (img != null && img.rectTransform.rect.width > 150f && img.rectTransform.rect.height > 150f)
            {
                paperRect = current as RectTransform;
            }

            current = current.parent;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        InitializeRandomness();
        vh.Clear();

        float wordW = rectTransform.rect.width / 2f;
        float wordH = rectTransform.rect.height / 2f;

        Vector2 localOffset = CalculatePaperOffset();
        Vector2 center = rectTransform.rect.center + localOffset;

        int totalSegments = Mathf.CeilToInt(segmentsPerLoop * (totalArcLength / 360f)); 
        float angleStep = totalArcLength / totalSegments;

        for (int i = 0; i < totalSegments; i++)
        {
            float progress1 = (float)i / totalSegments;
            float progress2 = (float)(i + 1) / totalSegments;

            float angle1 = (startAngleOffset + i * angleStep) * Mathf.Deg2Rad;
            float angle2 = (startAngleOffset + (i + 1) * angleStep) * Mathf.Deg2Rad;

            float r1 = GetDistortedRadius(wordW, wordH, angle1, progress1, seedOffset);
            float r2 = GetDistortedRadius(wordW, wordH, angle2, progress2, seedOffset);

            float thick1 = GetThickness(progress1, angle1);
            float thick2 = GetThickness(progress2, angle2);
            
            float alpha1 = GetAlpha(progress1);
            float alpha2 = GetAlpha(progress2);

            Color col1 = new Color(color.r, color.g, color.b, color.a * alpha1);
            Color col2 = new Color(color.r, color.g, color.b, color.a * alpha2);

            Vector2 p1Center = GetSlantedPoint(center, r1, angle1, slantAngle);
            Vector2 p2Center = GetSlantedPoint(center, r2, angle2, slantAngle);

            Vector2 dir1 = (p2Center - p1Center).normalized;
            Vector2 norm1 = new Vector2(-dir1.y, dir1.x); 
            
            Vector2 p1Outer = p1Center + norm1 * (thick1 / 2f);
            Vector2 p1Inner = p1Center - norm1 * (thick1 / 2f);

            Vector2 norm2 = norm1; 
            if (i < totalSegments - 1) {
                float angle3 = (startAngleOffset + (i + 2) * angleStep) * Mathf.Deg2Rad;
                float r3 = GetDistortedRadius(wordW, wordH, angle3, progress2 + 1f/totalSegments, seedOffset);
                Vector2 p3Center = GetSlantedPoint(center, r3, angle3, slantAngle);
                Vector2 dir2 = (p3Center - p2Center).normalized;
                norm2 = new Vector2(-dir2.y, dir2.x);
            }

            Vector2 p2Outer = p2Center + norm2 * (thick2 / 2f);
            Vector2 p2Inner = p2Center - norm2 * (thick2 / 2f);

            AddQuad(vh, p1Inner, p1Outer, p2Outer, p2Inner, col1, col2);
        }
    }

    private float GetDistortedRadius(float wordW, float wordH, float angle, float progress, float seed)
    {
        float baseW = wordW + padding;
        float baseH = wordH + padding;

        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        
        // Суперэллипс (чтобы длинные слова обводились "скругленным прямоугольником", а не огромным овалом)
        float absCos = Mathf.Pow(Mathf.Abs(cos) / baseW, roundness);
        float absSin = Mathf.Pow(Mathf.Abs(sin) / baseH, roundness);
        float rBase = 1f / Mathf.Pow(absCos + absSin, 1f / roundness);

        float macroDistortion = Mathf.Sin(angle + seed) * shapeDistortion;
        float spiral = Mathf.Lerp(spiralAmount, -spiralAmount, progress);
        float microNoise = (Mathf.PerlinNoise(seed + progress * 7f, 0) - 0.5f) * roughness;

        float finalRadius = rBase + macroDistortion + spiral + microNoise;

        // ИДЕАЛЬНАЯ ЗАЩИТА: Строгий математический радиус самого слова (углы включены)
        float rWord = 1f / Mathf.Max(Mathf.Abs(cos) / Mathf.Max(0.1f, wordW), Mathf.Abs(sin) / Mathf.Max(0.1f, wordH));
        
        // Линия обводки обязана быть хотя бы на 10px дальше самого дальнего угла слова (дистанция от края слова)
        float minAllowed = rWord + (thickness / 2f) + 10f;
        if (finalRadius < minAllowed)
        {
            finalRadius = minAllowed;
        }

        return finalRadius;
    }

    private float GetThickness(float progress, float angle)
    {
        float t = thickness;
        t *= 1f + Mathf.Sin(angle) * 0.25f;
        t *= 1f + (Mathf.PerlinNoise(seedOffset * 2f, progress * 20f) - 0.5f) * 0.35f;

        float taperLen = 0.08f; 
        if (progress < taperLen) t *= Mathf.Lerp(0.05f, 1f, progress / taperLen);
        else if (progress > 1f - taperLen) t *= Mathf.Lerp(1f, 0.05f, (progress - (1f - taperLen)) / taperLen);

        return Mathf.Max(0.5f, t);
    }

    private float GetAlpha(float progress)
    {
        float a = 1f;
        float fadeLen = 0.05f; 
        if (progress < fadeLen) a = Mathf.Lerp(0.1f, 1f, progress / fadeLen);
        else if (progress > 1f - fadeLen) a = Mathf.Lerp(1f, 0f, (progress - (1f - fadeLen)) / fadeLen);
        return a;
    }

    private Vector2 GetSlantedPoint(Vector2 center, float r, float angle, float slant)
    {
        float x = Mathf.Cos(angle) * r;
        float y = Mathf.Sin(angle) * r;

        float slantedX = x * Mathf.Cos(slant) - y * Mathf.Sin(slant);
        float slantedY = x * Mathf.Sin(slant) + y * Mathf.Cos(slant);

        return new Vector2(center.x + slantedX, center.y + slantedY);
    }

    private Vector2 CalculatePaperOffset()
    {
        if (paperRect == null) return Vector2.zero;
        
        // Максимальное расширение круга за пределы слова
        float maxRadiusExtension = padding + shapeDistortion + spiralAmount + roughness + thickness;
        
        Rect localRect = rectTransform.rect;
        localRect.xMin -= maxRadiusExtension;
        localRect.xMax += maxRadiusExtension;
        localRect.yMin -= maxRadiusExtension;
        localRect.yMax += maxRadiusExtension;

        Vector3[] corners = new Vector3[4];
        corners[0] = rectTransform.TransformPoint(new Vector2(localRect.xMin, localRect.yMin));
        corners[1] = rectTransform.TransformPoint(new Vector2(localRect.xMin, localRect.yMax));
        corners[2] = rectTransform.TransformPoint(new Vector2(localRect.xMax, localRect.yMin));
        corners[3] = rectTransform.TransformPoint(new Vector2(localRect.xMax, localRect.yMax));

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector3 paperPos = paperRect.InverseTransformPoint(corners[i]);
            minX = Mathf.Min(minX, paperPos.x);
            maxX = Mathf.Max(maxX, paperPos.x);
            minY = Mathf.Min(minY, paperPos.y);
            maxY = Mathf.Max(maxY, paperPos.y);
        }

        Rect pRect = paperRect.rect;
        float margin = 12f;
        float pMinX = pRect.xMin + margin;
        float pMaxX = pRect.xMax - margin;
        float pMinY = pRect.yMin + margin;
        float pMaxY = pRect.yMax - margin;

        Vector2 paperOffset = Vector2.zero;

        if (minX < pMinX) paperOffset.x = pMinX - minX;
        else if (maxX > pMaxX) paperOffset.x = pMaxX - maxX;

        if (minY < pMinY) paperOffset.y = pMinY - minY;
        else if (maxY > pMaxY) paperOffset.y = pMaxY - maxY;

        if (paperOffset == Vector2.zero) return Vector2.zero;

        Vector3 originWorld = paperRect.TransformPoint(Vector3.zero);
        Vector3 offsetWorld = paperRect.TransformPoint(paperOffset);
        Vector3 localOffset = rectTransform.InverseTransformPoint(offsetWorld) - rectTransform.InverseTransformPoint(originWorld);

        // Ограничиваем сдвиг, чтобы круг не "слез" со слова
        float maxShiftX = Mathf.Max(0, padding - 5f);
        float maxShiftY = Mathf.Max(0, padding - 5f);
        localOffset.x = Mathf.Clamp(localOffset.x, -maxShiftX, maxShiftX);
        localOffset.y = Mathf.Clamp(localOffset.y, -maxShiftY, maxShiftY);

        return new Vector2(localOffset.x, localOffset.y);
    }

    private void AddQuad(VertexHelper vh, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Color c1, Color c2)
    {
        int startIndex = vh.currentVertCount;

        UIVertex v1 = UIVertex.simpleVert; v1.color = c1; v1.position = p1; vh.AddVert(v1);
        UIVertex v2 = UIVertex.simpleVert; v2.color = c1; v2.position = p2; vh.AddVert(v2);
        UIVertex v3 = UIVertex.simpleVert; v3.color = c2; v3.position = p3; vh.AddVert(v3);
        UIVertex v4 = UIVertex.simpleVert; v4.color = c2; v4.position = p4; vh.AddVert(v4);

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }
}
