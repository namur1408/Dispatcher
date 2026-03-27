using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UIAirplane : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1f;
    private float _actualSpeed;
    public float despawnRadius = 1350f;
    public float fadeSpeed = 0.5f;
    public float minAlpha = 0.3f;
    public float showTextZoomThreshold = 1.2f;
    public float routeLineWidth = 2f;

    [Header("References")]
    public TextMeshProUGUI callsignText;
    public GameObject routeSegmentPrefab;
    public GameObject waypointMarkerPrefab;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform sweepLine;

    private List<Vector2> waypoints = new List<Vector2>();
    private List<GameObject> lineSegments = new List<GameObject>();
    private List<GameObject> activeMarkers = new List<GameObject>();

    public Vector2 targetPosition => waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : Vector2.zero;

    private Vector2 logicalPosition;
    private bool wasInitialized = false;
    private bool isSelected = false;

    // --- НОВАЯ ПЕРЕМЕННАЯ ДЛЯ ПЕРВОГО ОБНАРУЖЕНИЯ ---
    private bool hasBeenPinged = false;

    public enum DispatchStatus { Pending, Approved, Denied }
    public DispatchStatus dispatchStatus = DispatchStatus.Pending;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Прячем самолет сразу при появлении на сцене
        if (canvasGroup != null) canvasGroup.alpha = 0f;

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
        if (RadarManager.Instance != null) RadarManager.Instance.RegisterAirplane(this);
    }

    public void InitializeFromData(FlightData data)
    {
        wasInitialized = true;
        callsignText.text = data.callsign;
        logicalPosition = data.position;
        rectTransform.anchoredPosition = data.position;
        speed = data.speed;
        dispatchStatus = DispatchStatus.Pending;
        SetFlightPath(data.position, data.target);
    }

    public Vector2 GetLogicalPosition() => logicalPosition;

    public void SetFlightPath(Vector2 start, Vector2 target)
    {
        rectTransform.anchoredPosition = start;
        logicalPosition = start;
        waypoints.Clear();
        waypoints.Add(target);
        UpdateVisualRotation();
        RebuildRouteLayer();
    }

    public void AddWaypoint(Vector2 point)
    {
        if (dispatchStatus != DispatchStatus.Pending) return;
        waypoints.Insert(0, point);
        RebuildRouteLayer();
    }

    public void RemoveWaypoint(int index)
    {
        if (index >= 0 && index < waypoints.Count)
        {
            waypoints.RemoveAt(index);
            RebuildRouteLayer();
            UpdateVisualRotation();
        }
    }

    public int GetWaypointIndexAt(Vector2 clickPos, float thresholdRadius = 30f)
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (Vector2.Distance(clickPos, waypoints[i]) <= thresholdRadius) return i;
        }
        return -1;
    }

    void Update()
    {
        if (waypoints.Count == 0) return;

        Vector2 currentTarget = waypoints[0];
        logicalPosition = Vector2.MoveTowards(logicalPosition, currentTarget, _actualSpeed * Time.deltaTime);

        HandlePing();
        FadeOut();

        if (transform.parent != null)
        {
            float zoom = transform.parent.localScale.x;
            transform.localScale = new Vector3(1f / zoom, 1f / zoom, 1f);
            CheckZoomVisibility(zoom);
        }

        if (lineSegments.Count > 0) UpdateFirstSegment();

        if (Vector2.Distance(logicalPosition, currentTarget) < 1f)
        {
            if (waypoints.Count > 1)
            {
                waypoints.RemoveAt(0);
                RebuildRouteLayer();
                UpdateVisualRotation();
            }
        }

        if (Vector2.Distance(Vector2.zero, logicalPosition) > despawnRadius)
            Destroy(gameObject);

        SyncRouteAlpha();
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

            // Засветились на радаре!
            canvasGroup.alpha = 1f;
            hasBeenPinged = true;
        }
    }

    void UpdateVisualRotation()
    {
        if (waypoints.Count == 0) return;
        Vector2 direction = (waypoints[0] - logicalPosition).normalized;
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rectTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        if (callsignText != null)
        {
            callsignText.transform.rotation = Quaternion.identity;
            callsignText.rectTransform.localPosition =
                Quaternion.Inverse(rectTransform.localRotation) * new Vector3(0, -35f, 0);
        }
    }

    void FadeOut()
    {
        // Если сканер нас еще ни разу не увидел - остаемся полностью невидимыми
        if (!hasBeenPinged)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        canvasGroup.alpha = Mathf.Max(minAlpha, canvasGroup.alpha - fadeSpeed * Time.deltaTime);
    }

    public void UpdateInternalSpeed() => _actualSpeed = speed / 10f;

    private void CheckZoomVisibility(float zoom)
    {
        bool show = zoom >= showTextZoomThreshold;
        if (callsignText.gameObject.activeSelf != show) callsignText.gameObject.SetActive(show);
    }

    private void RebuildRouteLayer()
    {
        foreach (GameObject seg in lineSegments) Destroy(seg);
        lineSegments.Clear();
        foreach (GameObject marker in activeMarkers) Destroy(marker);
        activeMarkers.Clear();

        if (waypoints.Count == 0) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            CreateWaypointMarker(waypoints[i], i);

            if (i < waypoints.Count - 1)
            {
                CreateSegment(waypoints[i], waypoints[i + 1]);
            }
        }

        CreateSegment(logicalPosition, waypoints[0]);

        // Синхронизируем прозрачность сразу при перестроении, чтобы линии не мелькали
        SyncRouteAlpha();
    }

    private void CreateWaypointMarker(Vector2 pos, int index)
    {
        if (waypointMarkerPrefab == null) return;

        GameObject marker = Instantiate(waypointMarkerPrefab, transform.parent, false);
        marker.transform.SetSiblingIndex(transform.GetSiblingIndex());

        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;

        Color c = isSelected ? Color.yellow : Color.white;
        if (dispatchStatus == DispatchStatus.Approved) c = Color.green;
        if (dispatchStatus == DispatchStatus.Denied) c = Color.red;
        marker.GetComponent<UnityEngine.UI.Image>().color = c;

        activeMarkers.Add(marker);
    }

    private void CreateSegment(Vector2 start, Vector2 end)
    {
        GameObject newSeg = Instantiate(routeSegmentPrefab, transform.parent, false);
        newSeg.transform.SetSiblingIndex(transform.GetSiblingIndex());
        lineSegments.Add(newSeg);
        UpdateSegmentLook(newSeg.GetComponent<RectTransform>(), start, end);

        Color c = isSelected ? Color.yellow : Color.white;
        if (dispatchStatus == DispatchStatus.Approved) c = Color.green;
        if (dispatchStatus == DispatchStatus.Denied) c = Color.red;
        newSeg.GetComponent<UnityEngine.UI.Image>().color = c;
    }

    private void UpdateFirstSegment()
    {
        UpdateSegmentLook(lineSegments[lineSegments.Count - 1].GetComponent<RectTransform>(),
                          rectTransform.anchoredPosition, 
                          waypoints[0]);
    }

    private void UpdateSegmentLook(RectTransform segRect, Vector2 start, Vector2 end)
    {
        float dist = Vector2.Distance(start, end);
        segRect.sizeDelta = new Vector2(routeLineWidth, dist);
        segRect.anchoredPosition = start;
        Vector2 dir = (end - start).normalized;
        segRect.rotation = Quaternion.Euler(0, 0, (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg) - 90f);
    }

    public void Approve()
    {
        if (dispatchStatus != DispatchStatus.Pending) return;
        dispatchStatus = DispatchStatus.Approved;
        callsignText.color = Color.green;
        foreach (GameObject seg in lineSegments) seg.GetComponent<UnityEngine.UI.Image>().color = Color.green;
        foreach (GameObject marker in activeMarkers) marker.GetComponent<UnityEngine.UI.Image>().color = Color.green;
    }

    public void Deny()
    {
        if (dispatchStatus != DispatchStatus.Pending) return;
        dispatchStatus = DispatchStatus.Denied;
        waypoints.Clear();
        waypoints.Add(logicalPosition.normalized * (despawnRadius + 300f));
        callsignText.color = Color.red;
        RebuildRouteLayer();
    }

    public void SetHighlight(bool h)
    {
        isSelected = h;
        Color c = h ? Color.yellow : (dispatchStatus == DispatchStatus.Approved ? Color.green : (dispatchStatus == DispatchStatus.Denied ? Color.red : Color.white));
        callsignText.color = c;
        foreach (GameObject seg in lineSegments) seg.GetComponent<UnityEngine.UI.Image>().color = c;
        foreach (GameObject marker in activeMarkers) marker.GetComponent<UnityEngine.UI.Image>().color = c;
    }

    public void TriggerSelection()
    {
        if (BigRadarTerminal.Instance != null) BigRadarTerminal.Instance.SelectPlane(this);
        UIAirplane[] planes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        foreach (var p in planes) p.SetHighlight(p == this);
    }

    private void SyncRouteAlpha()
    {
        if (canvasGroup == null) return;

        float currentAlpha = canvasGroup.alpha;

        foreach (GameObject seg in lineSegments)
        {
            UnityEngine.UI.Image img = seg.GetComponent<UnityEngine.UI.Image>();
            Color c = img.color;
            c.a = currentAlpha;
            img.color = c;
        }

        foreach (GameObject marker in activeMarkers)
        {
            UnityEngine.UI.Image img = marker.GetComponent<UnityEngine.UI.Image>();
            Color c = img.color;
            c.a = currentAlpha;
            img.color = c;
        }
    }
}