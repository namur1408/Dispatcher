using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundTrigger : MonoBehaviour
{
    [Header("Режим тишины")]
    public bool isSilent = false; // <-- НОВОЕ: Галочка, чтобы сделать кнопку абсолютно беззвучной

    [Header("Кастомный звук (Оставь пустым для стандартного)")]
    public AudioClip customSound;

    [Range(0f, 1f)]
    public float volumeMultiplier = 1f;

    private Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlaySound);
        }
    }

    void PlaySound()
    {
        // 1. ПРОВЕРКА: Если кнопка некликабельна (серая) - молчим!
        if (btn != null && !btn.interactable) return;

        // 2. ПРОВЕРКА НА ТИШИНУ: Если стоит галочка isSilent, ничего не играем
        if (isSilent) return;

        if (ButtonSoundManager.instance == null) return;

        // 3. КАСТОМНЫЙ ЗВУК: Если задан свой звук, играем его
        if (customSound != null)
        {
            ButtonSoundManager.instance.PlaySpecialSound(customSound, ButtonSoundManager.instance.volume * volumeMultiplier);
        }
        // 4. СТАНДАРТНЫЙ ЗВУК: Иначе играем обычный клик
        else
        {
            ButtonSoundManager.instance.PlayDefaultClick();
        }
    }
}