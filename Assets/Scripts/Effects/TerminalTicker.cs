using UnityEngine;
using TMPro;

/// <summary>
/// Add this script to the terminal below the TMP-object Ticker.
/// Changes text on a timer, like the status bar in AEGIS OS.
/// Messages can be set in the inspector.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TerminalTicker : MonoBehaviour
{
    [Header("Сообщения (редактируй в инспекторе)")]
    public string[] messages = new string[]
    {
        "MONITORING ALL SECTORS...",
        "RADAR NOMINAL...",
        "WEATHER: VISIBILITY MODERATE...",
        "WIND: 12 KTS...",
        "ALL SYSTEMS OPERATIONAL...",
        "BASTION-7 CORRIDOR REQUEST PENDING...",
        "SIGNAL LOCK CONFIRMED...",
    };

    [Header("Тайминг")]
    public float intervalSeconds = 3.5f;

    private TMP_Text tmp;
    private int      currentIndex = 0;
    private float    timer = 0f;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    void Start()
    {
        if (messages.Length > 0)
            tmp.text = messages[0];
    }

    void Update()
    {
        if (messages.Length <= 1) return;

        timer += Time.deltaTime;
        if (timer >= intervalSeconds)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % messages.Length;
            tmp.text = messages[currentIndex];
        }
    }
}
