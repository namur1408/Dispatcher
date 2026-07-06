using UnityEngine;
using TMPro;

/// <summary>
/// Add to the TMP object next to StatusDot.
/// Shows the current time in HH:MM:SS format as in AEGIS OS.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TerminalClock : MonoBehaviour
{
    [Header("Формат")]
    public string format = "HH:mm:ss"; // standard C# time format
    // Examples: "HH:mm" → 22:09 |  "HH:mm:ss" → 22:09:23

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
