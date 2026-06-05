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
    
    // Список дочерних графических элементов
    private List<PaperBendChild> childrenModifiers = new List<PaperBendChild>();
    private List<TMP_Text> tmpTexts = new List<TMP_Text>();

    void Awake()
    {
        paperRect = GetComponent<RectTransform>();
    }

    void Start()
    {
        // Находим все Graphic компоненты (картинки, фоны и обычный Text)
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            // Пропускаем TextMeshPro, так как он обрабатывается отдельно
            if (g is TMP_Text) continue;

            var modifier = g.gameObject.GetComponent<PaperBendChild>();
            if (modifier == null)
            {
                modifier = g.gameObject.AddComponent<PaperBendChild>();
            }
            modifier.paperBend = this;
            childrenModifiers.Add(modifier);
        }

        // Находим все элементы TextMeshPro
        tmpTexts.AddRange(GetComponentsInChildren<TMP_Text>(true));
    }

    void Update()
    {
        // Обновляем геометрию (порог уменьшен для плавности)
        if (Mathf.Abs(bendAmount - lastBendAmount) > 0.01f)
        {
            lastBendAmount = bendAmount;
            
            // 1. Обновляем обычные UI элементы (Image и старый Text)
            foreach (var mod in childrenModifiers)
            {
                if (mod != null)
                {
                    Graphic g = mod.GetComponent<Graphic>();
                    if (g != null) g.SetVerticesDirty();
                }
            }

            // 2. Искривляем TextMeshPro напрямую
            foreach (var tmp in tmpTexts)
            {
                if (tmp != null && tmp.isActiveAndEnabled)
                {
                    tmp.ForceMeshUpdate(); // Генерируем ровный меш
                    TMP_TextInfo textInfo = tmp.textInfo;

                    // Модифицируем вершины каждого символа
                    for (int i = 0; i < textInfo.characterCount; i++)
                    {
                        if (!textInfo.characterInfo[i].isVisible) continue;

                        int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                        int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                        Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                        // У каждого символа 4 вершины
                        for (int vIndex = 0; vIndex < 4; vIndex++)
                        {
                            Vector3 origPos = sourceVertices[vertexIndex + vIndex];
                            
                            // Упаковываем в UIVertex для совместимости с нашей функцией
                            UIVertex tempV = new UIVertex();
                            tempV.position = origPos;
                            
                            ApplyBendToVertex(ref tempV, tmp.rectTransform, false);
                            
                            sourceVertices[vertexIndex + vIndex] = tempV.position;
                        }
                    }

                    // Обновляем меш TMP
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

    // Эта функция вызывается каждым дочерним элементом при генерации меша
    public void ApplyBendToVertex(ref UIVertex v, RectTransform childRect, bool isPaperBackground)
    {
        // 1. Переводим локальную координату вершины child в локальную координату paper
        Vector3 worldPos = childRect.TransformPoint(v.position);
        Vector3 localToPaper = paperRect.InverseTransformPoint(worldPos);

        // 2. Считаем изгиб (математика параболы)
        float width = paperRect.rect.width;
        if (width == 0) width = 100f; // Защита от деления на ноль

        // Нормализованная координата X на бумаге (0 - лево, 1 - право)
        float normalizedX = (localToPaper.x - paperRect.rect.xMin) / width;
        float dist = normalizedX - bendCenter;

        float bend = dist * dist * bendAmount;

        // 3. Если это фон бумаги, добавляем тень (изменяем цвет вершин)
        if (isPaperBackground)
        {
            float shadowStrength = 0.15f;
            // Тень теперь зависит от факта поднятия (foldShadowAlpha), а не от силы прогиба, 
            // чтобы не пропадала резко при переходе через 0.
            float shadowMask = 1.0f - Mathf.Clamp01(Mathf.Abs(dist) * 3f) * shadowStrength * foldShadowAlpha;
            
            Color32 c = v.color;
            c.r = (byte)(c.r * shadowMask);
            c.g = (byte)(c.g * shadowMask);
            c.b = (byte)(c.b * shadowMask);
            v.color = c;
        }

        // 4. Применяем изгиб
        localToPaper.y += bend;
        localToPaper.z -= Mathf.Abs(bend) * 0.02f; // Немного объёма

        // 5. Возвращаем обратно в локальные координаты child
        Vector3 newWorldPos = paperRect.TransformPoint(localToPaper);
        v.position = childRect.InverseTransformPoint(newWorldPos);
    }
}
