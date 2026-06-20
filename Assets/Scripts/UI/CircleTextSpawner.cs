using UnityEngine;
using TMPro;

public class CircleTextSpawner : MonoBehaviour
{
    [Header("Настройки Круга")]
    public GameObject textPrefab;
    public string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public float radius = 150f;  
    public bool faceCenter = true; 
    [ContextMenu("Сгенерировать буквы по кругу")]
    public void SpawnLetters()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (textPrefab == null)
        {
            Debug.LogError("Сначала назначьте Text Prefab в скрипт!");
            return;
        }

        float angleStep = 360f / alphabet.Length;

        for (int i = 0; i < alphabet.Length; i++)
        {
            GameObject go = Instantiate(textPrefab, transform);
            go.name = "Letter_" + alphabet[i];
            
            TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = alphabet[i].ToString();

            RectTransform rect = go.GetComponent<RectTransform>();
            float angleDeg = i * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            rect.anchoredPosition = new Vector2(radius * Mathf.Sin(angleRad), radius * Mathf.Cos(angleRad));

            if (faceCenter)
            {
                rect.localRotation = Quaternion.Euler(0, 0, -angleDeg);
            }
        }
    }
}
