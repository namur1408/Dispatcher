using UnityEngine;
using TMPro;

/// <summary>
/// Добавь этот скрипт на TMP-объект Ticker внизу терминала.
/// Меняет текст по таймеру, как строка статуса в AEGIS OS.
/// Сообщения можно задать в инспекторе.
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
