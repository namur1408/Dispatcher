using UnityEngine;
using TMPro;

/// <summary>
/// Добавь на TMP-объект — даёт эффект мерцания как у заголовка AEGIS OS.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class FlickerText : MonoBehaviour
{
    [Header("Flicker settings")]
    [Range(0f, 1f)] public float minAlpha    = 0.55f;
    public float flickerChance   = 0.04f;
    public float flickerDuration = 0.08f;

    private TMP_Text tmp;
    private float    flickerTimer = 0f;
    private bool     isFlickering = false;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (isFlickering)
        {
            flickerTimer -= Time.deltaTime;
            if (flickerTimer <= 0f)
            {
                isFlickering = false;
                SetAlpha(1f);
            }
        }
        else if (Random.value < flickerChance * Time.deltaTime * 60f)
        {
            isFlickering = true;
            flickerTimer = flickerDuration;
            SetAlpha(minAlpha);
        }
    }

    void SetAlpha(float a)
    {
        Color c = tmp.color;
        c.a = a;
        tmp.color = c;
    }
}
