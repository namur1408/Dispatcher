using UnityEngine;
using TMPro;

/// <summary>
/// Добавь на TMP-объект рядом со StatusDot.
/// Показывает текущее время в формате ЧЧ:ММ:СС как в AEGIS OS.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TerminalClock : MonoBehaviour
{
    [Header("Формат")]
    public string format = "HH:mm:ss"; // стандартный C# формат времени
    // Примеры: "HH:mm" → 22:09  |  "HH:mm:ss" → 22:09:23

    private TMP_Text tmp;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    void Update()
    {
        tmp.text = System.DateTime.Now.ToString(format);
    }
}
