using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LightFlicker : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private Image blackoutImage;
    
    [Tooltip("Higher value means faster fade. 5 = 0.2 seconds to fade.")]
    [SerializeField] private float fadeSpeed = 5f; 

    [Tooltip("Maximum darkness level. 1.0 = pitch black (255), 0.94 = slightly transparent (240).")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.94f;

    private void Awake()
    {
        if (blackoutImage != null)
        {
            // Ensure screen is clear on start and doesn't block clicks
            SetAlpha(0f);
            blackoutImage.raycastTarget = false; 
        }
    }

    // Fades the screen to the target maxAlpha
    public IEnumerator FadeOut()
    {
        if (blackoutImage == null) yield break;

        // Loop continues until the current alpha reaches maxAlpha
        while (blackoutImage.color.a < maxAlpha)
        {
            // Mathf.MoveTowards guarantees we reach exactly maxAlpha smoothly
            float newAlpha = Mathf.MoveTowards(blackoutImage.color.a, maxAlpha, fadeSpeed * Time.deltaTime);
            SetAlpha(newAlpha);
            yield return null; // Wait for the next frame
        }
    }

    // Fades the screen back to fully transparent (Alpha = 0)
    public IEnumerator FadeIn()
    {
        if (blackoutImage == null) yield break;

        while (blackoutImage.color.a > 0f)
        {
            float newAlpha = Mathf.MoveTowards(blackoutImage.color.a, 0f, fadeSpeed * Time.deltaTime);
            SetAlpha(newAlpha);
            yield return null;
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = blackoutImage.color;
        c.a = alpha;
        blackoutImage.color = c;
    }
}