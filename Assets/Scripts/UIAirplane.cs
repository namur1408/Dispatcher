using UnityEngine;

public class UIAirplane : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 20f;
    public float fadeSpeed = 0.5f;

    [Header("References")]
    public RectTransform directionLine;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform sweepLine;

    private Vector2 targetPosition = Vector2.zero;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        GameObject foundScanner = GameObject.Find("SweepLine");
        if (foundScanner != null)
        {
            sweepLine = foundScanner.transform;
        }
    }

    void Update()
    {
        HandlePing();
        FadeOut();
        Vector2 direction = (targetPosition - rectTransform.anchoredPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        float calculatedLength = speed / 2f;
        SetLineLength(calculatedLength);
        rectTransform.anchoredPosition = Vector2.MoveTowards(
            rectTransform.anchoredPosition,
            targetPosition,
            speed * Time.deltaTime
        );
        if (rectTransform.anchoredPosition == targetPosition)
        {
            Debug.Log("Flight landed!");
            Destroy(gameObject);
        }
    }

    public void SetLineLength(float length)
    {
        if (directionLine != null)
        {
            directionLine.sizeDelta = new Vector2(directionLine.sizeDelta.x, length);
        }
    }

    void HandlePing()
    {
        if (sweepLine == null || canvasGroup == null) return;
        float planeAngle = Mathf.Atan2(rectTransform.anchoredPosition.y, rectTransform.anchoredPosition.x) * Mathf.Rad2Deg;
        if (planeAngle < 0) planeAngle += 360f;
        float sweepAngle = (-sweepLine.eulerAngles.z) % 360f;
        if (sweepAngle < 0) sweepAngle += 360f;
        if (Mathf.Abs(sweepAngle - planeAngle) < 0.5f)
        {
            canvasGroup.alpha = 1f;
        }
    }

    void FadeOut()
    {
        if (canvasGroup == null) return;

        if (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= fadeSpeed * Time.deltaTime;
        }
    }
}