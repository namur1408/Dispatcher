using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

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

    [Header("Holding Pattern Settings")]
    public float holdingRadius = 80f;
    public float maxHoldingTime = 45f;

    [Header("References")]
    public TextMeshProUGUI callsignText;
    public GameObject routeSegmentPrefab;
    public GameObject waypointMarkerPrefab;

    [Header("Collision Hitbox")]
    public Image hitboxVisual;
    private bool isColliding = false;
    private bool isInDanger = false;

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
    private bool hasBeenPinged = false;

    private bool isHolding = false;
    private float holdingTimer = 0f;
    private float currentHoldingAngle = 0f;
    private Vector2 holdingCenter;

    public string cargo; 

    public enum DispatchStatus { Pending, Approved, Denied }
    public DispatchStatus dispatchStatus = DispatchStatus.Pending;

    public List<Vector2> GetWaypoints() => new List<Vector2>(waypoints);

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null) canvasGroup.alpha = 0f;

        GameObject foundScanner = GameObject.Find("SweepLine");
        if (foundScanner != null) sweepLine = foundScanner.transform;
    }

    void Start()
    {
        if (!wasInitialized)
        {
            string[] availablePrefixes = { "QY", "GE", "KO", "LX", "TR" };
            string randomPrefix = availablePrefixes[Random.Range(0, availablePrefixes.Length)];
            callsignText.text = randomPrefix + "-" + Random.Range(100, 999);
            string[] cargoTypes = { "Medicines", "People", "Food", "Scrap" };
            cargo = cargoTypes[Random.Range(0, cargoTypes.Length)];
        }
        UpdateInternalSpeed();
        if (RadarManager.Instance != null) RadarManager.Instance.RegisterAirplane(this);
    }

    public void SetCallsign(string newCallsign)
    {
        wasInitialized = true;
        callsignText.text = newCallsign;
        if (string.IsNullOrEmpty(cargo))
        {
            string[] cargoTypes = { "Medicines", "People", "Food", "Scrap" };
            cargo = cargoTypes[Random.Range(0, cargoTypes.Length)];
        }
    }

    public void InitializeFromData(FlightData data)
    {
        wasInitialized = true;
        callsignText.text = data.callsign;
        logicalPosition = data.position;
        rectTransform.anchoredPosition = data.position;
        speed = data.speed;

        cargo = data.cargo; 

        isHolding = false;
        waypoints = new List<Vector2>(data.savedWaypoints);

        UpdateVisualRotation();
        RebuildRouteLayer();

        if (data.decisionMade)
        {
            dispatchStatus = data.approved ? DispatchStatus.Approved : DispatchStatus.Denied;

            if (!data.approved)
            {
                waypoints.Clear();
                waypoints.Add(logicalPosition.normalized * (despawnRadius + 300f));
                RebuildRouteLayer();
            }
        }
        else
        {
            dispatchStatus = DispatchStatus.Pending;
        }

        UpdateHitboxColor();
    }

    public Vector2 GetLogicalPosition() => logicalPosition;

    public void SetFlightPath(Vector2 start, Vector2 target)
    {
        rectTransform.anchoredPosition = start;
        logicalPosition = start;
        isHolding = false;
        waypoints.Clear();
        waypoints.Add(target);
        UpdateVisualRotation();
        RebuildRouteLayer();
    }

    public void AddWaypoint(Vector2 clickPos)
    {
        if (dispatchStatus != DispatchStatus.Pending) return;

        if (waypoints.Count == 0)
        {
            waypoints.Add(clickPos);
            RebuildRouteLayer();
            UpdateVisualRotation();
            return;
        }

        int bestIndex = 0;
        float minDistance = float.MaxValue;

        float distToFirstSeg = DistanceToSegment(clickPos, logicalPosition, waypoints[0]);
        if (distToFirstSeg < minDistance)
        {
            minDistance = distToFirstSeg;
            bestIndex = 0;
        }

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            float dist = DistanceToSegment(clickPos, waypoints[i], waypoints[i + 1]);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestIndex = i + 1;
            }
        }

        float distToLastPoint = Vector2.Distance(clickPos, waypoints[waypoints.Count - 1]);
        if (distToLastPoint < minDistance)
        {
            bestIndex = waypoints.Count;
        }

        waypoints.Insert(bestIndex, clickPos);

        RebuildRouteLayer();
        UpdateVisualRotation();
    }

    private float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;
        if (ab.sqrMagnitude == 0f) return ap.magnitude;

        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
        Vector2 projection = a + t * ab;

        return Vector2.Distance(p, projection);
    }

    public void RemoveWaypoint(int index)
    {
        if (index >= 0 && index < waypoints.Count - 1)
        {
            waypoints.RemoveAt(index);
            RebuildRouteLayer();
            UpdateVisualRotation();
        }
        else
        {
            Debug.Log("[UIAirplane] Попытка удалить финальную точку маршрута заблокирована.");
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
        if (isHolding)
        {
            holdingTimer -= Time.deltaTime;

            if (holdingTimer <= 0)
            {
                Debug.Log($"[UIAirplane] {callsignText.text}: Время ожидания вышло. Самолет уходит.");
                Deny();
            }
            else
            {
                float angularSpeed = (_actualSpeed / holdingRadius) * Mathf.Rad2Deg;
                currentHoldingAngle += angularSpeed * Time.deltaTime;

                Vector2 circleTarget = holdingCenter + new Vector2(Mathf.Cos(currentHoldingAngle * Mathf.Deg2Rad), Mathf.Sin(currentHoldingAngle * Mathf.Deg2Rad)) * holdingRadius;

                logicalPosition = Vector2.MoveTowards(logicalPosition, circleTarget, _actualSpeed * Time.deltaTime);
            }
        }
        else if (waypoints.Count > 0)
        {
            Vector2 currentTarget = waypoints[0];

            if (waypoints.Count == 1 && dispatchStatus == DispatchStatus.Pending)
            {
                if (Vector2.Distance(logicalPosition, currentTarget) <= holdingRadius)
                {
                    StartHolding(currentTarget);
                    return;
                }
            }

            logicalPosition = Vector2.MoveTowards(logicalPosition, currentTarget, _actualSpeed * Time.deltaTime);

            if (Vector2.Distance(logicalPosition, currentTarget) < 5f)
            {
                if (waypoints.Count > 1)
                {
                    waypoints.RemoveAt(0);
                    RebuildRouteLayer();
                }
                else
                {
                    if (dispatchStatus == DispatchStatus.Approved && Vector2.Distance(logicalPosition, Vector2.zero) < 10f)
                    {
                        Debug.Log($"{callsignText.text} успешно сел.");
                        Destroy(gameObject);
                    }
                    else if (dispatchStatus == DispatchStatus.Denied || dispatchStatus == DispatchStatus.Approved)
                    {
                        Debug.Log($"{callsignText.text} покинул зону.");
                        Destroy(gameObject);
                    }
                }
            }
        }
        HandlePing();
        FadeOut();

        if (transform.parent != null)
        {
            float zoom = transform.parent.localScale.x;
            CheckZoomVisibility(zoom);
        }

        if (lineSegments.Count > 0 && !isHolding) UpdateFirstSegment();

        if (Vector2.Distance(Vector2.zero, logicalPosition) > despawnRadius)
            Destroy(gameObject);

        SyncRouteAlpha();
    }

    private void StartHolding(Vector2 center)
    {
        isHolding = true;
        holdingCenter = center;
        holdingTimer = maxHoldingTime;

        Vector2 dirFromCenter = (logicalPosition - center).normalized;
        currentHoldingAngle = Mathf.Atan2(dirFromCenter.y, dirFromCenter.x) * Mathf.Rad2Deg;

        waypoints.Clear();
        RebuildRouteLayer();

        Debug.Log($"[UIAirplane] {callsignText.text} вошел в зону ожидания.");
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
            hasBeenPinged = true;
        }
    }

    void UpdateVisualRotation()
    {
        Vector2 direction = Vector2.zero;

        if (isHolding)
        {
            float nextAngle = currentHoldingAngle + 10f;
            Vector2 nextCircleTarget = holdingCenter + new Vector2(Mathf.Cos(nextAngle * Mathf.Deg2Rad), Mathf.Sin(nextAngle * Mathf.Deg2Rad)) * holdingRadius;
            direction = (nextCircleTarget - logicalPosition).normalized;
        }
        else if (waypoints.Count > 0)
        {
            direction = (waypoints[0] - logicalPosition).normalized;
        }
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
        SyncRouteAlpha();
    }

    private void CreateWaypointMarker(Vector2 pos, int index)
    {
        if (waypointMarkerPrefab == null) return;

        GameObject marker = Instantiate(waypointMarkerPrefab, transform.parent, false);
        marker.transform.SetSiblingIndex(transform.GetSiblingIndex());

        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;

        activeMarkers.Add(marker);
        UpdateHitboxColor();
    }

    private void CreateSegment(Vector2 start, Vector2 end)
    {
        GameObject newSeg = Instantiate(routeSegmentPrefab, transform.parent, false);
        newSeg.transform.SetSiblingIndex(transform.GetSiblingIndex());
        lineSegments.Add(newSeg);
        UpdateSegmentLook(newSeg.GetComponent<RectTransform>(), start, end);
        UpdateHitboxColor();
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

        if (isHolding)
        {
            isHolding = false;
            waypoints.Clear();
            waypoints.Add(Vector2.zero);
            RebuildRouteLayer();
        }

        UpdateHitboxColor();
    }

    public void Deny()
    {
        if (dispatchStatus != DispatchStatus.Pending) return;
        dispatchStatus = DispatchStatus.Denied;

        isHolding = false;

        waypoints.Clear();
        waypoints.Add(logicalPosition.normalized * (despawnRadius + 300f));
        RebuildRouteLayer();
        UpdateHitboxColor();
    }

    public void SetHighlight(bool h)
    {
        isSelected = h;
        UpdateHitboxColor();
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
            if (seg == null) continue;
            Image img = seg.GetComponent<Image>();
            Color c = img.color;
            c.a = currentAlpha;
            img.color = c;
        }

        foreach (GameObject marker in activeMarkers)
        {
            if (marker == null) continue;
            Image img = marker.GetComponent<Image>();
            Color c = img.color;
            c.a = currentAlpha;
            img.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        UIAirplane otherPlane = other.GetComponentInParent<UIAirplane>();
        if (otherPlane != null && otherPlane != this)
        {
            if (!isColliding)
            {
                isColliding = true;
                TriggerCollision();
            }
        }
    }

    private void TriggerCollision()
    {
        Debug.Log($"<color=red>АВАРИЯ: {callsignText.text} столкнулся!</color>");
        if (RadarTutorialManager.Instance != null && !RadarTutorialManager.isRadarTutorialCompleted)
        {
            RadarTutorialManager.Instance.NotifyEmergencyCollision();
        }
        UpdateHitboxColor();
        Invoke("DestroyPlane", 0.05f);
    }

    private void DestroyPlane()
    {
        if (RadarScreenClicker.selectedPlane == this)
        {
            if (BigRadarTerminal.Instance != null) BigRadarTerminal.Instance.ClearSelection();
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.UnregisterAirplane(this);

        foreach (GameObject seg in lineSegments) if (seg != null) Destroy(seg);
        foreach (GameObject marker in activeMarkers) if (marker != null) Destroy(marker);
    }

    public void SetWarning(bool warn)
    {
        if (isColliding) return;
        isInDanger = warn;
        UpdateHitboxColor();
    }

    private void UpdateHitboxColor()
    {
        if (hitboxVisual == null) return;

        Color finalColor = Color.white;

        if (isColliding)
        {
            finalColor = Color.red;
        }
        else if (isSelected)
        {
            finalColor = Color.yellow;
        }
        else if (isInDanger)
        {
            finalColor = new Color(1f, 0.5f, 0f);
        }
        else
        {
            if (dispatchStatus == DispatchStatus.Approved) finalColor = Color.green;
            else if (dispatchStatus == DispatchStatus.Denied) finalColor = Color.red;
            else finalColor = Color.white;
        }

        hitboxVisual.color = finalColor;
        callsignText.color = finalColor;

        if (lineSegments != null)
        {
            foreach (GameObject seg in lineSegments)
                if (seg != null) seg.GetComponent<Image>().color = finalColor;
        }

        if (activeMarkers != null)
        {
            foreach (GameObject marker in activeMarkers)
                if (marker != null) marker.GetComponent<Image>().color = finalColor;
        }
    }
}