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

    // Состояние решения диспетчера
    public enum DispatchStatus { Pending, Approved, Denied }
    public DispatchStatus dispatchStatus = DispatchStatus.Pending;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup   = GetComponent<CanvasGroup>();
        GameObject foundScanner = GameObject.Find("SweepLine");
        if (foundScanner != null) sweepLine = foundScanner.transform;
    }

    void Start()
    {
        if (!wasInitialized)
        {
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string randomID = "" + letters[Random.Range(0, letters.Length)]
                                 + letters[Random.Range(0, letters.Length)];
            callsignText.text = randomID + "-" + Random.Range(100, 999);
        }

        UpdateInternalSpeed();

        if (RadarManager.Instance != null)
            RadarManager.Instance.RegisterAirplane(this);
    }

    public void InitializeFromData(FlightData data)
    {
        wasInitialized            = true;
        callsignText.text         = data.callsign;
        logicalPosition           = data.position;
        rectTransform.anchoredPosition = data.position;
        targetPosition            = data.target;
        speed                     = data.speed;
    }

    public Vector2 GetLogicalPosition() => logicalPosition;

    void Update()
    {
        logicalPosition = Vector2.MoveTowards(logicalPosition, targetPosition, _actualSpeed * Time.deltaTime);
        HandlePing();
        FadeOut();

        // Компенсируем масштаб радара (зум)
        if (transform.parent != null)
        {
            float zoom = transform.parent.localScale.x;
            transform.localScale = new Vector3(1f / zoom, 1f / zoom, 1f);
        }

        // Уничтожаем самолёт если улетел за границу или достиг цели
        bool reachedTarget  = Vector2.Distance(logicalPosition, targetPosition) < 0.5f;
        bool outOfBounds    = Vector2.Distance(Vector2.zero, logicalPosition) > despawnRadius;
        if (reachedTarget || outOfBounds)
            Destroy(gameObject);
    }

    public void UpdateInternalSpeed() => _actualSpeed = speed / 10f;

    // ── Разрешить посадку ─────────────────────────────────────────────
    public void Approve()
    {
        if (dispatchStatus != DispatchStatus.Pending) return;
        dispatchStatus = DispatchStatus.Approved;
        if (callsignText != null) callsignText.color = Color.green;
        Debug.Log($"[Dispatcher] {callsignText.text} РАЗРЕШЕНО");
    }

    // ── Запретить посадку — разворот и уход ───────────────────────────
    public void Deny()
    {
        if (dispatchStatus != DispatchStatus.Pending) return;
        dispatchStatus = DispatchStatus.Denied;

        // Направление прочь от центра
        Vector2 awayDir = logicalPosition.normalized;
        if (awayDir == Vector2.zero) awayDir = Vector2.right;

        targetPosition = awayDir * (despawnRadius + 300f);

        if (callsignText != null) callsignText.color = Color.red;
        Debug.Log($"[Dispatcher] {callsignText.text} ЗАПРЕЩЕНО — разворот");
    }

    void HandlePing()
    {
        if (sweepLine == null) return;
        float planeAngle = Mathf.Atan2(logicalPosition.y, logicalPosition.x) * Mathf.Rad2Deg;
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
            callsignText.rectTransform.localPosition =
                Quaternion.Inverse(rectTransform.localRotation) * new Vector3(0, -35f, 0);
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
        // Не перекрашиваем если решение уже принято
        if (dispatchStatus != DispatchStatus.Pending) return;
        callsignText.color    = highlight ? Color.yellow : Color.white;
        rectTransform.localScale = highlight ? Vector3.one * 1.1f : Vector3.one;
    }

    public void SetFlightPath(Vector2 start, Vector2 target)
    {
        rectTransform.anchoredPosition = start;
        logicalPosition = start;
        targetPosition  = target;
    }
}
