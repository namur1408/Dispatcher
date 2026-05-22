using UnityEngine;
using TMPro;

public class CircleTextSpawner : MonoBehaviour
{
    [Header("Настройки Круга")]
    public GameObject textPrefab; // Префаб одной буквы (Text Mesh Pro)
    public string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public float radius = 150f;   // Радиус диска (отдаление букв от центра)
    public bool faceCenter = true; // Поворачивать ли буквы низом к центру круга?

    // Эта команда появится в меню при нажатии правой кнопкой мыши по компоненту скрипта!
    [ContextMenu("Сгенерировать буквы по кругу")]
    public void SpawnLetters()
    {
        // Удаляем старые буквы, если они были (чтобы не наслаивались)
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
            
            // Считаем угол. Мы хотим, чтобы первая буква (A) была ровно на самом верху (в зените).
            float angleDeg = i * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            
            // Математика: X = Синус, Y = Косинус (это ставит нулевой угол ровно на 12 часов)
            rect.anchoredPosition = new Vector2(radius * Mathf.Sin(angleRad), radius * Mathf.Cos(angleRad));

            if (faceCenter)
            {
                // Поворачиваем саму букву
                rect.localRotation = Quaternion.Euler(0, 0, -angleDeg);
            }
        }
    }
}
