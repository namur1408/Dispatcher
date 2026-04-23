using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LandingAnimation : MonoBehaviour
{
    public static LandingAnimation Instance;

    [Header("References")]
    public GameObject animationCanvas;
    public RectTransform planeRect;
    public CanvasGroup canvasGroup;
    public RectTransform startPoint;
    public RectTransform endPoint;

    [Header("Animation Settings")]
    public float flightDuration = 3.5f;
    public float fadeInTime = 0.3f;
    public float fadeOutTime = 0.5f;
    public float planeScale = 1f;
    public float wobbleStrength = 8f;

    void Awake()
    {
        Instance = this;
        if (animationCanvas != null)
            animationCanvas.SetActive(false);
    }

    public void PlayLanding()
    {
        StartCoroutine(LandingCoroutine());
    }

    private IEnumerator LandingCoroutine()
    {
        animationCanvas.SetActive(true);
        canvasGroup.alpha = 0f;

        planeRect.anchoredPosition = startPoint.anchoredPosition;
        planeRect.localScale = Vector3.one * planeScale;

        Vector2 dir = (endPoint.anchoredPosition - startPoint.anchoredPosition).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        planeRect.rotation = Quaternion.Euler(0, 0, 0);

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeInTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        t = 0f;
        while (t < flightDuration)
        {
            t += Time.deltaTime;
            float progress = t / flightDuration;
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            planeRect.anchoredPosition = Vector2.Lerp(
                startPoint.anchoredPosition,
                endPoint.anchoredPosition,
                eased
            );

            float wobble = Mathf.Sin(progress * Mathf.PI * 4f) * wobbleStrength;
            planeRect.anchoredPosition += new Vector2(0, wobble);

            yield return null;
        }

        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutTime);
            yield return null;
        }

        animationCanvas.SetActive(false);
    }
}