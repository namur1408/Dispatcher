using UnityEngine;
using TMPro;

public class UIAirplane : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1f;
    public float fadeSpeed = 0.5f;
    public float minAlpha = 0.3f;
    private float _actualSpeed;
    public float despawnRadius = 1350f;

    [Header("References")]
    public RectTransform directionLine;
    public TextMeshProUGUI callsignText;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform sweepLine;
    private bool isSelected = false;

    public Vector2 targetPosition = Vector2.zero;
    private Vector2 logicalPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        GameObject foundScanner = GameObject.Find("SweepLine");
        if (foundScanner != null)
        {
            sweepLine = foundScanner.transform;
        }
    }

    void Start()
    {
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string randomID = "" + letters[Random.Range(0, letters.Length)] + letters[Random.Range(0, letters.Length)];
        callsignText.text = randomID + "-" + Random.Range(100, 999);
        UpdateInternalSpeed();

        if (RadarManager.Instance != null)
            RadarManager.Instance.RegisterAirplane(this);
    }

    void Update()
    {
        logicalPosition = Vector2.MoveTowards(
            logicalPosition,
            targetPosition,
            _actualSpeed * Time.deltaTime
        );
        HandlePing();
        FadeOut();
        if (transform.parent != null)
        {
            float currentZoom = transform.parent.localScale.x;
            transform.localScale = new Vector3(1f / currentZoom, 1f / currentZoom, 1f);
        }
        if (Vector2.Distance(Vector2.zero, logicalPosition) > despawnRadius)
        {
            Debug.Log("Plane " + callsignText.text + " left radar zone");
            Destroy(gameObject); 
        }
        if (rectTransform.anchoredPosition == targetPosition)
        {
            Debug.Log("Flight " + callsignText.text + " landed!");
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

    public void UpdateInternalSpeed()
    {
        _actualSpeed = speed / 10f;
    }

    void HandlePing()
    {
        Vector2 planeDir = logicalPosition;
        float planeAngle = Mathf.Atan2(planeDir.y, planeDir.x) * Mathf.Rad2Deg;
        Vector2 sweepDir = sweepLine.up;
        float sweepAngle = Mathf.Atan2(sweepDir.y, sweepDir.x) * Mathf.Rad2Deg;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(sweepAngle, planeAngle));

        if (angleDiff < 3f)
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
            float yOffset = -35f;
            callsignText.rectTransform.localPosition = Quaternion.Inverse(rectTransform.localRotation) * new Vector3(0, yOffset, 0);
        }

        if (directionLine != null)
        {
            SetLineLength(speed * 1f);
        }
    }

    void FadeOut()
    {
        if (canvasGroup == null) return;

        if (canvasGroup.alpha > minAlpha)
        {
            canvasGroup.alpha -= fadeSpeed * Time.deltaTime;
        }
    }

    void OnDestroy()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.UnregisterAirplane(this);
    }

    public void SetHighlight(bool highlight)
    {
        isSelected = highlight;
        if (isSelected)
        {
            callsignText.color = Color.yellow; 
            rectTransform.localScale = Vector3.one * 1.1f; 
        }
        else
        {
            callsignText.color = Color.white;
            rectTransform.localScale = Vector3.one;
        }
    }

    public void SetFlightPath(Vector2 start, Vector2 target)
    {
        rectTransform.anchoredPosition = start;
        logicalPosition = start;
        targetPosition = target;
    }
}