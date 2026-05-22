using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CipherWheel : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform container;      // Пустой RectTransform, куда будут спавниться буквы
    public GameObject textPrefab;        // Префаб обычного TextMeshProUGUI (выравнивание по центру)

    [Header("Cylinder Settings")]
    public float radius = 250f;          // Радиус виртуального цилиндра (регулирует высоту барабана)
    public float spacingMultiplier = 1f; // Расстояние между буквами
    public Color normalColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Цвет боковых букв (серый/коричневый)
    public Color centerColor = new Color(0.7f, 0.1f, 0.1f, 1f); // Цвет центральной буквы (темно-красный)

    [Header("Logic")]
    public string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // Можно поменять на кириллицу
    public int currentIndex = 0;
    public float rotationSpeed = 8f;

    private List<RectTransform> slots = new List<RectTransform>();
    private List<TextMeshProUGUI> letters = new List<TextMeshProUGUI>();
    private List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
    private float currentFloatIndex = 0f;

    void Start()
    {
        for (int i = 0; i < alphabet.Length; i++)
        {
            GameObject go = Instantiate(textPrefab, container);
            
            RectTransform rect = go.GetComponent<RectTransform>();
            slots.Add(rect);

            TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = alphabet[i].ToString();
                txt.alignment = TextAlignmentOptions.Center;
                letters.Add(txt);
            }

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            canvasGroups.Add(cg);
        }
        currentFloatIndex = currentIndex;
    }

    void Update()
    {
        float target = currentIndex;
        float diff = target - currentFloatIndex;

        if (diff > alphabet.Length / 2f) currentFloatIndex += alphabet.Length;
        else if (diff < -alphabet.Length / 2f) currentFloatIndex -= alphabet.Length;

        currentFloatIndex = Mathf.Lerp(currentFloatIndex, target, Time.deltaTime * rotationSpeed);
        
        if (currentFloatIndex >= alphabet.Length) currentFloatIndex -= alphabet.Length;
        if (currentFloatIndex < 0) currentFloatIndex += alphabet.Length;

        float angleStep = (360f / alphabet.Length) * spacingMultiplier;

        for (int i = 0; i < slots.Count; i++)
        {
            float distance = i - currentFloatIndex;
            
            if (distance > alphabet.Length / 2f) distance -= alphabet.Length;
            else if (distance < -alphabet.Length / 2f) distance += alphabet.Length;

            float angle = distance * angleStep;

            if (Mathf.Abs(angle) < 85f)
            {
                if (!slots[i].gameObject.activeSelf) slots[i].gameObject.SetActive(true);
                
                float rad = angle * Mathf.Deg2Rad;
                
                slots[i].anchoredPosition = new Vector2(0, radius * Mathf.Sin(rad));
                
                float cos = Mathf.Cos(rad);
                slots[i].localScale = new Vector3(1f, cos, 1f);

                if (Mathf.Abs(angle) <= angleStep * 0.4f)
                {
                    float centerLerp = 1f - (Mathf.Abs(angle) / (angleStep * 0.4f));
                    if (letters.Count > i && letters[i] != null)
                    {
                        letters[i].color = Color.Lerp(normalColor, centerColor, centerLerp);
                    }
                    canvasGroups[i].alpha = 1f;
                }
                else
                {
                    if (letters.Count > i && letters[i] != null)
                    {
                        letters[i].color = normalColor;
                    }
                    canvasGroups[i].alpha = cos * cos;
                }
            }
            else
            {
                if (slots[i].gameObject.activeSelf) slots[i].gameObject.SetActive(false);
            }
        }
    }
}
