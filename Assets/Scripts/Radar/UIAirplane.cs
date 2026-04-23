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
    public bool inStorm = false;
    private string realCallsign;

    [Header("Fuel Mechanics")]
    public float currentFuel = 100f;
    public float distancePerFuelUnit = 15f;
    public float emergencyTimer = 20f;
    private float fuelAtLastPing;
    private bool isOutOfFuel = false;
    private Vector2 lastPosition;

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

        realCallsign = callsignText.text;
        lastPosition = logicalPosition;
        fuelAtLastPing = currentFuel;

        UpdateInternalSpeed();
        if (RadarManager.Instance != null) RadarManager.Instance.RegisterAirplane(this);
    }

    public void SetCallsign(string newCallsign)
    {
        wasInitialized = true;
        callsignText.text = newCallsign;
        realCallsign = newCallsign;

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
        realCallsign = data.callsign;

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
        if (inStorm || isOutOfFuel) return;
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
        if (isOutOfFuel) return; 

        if (index >= 0 && index < waypoints.Count - 1)
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
        float distanceMoved = Vector2.Distance(logicalPosition, lastPosition);
        lastPosition = logicalPosition;

        if (!isOutOfFuel && dispatchStatus != DispatchStatus.Approved && distanceMoved > 0)
        {
            currentFuel -= distanceMoved / distancePerFuelUnit;

            if (currentFuel <= 0)
            {
                currentFuel = 0;
                isOutOfFuel = true;
                _actualSpeed *= 0.3f;
                UpdateHitboxColor();
            }
        }

        if (isOutOfFuel)
        {
            emergencyTimer -= Time.deltaTime;

            if (Mathf.FloorToInt(Time.time * 3) % 2 == 0) callsignText.text = "MAYDAY";
            else callsignText.text = "";

            if (emergencyTimer <= 0)
            {
                Debug.Log($"<color=red>АВАРИЯ: {realCallsign} рухнул из-за нехватки топлива!</color>");
                DestroyPlane();
                return;
            }
        }

        if (DynamicStorm.Instance != null)
        {
            bool currentlyInStorm = DynamicStorm.Instance.IsInStorm(logicalPosition);

            if (currentlyInStorm && !inStorm)
            {
                inStorm = true;
                if (!isOutOfFuel) callsignText.text = "NO SIGNAL"; 
                if (isSelected)
                {
                    SetHighlight(false);
                    if (BigRadarTerminal.Instance != null) BigRadarTerminal.Instance.ClearSelection();
                }
                UpdateHitboxColor();
            }
            else if (!currentlyInStorm && inStorm)
            {
                inStorm = false;
                if (!isOutOfFuel) callsignText.text = realCallsign;
                UpdateHitboxColor();
            }
        }

        float currentSpeed = inStorm ? (_actualSpeed * 0.5f) : _actualSpeed;

        if (isHolding)
        {
            holdingTimer -= Time.deltaTime;

            if (holdingTimer <= 0)
            {
                Deny();
            }
            else
            {
                float angularSpeed = (currentSpeed / holdingRadius) * Mathf.Rad2Deg;
                currentHoldingAngle += angularSpeed * Time.deltaTime;
                Vector2 circleTarget = holdingCenter + new Vector2(Mathf.Cos(currentHoldingAngle * Mathf.Deg2Rad), Mathf.Sin(currentHoldingAngle * Mathf.Deg2Rad)) * holdingRadius;
                logicalPosition = Vector2.MoveTowards(logicalPosition, circleTarget, currentSpeed * Time.deltaTime);
            }
        }
        else if (waypoints.Count > 0)
        {
            Vector2 currentTarget = waypoints[0];

            if (waypoints.Count == 1 && dispatchStatus == DispatchStatus.Pending)
            {
                if (Vector2.Distance(logicalPosition, currentTarget) <= holdingRadius)
                {
                    if (!isOutOfFuel) StartHolding(currentTarget);
                    return;
                }
            }

            logicalPosition = Vector2.MoveTowards(logicalPosition, currentTarget, currentSpeed * Time.deltaTime);

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
                         if (FlightDataManager.Instance != null) FlightDataManager.Instance.MarkFlightAsLanded(realCallsign);

                         if (LandingAnimation.Instance != null)
                         LandingAnimation.Instance.PlayLanding();

                        Destroy(gameObject);
                    }
                    else if (dispatchStatus == DispatchStatus.Denied || dispatchStatus == DispatchStatus.Approved)
                    {
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
            fuelAtLastPing = currentFuel;
            UpdateVisualRotation();
            UpdateHitboxColor();
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
                Quaternion.Inverse(rectTransform.localRotation) * new Vector3(0, -60f, 0);
        }
    }

    void FadeOut()
    {
        if (!hasBeenPinged)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        if (isSelected)
        {
            if (canvasGroup.alpha != 1f)
            {
                canvasGroup.alpha = 1f;
                SyncRouteAlpha();
            }
            return;
        }

        if (canvasGroup.alpha > minAlpha)
        {
            canvasGroup.alpha = Mathf.Max(minAlpha, canvasGroup.alpha - fadeSpeed * Time.deltaTime);
            SyncRouteAlpha();
        }
    }

    public void UpdateInternalSpeed() => _actualSpeed = speed / 10f;

    private void CheckZoomVisibility(float zoom)
    {
        bool show = zoom >= showTextZoomThreshold;
        if (callsignText.gameObject.activeSelf != show) callsignText.gameObject.SetActive(show);
    }

    private void RebuildRouteLayer()
    {
        if (waypoints.Count == 0)
        {
            foreach (var seg in lineSegments) seg.SetActive(false);
            foreach (var marker in activeMarkers) marker.SetActive(false);
            return;
        }

        foreach (var seg in lineSegments) seg.SetActive(false);
        foreach (var marker in activeMarkers) marker.SetActive(false);

        int currentMarkerIndex = 0;
        int currentSegmentIndex = 0;

        if (waypoints.Count == 0) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            SetMarker(currentMarkerIndex, waypoints[i]);
            currentMarkerIndex++;

            if (i < waypoints.Count - 1)
            {
                SetSegment(currentSegmentIndex, waypoints[i], waypoints[i + 1]);
                currentSegmentIndex++;
            }
        }

        SetSegment(currentSegmentIndex, logicalPosition, waypoints[0]);
        SyncRouteAlpha();
        UpdateHitboxColor();
    }

    private void SetMarker(int index, Vector2 pos)
    {
        GameObject marker;
        if (index < activeMarkers.Count)
        {
            marker = activeMarkers[index];
            marker.SetActive(true);
        }
        else
        {
            marker = Instantiate(waypointMarkerPrefab, transform.parent, false);
            activeMarkers.Add(marker);
        }

        marker.transform.SetSiblingIndex(transform.GetSiblingIndex());
        marker.GetComponent<RectTransform>().anchoredPosition = pos;
    }

    private void SetSegment(int index, Vector2 start, Vector2 end)
    {
        GameObject seg;
        if (index < lineSegments.Count)
        {
            seg = lineSegments[index];
            seg.SetActive(true);
        }
        else
        {
            seg = Instantiate(routeSegmentPrefab, transform.parent, false);
            lineSegments.Add(seg);
        }

        seg.transform.SetSiblingIndex(transform.GetSiblingIndex());
        UpdateSegmentLook(seg.GetComponent<RectTransform>(), start, end);
    }

    private void UpdateFirstSegment()
    {
        if (waypoints.Count == 0) return;

        int activeSegmentIndex = waypoints.Count - 1;

        if (activeSegmentIndex >= 0 && activeSegmentIndex < lineSegments.Count)
        {
            UpdateSegmentLook(lineSegments[activeSegmentIndex].GetComponent<RectTransform>(),
                              rectTransform.anchoredPosition,
                              waypoints[0]);
        }
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
        if (dispatchStatus != DispatchStatus.Pending || isOutOfFuel) return; 
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
        if (dispatchStatus != DispatchStatus.Pending || isOutOfFuel) return; 
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
        if (inStorm) return;

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

            Image parentImg = seg.GetComponent<Image>();
            if (parentImg != null)
            {
                Color pc = parentImg.color;
                pc.a = currentAlpha * 0.4f; 
                parentImg.color = pc;
            }

            Transform fuelVisualTrans = seg.transform.Find("FuelVisual");
            if (fuelVisualTrans != null)
            {
                Image childImg = fuelVisualTrans.GetComponent<Image>();
                Color cc = childImg.color;
                cc.a = currentAlpha; 
                childImg.color = cc;
            }
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
        Debug.Log($"<color=red>АВАРИЯ: {realCallsign} столкнулся!</color>");
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

        if (lineSegments != null)
            foreach (GameObject seg in lineSegments) if (seg != null) Destroy(seg);

        if (activeMarkers != null)
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

        Color iconColor = Color.white;

        if (isColliding || isOutOfFuel) iconColor = Color.red;
        else if (inStorm) iconColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        else if (isSelected) iconColor = new Color(1f, 0.9f, 0f, 1f);
        else if (isInDanger) iconColor = new Color(1f, 0.5f, 0f);
        else
        {
            if (dispatchStatus == DispatchStatus.Approved) iconColor = Color.green;
            else if (dispatchStatus == DispatchStatus.Denied) iconColor = Color.red;
            else iconColor = Color.white;
        }

        if (canvasGroup != null) iconColor.a = canvasGroup.alpha;
        hitboxVisual.color = iconColor;

        if (!isOutOfFuel || callsignText.text != "MAYDAY")
        {
            callsignText.color = iconColor;
        }
        else
        {
            callsignText.color = Color.red; 
        }

        Color fuelColor = isSelected ? new Color(1f, 0.9f, 0f, iconColor.a) : new Color(0f, 1f, 0f, iconColor.a);
        Color emptyColor = new Color(1f, 0f, 0f, iconColor.a * 0.4f);

        float maxFlightDistance = fuelAtLastPing * distancePerFuelUnit;
        float accumulatedDistance = 0f;

        if (lineSegments != null && waypoints.Count > 0)
        {
            List<int> orderedIndices = new List<int>();
            orderedIndices.Add(waypoints.Count - 1);
            for (int i = 0; i < waypoints.Count - 1; i++) orderedIndices.Add(i);

            Vector2 lastPos = rectTransform.anchoredPosition;

            foreach (int idx in orderedIndices)
            {
                if (idx < lineSegments.Count && lineSegments[idx] != null)
                {
                    Vector2 nextPos = (idx == orderedIndices[0]) ? waypoints[0] : waypoints[idx + 1];
                    float segLen = Vector2.Distance(lastPos, nextPos);

                    Image redLineImg = lineSegments[idx].GetComponent<Image>();
                    if (redLineImg != null) redLineImg.color = emptyColor;

                    Transform fuelVisualTrans = lineSegments[idx].transform.Find("FuelVisual");
                    if (fuelVisualTrans != null)
                    {
                        Image fuelImg = fuelVisualTrans.GetComponent<Image>();
                        float distLeft = maxFlightDistance - accumulatedDistance;

                        if (distLeft <= 0) fuelImg.fillAmount = 0;
                        else if (distLeft >= segLen) { fuelImg.fillAmount = 1; fuelImg.color = fuelColor; }
                        else { fuelImg.fillAmount = distLeft / segLen; fuelImg.color = fuelColor; }
                    }

                    accumulatedDistance += segLen;
                    lastPos = nextPos;
                }
            }
        }

        if (activeMarkers != null)
        {
            float distToMarker = 0f;
            Vector2 markerPathPos = rectTransform.anchoredPosition; 
            for (int i = 0; i < waypoints.Count; i++)
            {
                distToMarker += Vector2.Distance(markerPathPos, waypoints[i]);
                markerPathPos = waypoints[i];
                if (i < activeMarkers.Count && activeMarkers[i] != null)
                {
                    Image mImg = activeMarkers[i].GetComponent<Image>();
                    mImg.color = (distToMarker > maxFlightDistance) ? Color.red : fuelColor;
                }
            }
        }
    }
}