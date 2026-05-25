using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIAirplane : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1f;
    private float _actualSpeed;
    public float despawnRadius = 680f;
    public float fadeSpeed = 0.5f;
    public float minAlpha = 0.3f;
    public float showTextZoomThreshold = 1.2f;
    public float routeLineWidth = 2f;

    [Header("Holding Pattern Settings")]
    public float holdingRadius = 80f;
    public float maxHoldingTime = 135f;

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
    public float distancePerFuelUnit = 6f;
    public float emergencyTimer = 20f;
    private float fuelAtLastPing;
    private bool isOutOfFuel = false;
    private Vector2 lastPosition;

    [Header("Audio")]
    public AudioClip pingSound;
    [Range(0f, 1f)] public float pingVolume = 0.6f; // <-- НОВОЕ: Ползунок громкости
    private AudioSource audioSource;
    private float lastPingTime = 0f;

    public string originalCallsign = "";
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

    public bool hasBeenPinged = false;

    private bool isHolding = false;
    private float holdingTimer = 0f;
    private float currentHoldingAngle = 0f;
    private Vector2 holdingCenter;

    public string cargo;

    public string assignedRunway = "";
    public bool isAligningToLand = false;
    public bool isDeparting = false;
    public string departureDestination = "";

    public bool isTakingOff = false;
    public Vector2 takeoffStartPos;

    public bool isLandingPhase = false;

    public void SetCollidersActive(bool active)
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders) col.enabled = active;
        if (hitboxVisual != null) hitboxVisual.gameObject.SetActive(active);
    }

    public enum DispatchStatus { Pending, Approved, Denied }
    public DispatchStatus dispatchStatus = DispatchStatus.Pending;

    public void SetAssignedRunway(string rwId, bool isLanding)
    {
        assignedRunway = rwId;
        if (isLanding)
        {
            isAligningToLand = true;
            isHolding = false; // Stop holding immediately!
            if (RunwayManager.Instance != null)
            {
                Runway rw = RunwayManager.Instance.GetRunwayByID(rwId);
                if (rw != null)
                {
                    // Clear all previous waypoints (including holding or transit points)
                    waypoints.Clear();

                    RectTransform rwRect = rw.GetComponent<RectTransform>();
                    Vector2 rwPos = rwRect != null ? rwRect.anchoredPosition : Vector2.zero;
                    Vector2 approachPoint = rw.GetAlignmentPoint(rwId, rwPos);
                    Vector2 runwayDir = rw.GetDirection(rwId);
                    
                    // SMART APPROACH LOGIC
                    Vector2 lastPoint = waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : logicalPosition;
                    
                    Vector2 diff = approachPoint - lastPoint;
                    if (diff.sqrMagnitude > 1f)
                    {
                        Vector2 incomingDir = diff.normalized;
                        float turnAngle = Vector2.Angle(incomingDir, runwayDir);
                        
                        // If the plane has to make a turn greater than 110 degrees, it's too sharp
                        if (turnAngle > 110f) 
                        {
                            Vector2 toPlane = -incomingDir;
                            
                            // 1. Calculate side direction (perpendicular to runway)
                            Vector2 sideDir = new Vector2(-runwayDir.y, runwayDir.x);
                            if (Vector2.Dot(toPlane, sideDir) < 0) sideDir = -sideDir;
                            
                            // 2. Add Base Leg: it's "beside" the approach point
                            Vector3 baseLeg = approachPoint + sideDir * (rw.alignmentDistance * 0.8f);
                            waypoints.Add(baseLeg);
                        }
                    }

                    waypoints.Add(approachPoint);
                    RebuildRouteLayer();
                    UpdateVisualRotation();
                }
            }
        }
        else
        {
            isDeparting = true;
            if (RunwayManager.Instance != null)
            {
                Runway rw = RunwayManager.Instance.GetRunwayByID(rwId);
                if (rw != null)
                {
                    RectTransform rwRect = rw.GetComponent<RectTransform>();
                    Vector2 rwPos = rwRect != null ? rwRect.anchoredPosition : Vector2.zero;

                    logicalPosition = rwPos;
                    rectTransform.anchoredPosition = rwPos;

                    // Отключаем хитбокс на время взлета
                    isTakingOff = true;
                    takeoffStartPos = rwPos;
                    SetCollidersActive(false);

                    Vector2 dir = -rw.GetDirection(rwId);
                    waypoints.Clear();

                    Vector2 destPos = GetDestinationCoordinate(departureDestination);
                    if (destPos != Vector2.zero)
                    {
                        // 1. Initial takeoff climb/run (150 units in runway direction)
                        Vector2 climbPoint = rwPos + dir * 150f;
                        waypoints.Add(climbPoint);

                        // 2. Head to the destination coordinate
                        waypoints.Add(destPos);

                        // 3. Continue in that direction past the destination to the despawn boundary
                        Vector2 outDir = (destPos - climbPoint).normalized;
                        if (outDir == Vector2.zero) outDir = dir;
                        waypoints.Add(destPos + outDir * despawnRadius);
                    }
                    else
                    {
                        // Fallback: fly straight along the runway
                        waypoints.Add(rwPos + dir * 500f);
                        waypoints.Add(rwPos + dir * despawnRadius);
                    }

                    RebuildRouteLayer();
                    UpdateVisualRotation();
                    
                    // Occupy runway for takeoff
                    RunwayManager.Instance.OccupyRunway(rwId, 15f);
                }
            }
        }
    }

    private Vector2 GetDestinationCoordinate(string destination)
    {
        if (string.IsNullOrEmpty(destination)) return Vector2.zero;

        switch (destination)
        {
            case "Bastion-1": return new Vector2(-416f, 476f);
            case "Bastion-2": return new Vector2(400f, 400f);
            case "Bastion-3": return new Vector2(-535f, 119f);
            case "Bastion-4": return new Vector2(0f, 535f);
            case "Bastion-5": return new Vector2(437f, -357f);
            case "Bastion-6": return new Vector2(-450f, -400f);
            case "Bastion-7": return new Vector2(500f, 100f);
            case "Bastion-8": return new Vector2(150f, -500f);
            case "Bastion-9": return new Vector2(-200f, 500f);
            case "Sector-Z":  return new Vector2(0f, 535f);
            default:
                int hash = destination.GetHashCode();
                float angle = Mathf.Abs(hash % 360) * Mathf.Deg2Rad;
                float radius = 480f + Mathf.Abs(hash % 50);
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }

    public List<Vector2> GetWaypoints() => new List<Vector2>(waypoints);

    private void UpdateDespawnRadius()
    {
        if (AirplaneSpawner.Instance != null)
        {
            despawnRadius = Mathf.Max(despawnRadius, AirplaneSpawner.Instance.spawnRadius + 150f);
        }
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        audioSource.volume = pingVolume; // <-- НОВОЕ: Применяем громкость при старте
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        // Ищем SweepLine в своей иерархии радара
        if (transform.parent != null && transform.parent.parent != null)
        {
            Transform localScanner = transform.parent.parent.Find("SweepLine");
            if (localScanner != null) sweepLine = localScanner;
        }
        // Фолбэк
        if (sweepLine == null)
        {
            GameObject foundScanner = GameObject.Find("SweepLine");
            if (foundScanner != null) sweepLine = foundScanner.transform;
        }
        
        UpdateDespawnRadius();
    }

    void Start()
    {
        UpdateDespawnRadius();

        if (!wasInitialized)
        {
            string[] availablePrefixes = { "QY", "GE", "KO", "LX", "TR" };
            string randomPrefix = availablePrefixes[Random.Range(0, availablePrefixes.Length)];
            string newCall = randomPrefix + "-" + Random.Range(100, 999);
            callsignText.text = newCall;
            realCallsign = newCall;
            originalCallsign = newCall;

            string[] cargoTypes = { "Medicines", "People", "Food", "Scrap" };
            cargo = cargoTypes[Random.Range(0, cargoTypes.Length)];
            wasInitialized = true;
        }

        // Only set from text if not already initialized by a dedicated method
        if (string.IsNullOrEmpty(realCallsign)) realCallsign = callsignText.text;
        if (string.IsNullOrEmpty(originalCallsign)) originalCallsign = realCallsign;

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
        originalCallsign = newCallsign;

        if (string.IsNullOrEmpty(cargo))
        {
            string[] cargoTypes = { "Medicines", "People", "Food", "Scrap" };
            cargo = cargoTypes[Random.Range(0, cargoTypes.Length)];
        }
    }

    public void InitializeFromData(FlightData data)
    {
        UpdateDespawnRadius();
        wasInitialized = true;
        callsignText.text = data.callsign;
        realCallsign = data.callsign;

        logicalPosition = data.position;
        rectTransform.anchoredPosition = data.position;
        speed = data.speed;

        cargo = data.cargo;

        assignedRunway = data.assignedRunway;
        isAligningToLand = data.isAligningToLand;
        isDeparting = data.isDeparting;
        departureDestination = data.departureDestination;

        currentFuel = data.currentFuel;
        fuelAtLastPing = currentFuel;
        isOutOfFuel = (currentFuel <= 0);

        isHolding = false;
        waypoints = new List<Vector2>(data.savedWaypoints);

        hasBeenPinged = data.hasBeenPinged;
        if (hasBeenPinged && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (data.decisionMade)
        {
            dispatchStatus = data.approved ? DispatchStatus.Approved : DispatchStatus.Denied;

            if (!data.approved)
            {
                waypoints.Clear();
                waypoints.Add(logicalPosition.normalized * (despawnRadius + 300f));
            }
            else
            {
                if (waypoints.Count == 0) waypoints.Add(Vector2.zero);
            }
        }
        else
        {
            dispatchStatus = DispatchStatus.Pending;
            if (waypoints.Count == 0) waypoints.Add(Vector2.zero);
        }
        originalCallsign = data.callsign;
        UpdateVisualRotation();
        RebuildRouteLayer();

        isTakingOff = data.isTakingOff;
        takeoffStartPos = data.takeoffStartPos;
        if (isTakingOff)
        {
            SetCollidersActive(false);
            Debug.Log($"<color=green>[UIAirplane] Restored {realCallsign} in takeoff state: isTakingOff={isTakingOff}, takeoffStartPos={takeoffStartPos}, pos={logicalPosition}</color>");
        }

        isLandingPhase = data.isLandingPhase;
        if (isLandingPhase)
        {
            // HIDE UI - Make it look like it's landing
            if (canvasGroup != null) canvasGroup.alpha = 0.2f;
            if (callsignText != null) callsignText.gameObject.SetActive(false);
            foreach (var marker in activeMarkers) if (marker != null) marker.SetActive(false);
            foreach (var segment in lineSegments) if (segment != null) segment.SetActive(false);

            // Disable ALL colliders so it's a "ghost"
            SetCollidersActive(false);
            Debug.Log($"<color=green>[UIAirplane] Restored {realCallsign} in landing state: isLandingPhase={isLandingPhase}, pos={logicalPosition}</color>");
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
        
        if (isHolding)
        {
            isHolding = false;
            waypoints.Clear();
            waypoints.Add(clickPos);
            waypoints.Add(Vector2.zero); // Автоматически добавляем точку в центр
            RebuildRouteLayer();
            UpdateVisualRotation();
            return;
        }

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

        if (waypoints.Count > 0 && waypoints[waypoints.Count - 1] != Vector2.zero)
        {
            if (bestIndex == waypoints.Count)
            {
                bestIndex = waypoints.Count - 1;
            }
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
        if (inStorm || isHolding || isOutOfFuel) return;

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
        if (!string.IsNullOrEmpty(assignedRunway) && !isDeparting)
        {
            if (RunwayManager.Instance != null)
            {
                Runway rw = RunwayManager.Instance.GetRunwayByID(assignedRunway);
                if (rw != null && rw.isOccupied)
                {
                    Debug.Log($"<color=orange>[UIAirplane] {realCallsign} aborting landing! Runway {assignedRunway} is physically occupied.</color>");
                    assignedRunway = "";
                    isAligningToLand = false;
                    
                    if (isLandingPhase)
                    {
                        isLandingPhase = false;
                        SetCollidersActive(true);
                        if (canvasGroup != null) canvasGroup.alpha = 1f;
                        if (callsignText != null) callsignText.gameObject.SetActive(true);
                    }
                    
                    waypoints.Clear();
                    waypoints.Add(Vector2.zero); // Fly to center
                    RebuildRouteLayer();
                    UpdateVisualRotation();
                    
                    if (FlightDataManager.Instance != null)
                    {
                        var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == realCallsign);
                        if (fd != null)
                        {
                            fd.assignedRunway = "";
                            fd.isAligningToLand = false;
                            fd.isLandingPhase = false;
                        }
                    }
                }
            }
        }

        if (isTakingOff)
        {
            // Если takeoffStartPos равен Vector2.zero, мы восстанавливаем его на основе assignedRunway
            if (takeoffStartPos == Vector2.zero && !string.IsNullOrEmpty(assignedRunway) && RunwayManager.Instance != null)
            {
                Runway rw = RunwayManager.Instance.GetRunwayByID(assignedRunway);
                if (rw != null)
                {
                    RectTransform rwRect = rw.GetComponent<RectTransform>();
                    if (rwRect != null)
                    {
                        takeoffStartPos = rwRect.anchoredPosition;
                        // Смещение на микро-значение, если ВПП находится ровно в (0,0), чтобы избежать повторных поисков
                        if (takeoffStartPos == Vector2.zero)
                        {
                            takeoffStartPos = new Vector2(0.001f, 0.001f);
                        }
                        Debug.Log($"<color=yellow>[UIAirplane] Self-healed/restored takeoffStartPos to: {takeoffStartPos} for {realCallsign}</color>");
                    }
                }
            }

            float dist = takeoffStartPos != Vector2.zero ? Vector2.Distance(logicalPosition, takeoffStartPos) : 0f;

            // Если самолет отлетел от полосы более чем на 150 единиц, он "взлетел"
            if (takeoffStartPos != Vector2.zero && dist > 150f)
            {
                isTakingOff = false;
                SetCollidersActive(true);
                Debug.Log($"<color=cyan>[UIAirplane] {realCallsign} has taken off! Hitbox enabled. Distance: {dist:F1} units from start {takeoffStartPos}</color>");
            }
        }

        float distanceMoved = Vector2.Distance(logicalPosition, lastPosition);
        lastPosition = logicalPosition;

        if (!isOutOfFuel && distanceMoved > 0)
        {
            float fuelConsumed = distanceMoved / distancePerFuelUnit;
            currentFuel -= fuelConsumed;
            if (cargo == "Fuel" && FlightDataManager.Instance != null)
            {
                var flightData = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == realCallsign);
                if (flightData != null)
                {
                    flightData.cargoAmount = Mathf.Max(0, Mathf.RoundToInt(currentFuel));
                }
            }

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

                if (AirplaneSpawner.Instance != null && FlightDataManager.Instance != null)
                {
                    var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == originalCallsign);
                    if (fd != null) AirplaneSpawner.Instance.NotifyPlaneCrashed(fd);
                }

                DestroyPlane();
                return;
            }
        }

        if (DynamicStorm.Instance != null)
        {
            bool currentlyInStorm = DynamicStorm.Instance.IsInStorm(rectTransform.position);

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
            // All planes in the holding pattern circle indefinitely (no timer countdown)
            float angularSpeed = (currentSpeed / holdingRadius) * Mathf.Rad2Deg;
            currentHoldingAngle += angularSpeed * Time.deltaTime;
            Vector2 circleTarget = holdingCenter + new Vector2(Mathf.Cos(currentHoldingAngle * Mathf.Deg2Rad), Mathf.Sin(currentHoldingAngle * Mathf.Deg2Rad)) * holdingRadius;
            logicalPosition = Vector2.MoveTowards(logicalPosition, circleTarget, currentSpeed * Time.deltaTime);
        }
        else if (waypoints.Count > 0)
        {
            Vector2 currentTarget = waypoints[0];

            bool isWaitingForRunway = (dispatchStatus == DispatchStatus.Approved && string.IsNullOrEmpty(assignedRunway));
            if (waypoints.Count == 1 && (dispatchStatus == DispatchStatus.Pending || isWaitingForRunway) && currentTarget == Vector2.zero)
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
                else // We reached the LAST waypoint
                {
                    // Case A: This was the point BEFORE the runway (alignment point)
                    if (isAligningToLand)
                    {
                        isAligningToLand = false;
                        isLandingPhase = true;
                        waypoints.Clear();

                        // HIDE UI - Make it look like it's landing
                        if (canvasGroup != null) canvasGroup.alpha = 0.2f;
                        if (callsignText != null) callsignText.gameObject.SetActive(false);
                        foreach (var marker in activeMarkers) if (marker != null) marker.SetActive(false);
                        foreach (var segment in lineSegments) if (segment != null) segment.SetActive(false);

                        // Disable ALL colliders so it's a "ghost"
                        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
                        foreach (var col in colliders) col.enabled = false;
                        if (hitboxVisual != null) hitboxVisual.gameObject.SetActive(false);

                        // Target the REAL runway position now
                        Vector2 runwayPos = Vector2.zero;
                        if (RunwayManager.Instance != null)
                        {
                            Runway rw = RunwayManager.Instance.GetRunwayByID(assignedRunway);
                            if (rw != null) runwayPos = rw.GetComponent<RectTransform>().anchoredPosition;
                        }

                        waypoints.Add(runwayPos);
                        RebuildRouteLayer();
                        UpdateVisualRotation();
                        return; 
                    }

                    // Case B: We reached the ACTUAL runway
                    Vector2 targetRunwayPos = Vector2.zero;
                    bool hasRunway = false;
                    if (!string.IsNullOrEmpty(assignedRunway) && RunwayManager.Instance != null)
                    {
                        Runway rw = RunwayManager.Instance.GetRunwayByID(assignedRunway);
                        if (rw != null) 
                        {
                            targetRunwayPos = rw.GetComponent<RectTransform>().anchoredPosition;
                            hasRunway = true;
                        }
                    }

                    // Check if we actually landed - use a larger threshold (30f) to prevent "freezing"
                    if (dispatchStatus == DispatchStatus.Approved && hasRunway && Vector2.Distance(logicalPosition, targetRunwayPos) < 30f)
                    {
                        if (FlightDataManager.Instance != null) FlightDataManager.Instance.MarkFlightAsLanded(realCallsign);
                        if (VideoLandingManager.Instance != null) VideoLandingManager.Instance.RequestLandingVideo();
                        if (RunwayManager.Instance != null) RunwayManager.Instance.OccupyRunway(assignedRunway, 15f);
                        Destroy(gameObject);
                    }
                    else 
                    {
                        // In any other case, if we reached the end of waypoints, just despawn
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
        {
            if (FlightDataManager.Instance != null)
            {
                var flight = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == originalCallsign);
                if (flight != null && flight.hasLanded && FlightDataManager.Instance.ShouldPlaneDepart(flight))
                {
                    FlightDataManager.Instance.RemoveDepartedPlane(originalCallsign);
                }
            }
            Destroy(gameObject);
        }
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

        // planeAngle — угол самолёта в локальном пространстве radarContent
        float planeAngle = Mathf.Atan2(logicalPosition.y, logicalPosition.x) * Mathf.Rad2Deg;

        // sweepLine.up — мировые координаты. Переводим в локальное пространство radarContent,
        // чтобы обе системы координат совпадали.
        Transform radarParent = rectTransform.parent;
        Vector3 sweepWorldUp = sweepLine.up;
        Vector3 sweepLocalDir = radarParent != null
            ? radarParent.InverseTransformDirection(sweepWorldUp)
            : sweepWorldUp;
        float sweepAngle = Mathf.Atan2(sweepLocalDir.y, sweepLocalDir.x) * Mathf.Rad2Deg;

        if (Mathf.Abs(Mathf.DeltaAngle(sweepAngle, planeAngle)) < 3f)
        {
            rectTransform.anchoredPosition = logicalPosition;
            fuelAtLastPing = currentFuel;
            UpdateVisualRotation();
            UpdateHitboxColor();
            if (canvasGroup != null) canvasGroup.alpha = isLandingPhase ? 0.2f : 1f;

            if (pingSound != null && Time.time - lastPingTime > 1.0f)
            {
                audioSource.PlayOneShot(pingSound, pingVolume);
                lastPingTime = Time.time;
            }
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
        if (isLandingPhase)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0.2f;
            SyncRouteAlpha();
            return;
        }

        if (!hasBeenPinged)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            SyncRouteAlpha();
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

        if (canvasGroup != null && canvasGroup.alpha > minAlpha)
        {
            canvasGroup.alpha = Mathf.Max(minAlpha, canvasGroup.alpha - fadeSpeed * Time.deltaTime);
            SyncRouteAlpha();
        }
    }

    public void UpdateInternalSpeed() => _actualSpeed = speed / 25f;

    private void CheckZoomVisibility(float zoom)
    {
        bool show = zoom >= showTextZoomThreshold;
        if (callsignText.gameObject.activeSelf != show) callsignText.gameObject.SetActive(show);
    }

    private void RebuildRouteLayer()
    {
        if (isLandingPhase || waypoints.Count == 0)
        {
            foreach (var seg in lineSegments) if (seg != null) seg.SetActive(false);
            foreach (var marker in activeMarkers) if (marker != null) marker.SetActive(false);
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
        if (isLandingPhase || waypoints.Count == 0) return;

        int activeSegmentIndex = waypoints.Count - 1;

        if (activeSegmentIndex >= 0 && activeSegmentIndex < lineSegments.Count)
        {
            UpdateSegmentLook(lineSegments[activeSegmentIndex].GetComponent<RectTransform>(),
                              logicalPosition,
                              waypoints[0]);
        }
    }

    private void UpdateSegmentLook(RectTransform segRect, Vector2 start, Vector2 end)
    {
        float dist = Vector2.Distance(start, end);

        // Принудительно ставим Pivot вниз по центру, чтобы линия росла только ВПЕРЕД от самолета, а не в обе стороны
        segRect.pivot = new Vector2(0.5f, 0f);

        // Компенсируем Scale префаба только в длину, если пользователь его уменьшил
        float scaleY = segRect.localScale.y != 0 ? segRect.localScale.y : 1f;

        segRect.sizeDelta = new Vector2(routeLineWidth, dist / scaleY);
        segRect.anchoredPosition = start;
        Vector2 dir = (end - start).normalized;
        segRect.rotation = Quaternion.Euler(0, 0, (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg) - 90f);
    }

    public void Approve()
    {
        if (dispatchStatus != DispatchStatus.Pending || isOutOfFuel) return;
        dispatchStatus = DispatchStatus.Approved;

        isHolding = false;

        if (waypoints.Count == 0 || waypoints[waypoints.Count - 1] != Vector2.zero)
        {
            waypoints.Add(Vector2.zero);
        }

        UpdateVisualRotation();
        RebuildRouteLayer();
        UpdateHitboxColor();
    }

    public void Deny()
    {
        if (dispatchStatus != DispatchStatus.Pending || isOutOfFuel) return;
        dispatchStatus = DispatchStatus.Denied;

        isHolding = false;

        waypoints.Clear();
        waypoints.Add(logicalPosition.normalized * (despawnRadius + 300f));

        UpdateVisualRotation();
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

        if (AirplaneSpawner.Instance != null && FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == originalCallsign);
            if (fd != null) AirplaneSpawner.Instance.NotifyPlaneCrashed(fd);
        }

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
                    mImg.color = (distToMarker > maxFlightDistance) ? new Color(1f, 0f, 0f, iconColor.a) : fuelColor;
                }
            }
        }
    }
}