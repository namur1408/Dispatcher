using UnityEngine;
using TMPro;

public class UIAirplane : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1f;
    private float _actualSpeed;
    public float despawnRadius = 1350f;
    public float fadeSpeed = 0.5f;
    public float minAlpha = 0.3f;

    [Header("References")]
    public RectTransform directionLine;
    public TextMeshProUGUI callsignText;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform sweepLine;

    public Vector2 targetPosition = Vector2.zero;
    private Vector2 logicalPosition;
    private bool wasInitialized = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        GameObject foundScanner = GameObject.Find("SweepLine");
        if (foundScanner != null) sweepLine = foundScanner.transform;
    }

    void Start()
    {
        if (!wasInitialized)
        {
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string randomID = "" + letters[Random.Range(0, letters.Length)] + letters[Random.Range(0, letters.Length)];
            callsignText.text = randomID + "-" + Random.Range(100, 999);
        }

        UpdateInternalSpeed();
        if (RadarManager.Instance != null)
            RadarManager.Instance.RegisterAirplane(this);
    }

    public void InitializeFromData(FlightData data)
    {
        wasInitialized = true;
        callsignText.text = data.callsign;
        logicalPosition = data.position;
        rectTransform.anchoredPosition = data.position;
        targetPosition = data.target;
        speed = data.speed;
    }

    public Vector2 GetLogicalPosition() => logicalPosition;

    void Update()
    {
        logicalPosition = Vector2.MoveTowards(logicalPosition, targetPosition, _actualSpeed * Time.deltaTime);
        HandlePing();
        FadeOut();

        if (transform.parent != null)
        {
            float currentZoom = transform.parent.localScale.x;
            transform.localScale = new Vector3(1f / currentZoom, 1f / currentZoom, 1f);
        }

        if (Vector2.Distance(Vector2.zero, logicalPosition) > despawnRadius || rectTransform.anchoredPosition == targetPosition)
        {
            Destroy(gameObject);
        }
    }

    public void UpdateInternalSpeed() => _actualSpeed = speed / 10f;

    void HandlePing()
    {
        if (sweepLine == null) return;
        Vector2 planeDir = logicalPosition;
        float planeAngle = Mathf.Atan2(planeDir.y, planeDir.x) * Mathf.Rad2Deg;
        Vector2 sweepDir = sweepLine.up;
        float sweepAngle = Mathf.Atan2(sweepDir.y, sweepDir.x) * Mathf.Rad2Deg;
        if (Mathf.Abs(Mathf.DeltaAngle(sweepAngle, planeAngle)) < 3f)
        {
            rectTransform.anchoredPosition = logicalPosition;
            UpdateVisualRotation();
            canvasGroup.alpha = 1f;
        }
    }

    void UpdateVisualRotation()
    {
        Vector2 direction = (targetPosition - logicalPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        if (callsignText != null)
        {
            callsignText.transform.rotation = Quaternion.identity;
            callsignText.rectTransform.localPosition = Quaternion.Inverse(rectTransform.localRotation) * new Vector3(0, -35f, 0);
        }
    }

    void FadeOut()
    {
        if (canvasGroup != null && canvasGroup.alpha > minAlpha)
            canvasGroup.alpha -= fadeSpeed * Time.deltaTime;
    }

    void OnDestroy()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.UnregisterAirplane(this);
    }

    public void SetHighlight(bool highlight)
    {
        callsignText.color = highlight ? Color.yellow : Color.white;
        rectTransform.localScale = highlight ? Vector3.one * 1.1f : Vector3.one;
    }

    public void SetFlightPath(Vector2 start, Vector2 target)
    {
        rectTransform.anchoredPosition = start;
        logicalPosition = start;
        targetPosition = target;
    }
}