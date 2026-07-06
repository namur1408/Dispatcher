using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundTrigger : MonoBehaviour
{
    [Header("Режим тишины")]
    public bool isSilent = false; // <-- NEW: Checkbox to make the button completely silent

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
        // 1. CHECK: If the button is not clickable (gray) - keep quiet!
        if (btn != null && !btn.interactable) return;

        // 2. CHECKING FOR SILENCE: If isSilent is checked, we don’t play anything
        if (isSilent) return;

        if (ButtonSoundManager.instance == null) return;

        // 3. CUSTOM SOUND: If you have your own sound, play it
        if (customSound != null)
        {
            ButtonSoundManager.instance.PlaySpecialSound(customSound, ButtonSoundManager.instance.volume * volumeMultiplier);
        }
        // 4. STANDARD SOUND: Otherwise we play a normal click
        else
        {
            ButtonSoundManager.instance.PlayDefaultClick();
        }
    }
}