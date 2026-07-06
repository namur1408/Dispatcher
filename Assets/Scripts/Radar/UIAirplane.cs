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
    public bool isColliding = false;
    public bool isInDanger = false;
    private float dangerTimer = 0f;
    public bool inStorm = false;
    private string realCallsign;

    [HideInInspector] public bool isBigRadarCopy = false;

    [Header("Fuel Mechanics")]
    public float distancePerFuelUnit = 6f;
    public AirplaneFuel fuelSystem;
    public float currentFuel 
    {
        get => fuelSystem != null ? fuelSystem.currentFuel : 100f;
        set { if (fuelSystem != null) fuelSystem.currentFuel = value; }
    }
    public bool isOutOfFuel => fuelSystem != null && fuelSystem.isOutOfFuel;

    [Header("Audio")]
    public AudioClip pingSound;
    [Range(0f, 1f)] public float pingVolume = 0.6f;
    [Tooltip("Звук при клике на самолет")]
    public AudioClip airplaneClickSound;
    [Range(0f, 1f)] public float airplaneClickVolume = 1f;
    public AirplaneAudio audioSystem;
    public AirplaneVisuals visuals;

    public string originalCallsign = "";
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform sweepLine;

    public AirplaneMovement movement = new AirplaneMovement();
    public List<Vector2> waypoints { get => movement.waypoints; set => movement.waypoints = value; }
    public Vector2 logicalPosition { get => movement.logicalPosition; set => movement.logicalPosition = value; }
    public bool isHolding { get => movement.isHolding; set => movement.isHolding = value; }
    public float currentHoldingAngle { get => movement.currentHoldingAngle; set => movement.currentHoldingAngle = value; }
    public Vector2 holdingCenter { get => movement.holdingCenter; set => movement.holdingCenter = value; }
    
    public Vector2 targetPosition => waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : Vector2.zero;

    private bool wasInitialized = false;
    private bool isSelected = false;

    public bool hasBeenPinged = false;

    public string cargo;

    public string assignedRunway = "";
    public bool isAligningToLand = false;
    public bool isDeparting = false;
    public string departureDestination = "";

    public bool isTakingOff = false;
    public Vector2 takeoffStartPos;

    public bool isLandingPhase = false;

    private Collider2D[] myColliders;


    public void SetCollidersActive(bool active)
    {
        if (myColliders == null) myColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var col in myColliders) col.enabled = active;
    }

    public void ResetPlane()
    {
        wasInitialized = false;
        isTakingOff = false;
        isLandingPhase = false;
        isDeparting = false;
        isAligningToLand = false;
        isHolding = false;
        inStorm = false;
        hasBeenPinged = false;
        isInDanger = false;
        isColliding = false;
        dangerTimer = 0f;
        isBigRadarCopy = false;
        assignedRunway = "";
        cargo = "";
        dispatchStatus = DispatchStatus.Pending;
        waypoints.Clear();
        visuals?.CleanupRouteVisuals();
        SetCollidersActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (callsignText != null) callsignText.gameObject.SetActive(true);
        if (hitboxVisual != null) hitboxVisual.gameObject.SetActive(true);
        if (fuelSystem != null) fuelSystem.ResetFuel();
    }

    public enum DispatchStatus { Pending, Approved, Denied }
    public DispatchStatus dispatchStatus = DispatchStatus.Pending;

    public void SetAssignedRunway(string rwId, bool isLanding)
    {
        assignedRunway = rwId;
        if (isLanding)
        {
            isAligningToLand = true;
            isHolding = false; 
            if (RunwayManager.Instance != null)
            {
                Runway rw = RunwayManager.Instance.GetRunwayByID(rwId);
                if (rw != null)
                {
                    RectTransform rwRect = rw.GetComponent<RectTransform>();
                    Vector2 rwPos = rwRect != null ? rwRect.anchoredPosition : Vector2.zero;
                    Vector2 approachPoint = rw.GetAlignmentPoint(rwId, rwPos);
                    Vector2 runwayDir = rw.GetDirection(rwId);
                    while (waypoints.Count > 0)
                    {
                        Vector2 wp = waypoints[waypoints.Count - 1];
                        
                        bool nearCenter = wp.sqrMagnitude < (70f * 70f); 
                        bool nearRunway = (wp - rwPos).sqrMagnitude < (120f * 120f);
                        bool nearApproach = (wp - approachPoint).sqrMagnitude < (80f * 80f);
                        
                        Vector2 approachToWp = wp - approachPoint;
                        bool isPastApproach = Vector2.Dot(approachToWp.normalized, runwayDir) > 0.2f && 
                                              (wp - rwPos).sqrMagnitude < (200f * 200f);

                        if (nearCenter || nearRunway || nearApproach || isPastApproach)
                        {
                            waypoints.RemoveAt(waypoints.Count - 1);
                        }
                        else
                        {
                            break;
                        }
                    }

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
                    visuals?.RebuildRouteLayer(isLandingPhase);
                    UpdateVisualRotation();
                }
            }
        }
        else
        {
            isDeparting = true;
            isAligningToLand = false;
            speed = 68f; // Departure speed
            UpdateInternalSpeed();
            
            visuals?.SetVisualState(false, 0.3f);
            
            if (FlightDataManager.Instance != null)
            {
                var fd = FlightDataManager.Instance.GetFlight(originalCallsign);
                if (fd != null) fd.speed = speed;
            }
            
            if (RunwayManager.Instance != null)
            {
                Runway rw = RunwayManager.Instance.GetRunwayByID(rwId);
                if (rw != null)
                {
                    RectTransform rwRect = rw.GetComponent<RectTransform>();
                    Vector2 rwPos = rwRect != null ? rwRect.anchoredPosition : Vector2.zero;

                    logicalPosition = rwPos;
                    rectTransform.anchoredPosition = rwPos;

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

                    visuals?.RebuildRouteLayer(isLandingPhase);
                    UpdateVisualRotation();
                    
                    RunwayManager.Instance.OccupyRunway(rwId, 15f);
                }
            }
        }
        SyncRouteToGlobal();
    }

    private Vector2 GetDestinationCoordinate(string destination)
        => DestinationHelper.GetCoordinate(destination);

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
        fuelSystem = new AirplaneFuel(this, distancePerFuelUnit);
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        myColliders = GetComponentsInChildren<Collider2D>(true);

        AudioSource source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        audioSystem = new AirplaneAudio(this, source);
        visuals = new AirplaneVisuals(this, canvasGroup, callsignText, hitboxVisual, routeSegmentPrefab, waypointMarkerPrefab, transform.parent);
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (transform.parent != null && transform.parent.parent != null)
        {
            Transform localScanner = transform.parent.parent.Find("SweepLine");
            if (localScanner != null) sweepLine = localScanner;
        }
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

        if (fuelSystem != null)
        {
            fuelSystem.SetLastPosition(logicalPosition);
            fuelSystem.RecalcFuelRange();
        }

        UpdateInternalSpeed();
        if (!isBigRadarCopy && RadarManager.Instance != null && !RadarManager.Instance.activeAirplanes.Contains(this))
            RadarManager.Instance.RegisterAirplane(this);
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

        if (fuelSystem != null) fuelSystem.InitFromData(data.currentFuel);

        isHolding = false;
        waypoints = new List<Vector2>(data.savedWaypoints);

        hasBeenPinged = data.hasBeenPinged;
        if ((hasBeenPinged || data.isDeparting) && canvasGroup != null)
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
        visuals?.RebuildRouteLayer(isLandingPhase);

        isTakingOff = data.isTakingOff;
        takeoffStartPos = data.takeoffStartPos;
        if (isTakingOff)
        {
            SetCollidersActive(false);
            visuals?.SetVisualState(false, 0.3f);
        }

        isLandingPhase = data.isLandingPhase;
        if (isLandingPhase)
        {
            // HIDE UI - Make it look like it's landing
            visuals?.SetVisualState(false, 0.2f);

            // Disable ALL colliders so it's a "ghost"
            SetCollidersActive(false);
        }

        visuals?.UpdateHitboxColor();
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
        visuals?.RebuildRouteLayer(isLandingPhase);
    }

    public void AddWaypoint(Vector2 clickPos)
    {
        if (inStorm || isOutOfFuel) return;
        
        if (isHolding)
        {
            isHolding = false;
            waypoints.Clear();
            waypoints.Add(clickPos);
            waypoints.Add(Vector2.zero);
            visuals?.RebuildRouteLayer(isLandingPhase);
            UpdateVisualRotation();
            return;
        }

        if (dispatchStatus != DispatchStatus.Pending && !isDeparting) return;

        if (waypoints.Count == 0)
        {
            waypoints.Add(clickPos);
            visuals?.RebuildRouteLayer(isLandingPhase);
            UpdateVisualRotation();
            return;
        }

        int bestIndex = 0;
        float minDistance = float.MaxValue;

        float distToFirstSeg = movement.DistanceToSegment(clickPos, logicalPosition, waypoints[0]);
        if (distToFirstSeg < minDistance)
        {
            minDistance = distToFirstSeg;
            bestIndex = 0;
        }

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            float dist = movement.DistanceToSegment(clickPos, waypoints[i], waypoints[i + 1]);
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

        visuals?.RebuildRouteLayer(isLandingPhase);
        UpdateVisualRotation();
        SyncRouteToGlobal();
    }

    public void RemoveWaypoint(int index)
    {
        if (inStorm || isHolding || isOutOfFuel) return;

        if (index >= 0 && index < waypoints.Count - 1)
        {
            if (audioSystem != null) audioSystem.PlayClick();
            waypoints.RemoveAt(index);
            visuals?.RebuildRouteLayer(isLandingPhase);
            UpdateVisualRotation();
            SyncRouteToGlobal();
        }
    }

    public int GetWaypointIndexAt(Vector2 clickPos, float thresholdRadius = 30f)
    {
        return movement.GetWaypointIndexAt(clickPos, thresholdRadius);
    }

    void Update()
    {
        HandleDangerTimer();
        HandleRunwayOccupancyAbort();
        HandleTakeoff();
        if (fuelSystem != null)
        {
            fuelSystem.HandleFuelConsumption(_actualSpeed);
            fuelSystem.HandleFuelEmergency(Time.deltaTime);
        }
        HandleLowFuelWarning();
        HandleStormDetection();
        HandleMovement();
        HandlePing();
        FadeOut();
        if (transform.parent != null) CheckZoomVisibility(transform.parent.localScale.x);
        HandleDespawnCheck();
    }

    private void HandleDangerTimer()
    {
        if (dangerTimer > 0f) dangerTimer -= Time.deltaTime;
    }

    private void HandleRunwayOccupancyAbort()
    {
        if (string.IsNullOrEmpty(assignedRunway) || isDeparting) return;
        if (RunwayManager.Instance == null) return;

        Runway rw = RunwayManager.Instance.GetRunwayByID(assignedRunway);
        if (rw == null || !rw.isOccupied) return;

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
        waypoints.Add(Vector2.zero); 
        visuals?.RebuildRouteLayer(isLandingPhase);
        UpdateVisualRotation();

        if (FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.GetFlight(realCallsign);
            if (fd != null)
            {
                fd.assignedRunway = "";
                fd.isAligningToLand = false;
                fd.isLandingPhase = false;
            }
        }
    }

    private void HandleTakeoff()
    {
        if (!isTakingOff) return;

        if (takeoffStartPos == Vector2.zero && !string.IsNullOrEmpty(assignedRunway) && RunwayManager.Instance != null)
        {
            Runway rw = RunwayManager.Instance.GetRunwayByID(assignedRunway);
            if (rw != null)
            {
                RectTransform rwRect = rw.GetComponent<RectTransform>();
                if (rwRect != null)
                {
                    takeoffStartPos = rwRect.anchoredPosition;
                    if (takeoffStartPos == Vector2.zero) takeoffStartPos = new Vector2(0.001f, 0.001f);
                }
            }
        }

        float dist = takeoffStartPos != Vector2.zero ? Vector2.Distance(logicalPosition, takeoffStartPos) : 0f;

        if (takeoffStartPos != Vector2.zero && dist > 150f)
        {
            isTakingOff = false;
            SetCollidersActive(true);
            visuals?.SetVisualState(true, 1f);
            visuals?.RebuildRouteLayer(isLandingPhase);

            if (FlightDataManager.Instance != null)
                FlightDataManager.Instance.FreeBaseSlot(originalCallsign);
        }
    }

    private void HandleLowFuelWarning()
    {
        if (isOutOfFuel || (fuelSystem != null && fuelSystem.currentFuel > 30f) || (fuelSystem != null && fuelSystem.currentFuel <= 0f)) return;
        if (hitboxVisual == null) return;

        Color baseColor = Color.white;
        if (inStorm)                                     baseColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        else if (isSelected)                             baseColor = new Color(1f, 0.9f, 0f, 1f);
        else if (isInDanger)                             baseColor = new Color(1f, 0.5f, 0f);
        else if (dispatchStatus == DispatchStatus.Approved) baseColor = Color.green;
        else if (dispatchStatus == DispatchStatus.Denied)   baseColor = Color.red;

        float t = Mathf.PingPong(Time.time * 5f, 1f);
        Color blinkColor = Color.Lerp(Color.red, baseColor, t);
        if (canvasGroup != null) blinkColor.a = canvasGroup.alpha;

        hitboxVisual.color = blinkColor;
        if (callsignText != null && callsignText.text != "NO SIGNAL")
            callsignText.color = blinkColor;
    }

    private void HandleStormDetection()
    {
        if (DynamicStorm.Instance == null) return;

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
            visuals?.UpdateHitboxColor();
        }
        else if (!currentlyInStorm && inStorm)
        {
            inStorm = false;
            if (!isOutOfFuel) callsignText.text = realCallsign;
            visuals?.UpdateHitboxColor();
        }
    }

    private void HandleMovement()
    {
        float currentSpeed = inStorm ? (_actualSpeed * 0.5f) : _actualSpeed;
        if (!isHolding && waypoints.Count == 1 && waypoints[0] == Vector2.zero)
        {
            bool isWaitingForRunway = dispatchStatus == DispatchStatus.Approved && string.IsNullOrEmpty(assignedRunway);
            if (dispatchStatus == DispatchStatus.Pending || isWaitingForRunway)
            {
                if (Vector2.Distance(logicalPosition, waypoints[0]) <= holdingRadius)
                {
                    if (!isOutOfFuel) StartHolding(waypoints[0]);
                    return;
                }
            }
        }

        bool reachedWaypoint = movement.UpdatePosition(Time.deltaTime, currentSpeed, holdingRadius);

        if (!reachedWaypoint) return;
        if (waypoints.Count > 1)
        {
            waypoints.RemoveAt(0);
            visuals?.RebuildRouteLayer(isLandingPhase);
            return;
        }
        HandleWaypointReached();
    }

    private void HandleWaypointReached()
    {
        if (isAligningToLand)
        {
            isAligningToLand = false;
            isLandingPhase = true;
            waypoints.Clear();
            visuals?.SetVisualState(false, 0.2f);
            SetCollidersActive(false);

            Vector2 runwayPos = Vector2.zero;
            if (RunwayManager.Instance != null)
            {
                Runway rw = RunwayManager.Instance.GetRunwayByID(assignedRunway);
                if (rw != null) runwayPos = rw.GetComponent<RectTransform>().anchoredPosition;
            }

            waypoints.Add(runwayPos);
            visuals?.RebuildRouteLayer(isLandingPhase);
            UpdateVisualRotation();
            return;
        }

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

        if (dispatchStatus == DispatchStatus.Approved && hasRunway && Vector2.Distance(logicalPosition, targetRunwayPos) < 30f)
        {
            if (FlightDataManager.Instance != null)
            {
                var fd = FlightDataManager.Instance.GetFlight(realCallsign);
                if (fd != null) GameEvents.FlightLanded(fd);
            }
            if (VideoLandingManager.Instance != null) VideoLandingManager.Instance.RequestLandingVideo();
            if (RunwayManager.Instance != null) RunwayManager.Instance.OccupyRunway(assignedRunway, 15f);
            AirplaneSpawner.Instance.ReturnPlaneToPool(this);
        }
        else
        {
            AirplaneSpawner.Instance.ReturnPlaneToPool(this);
        }
    }

    private void HandleDespawnCheck()
    {
        if (Vector2.Distance(Vector2.zero, logicalPosition) <= despawnRadius) return;

        if (FlightDataManager.Instance != null)
        {
            var flight = FlightDataManager.Instance.GetFlight(originalCallsign);
            if (flight != null)
            {
                bool shouldRemove = (flight.hasLanded && FlightDataManager.Instance.ShouldPlaneDepart(flight))
                                 || flight.isDeparting
                                 || (flight.decisionMade && !flight.approved);
                if (shouldRemove) FlightDataManager.Instance.RemoveDepartedPlane(originalCallsign);
            }
        }
        AirplaneSpawner.Instance.ReturnPlaneToPool(this);
    }

    public void ReturnToPool()
    {
        AirplaneSpawner.Instance.ReturnPlaneToPool(this);
    }

    public void UpdateHitboxColor()
    {
        visuals?.UpdateHitboxColor();
    }

    private void StartHolding(Vector2 center)
    {
        movement.StartHolding(center);
        visuals?.RebuildRouteLayer(isLandingPhase);
    }

    void HandlePing()
    {
        if (sweepLine == null) return;
        float planeAngle = Mathf.Atan2(logicalPosition.y, logicalPosition.x) * Mathf.Rad2Deg;
        Transform radarParent = rectTransform.parent;
        Vector3 sweepWorldUp = sweepLine.up;
        Vector3 sweepLocalDir = radarParent != null
            ? radarParent.InverseTransformDirection(sweepWorldUp)
            : sweepWorldUp;
        float sweepAngle = Mathf.Atan2(sweepLocalDir.y, sweepLocalDir.x) * Mathf.Rad2Deg;

        if (Mathf.Abs(Mathf.DeltaAngle(sweepAngle, planeAngle)) < 3f)
        {
            rectTransform.anchoredPosition = logicalPosition;
            UpdateVisualRotation();
            visuals?.UpdateHitboxColor();
            if (canvasGroup != null) canvasGroup.alpha = (isLandingPhase || isTakingOff) ? 0.3f : 1f;

            if (!isTakingOff && !isLandingPhase && !isHolding)
                visuals?.UpdateFirstSegment();

            if (audioSystem != null) audioSystem.PlayPing();
            hasBeenPinged = true;
        }
    }

    void UpdateVisualRotation()
    {
        rectTransform.rotation = movement.GetVisualRotation(holdingRadius);
        if (callsignText != null)
        {
            callsignText.transform.rotation = Quaternion.identity;
            callsignText.rectTransform.localPosition =
                Quaternion.Inverse(rectTransform.localRotation) * new Vector3(0, -60f, 0);
        }
    }

    void FadeOut()
    {
        if (isLandingPhase || isTakingOff)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0.3f;
            visuals?.SyncRouteAlpha();
            return;
        }

        if (!hasBeenPinged)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            visuals?.SyncRouteAlpha();
            return;
        }

        if (isSelected)
        {
            if (canvasGroup.alpha != 1f)
            {
                canvasGroup.alpha = 1f;
                visuals?.SyncRouteAlpha();
            }
            return;
        }

        if (canvasGroup != null && canvasGroup.alpha > minAlpha)
        {
            canvasGroup.alpha = Mathf.Max(minAlpha, canvasGroup.alpha - fadeSpeed * Time.deltaTime);
            visuals?.SyncRouteAlpha();
        }
    }

    public void UpdateInternalSpeed() => _actualSpeed = speed / 29f;

    private void CheckZoomVisibility(float zoom)
    {
        bool show = zoom >= showTextZoomThreshold;
        if (callsignText.gameObject.activeSelf != show) callsignText.gameObject.SetActive(show);
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
        visuals?.RebuildRouteLayer(isLandingPhase);
        visuals?.UpdateHitboxColor();
        SyncRouteToGlobal();
    }

    public void Deny()
    {
        if (dispatchStatus != DispatchStatus.Pending || isOutOfFuel) return;
        dispatchStatus = DispatchStatus.Denied;

        isHolding = false;

        waypoints.Clear();
        waypoints.Add(logicalPosition.normalized * (despawnRadius + 300f));

        UpdateVisualRotation();
        visuals?.RebuildRouteLayer(isLandingPhase);
        visuals?.UpdateHitboxColor();
        SyncRouteToGlobal();
    }

    public void SetHighlight(bool h)
    {
        isSelected = h;
        visuals?.UpdateHitboxColor();
    }



    public void TriggerSelection()
    {
        if (inStorm) return;
        if (audioSystem != null) audioSystem.PlayClick();

        if (BigRadarTerminal.Instance != null) BigRadarTerminal.Instance.SelectPlane(this);
        UIAirplane[] planes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        foreach (var p in planes) p.SetHighlight(p == this);
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
        if (AirplaneSpawner.Instance != null && FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.GetFlight(originalCallsign);
            if (fd != null) 
            {
                AirplaneSpawner.Instance.NotifyPlaneCrashed(fd);
            }
            FlightDataManager.Instance.RemoveDepartedPlane(originalCallsign);
        }

        visuals?.UpdateHitboxColor();
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

    public void CleanupRouteVisuals()
    {
        visuals?.CleanupRouteVisuals();
    }

    private void OnDestroy()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.UnregisterAirplane(this);

        CleanupRouteVisuals();
    }

    public void SetWarning(bool warn)
    {
        if (isColliding) return;
        
        if (warn)
        {
            dangerTimer = 2f; 
            if (!isInDanger)
            {
                isInDanger = true;
                visuals?.UpdateHitboxColor();
            }
        }
        else
        {
            if (isInDanger && dangerTimer <= 0f)
            {
                isInDanger = false;
                visuals?.UpdateHitboxColor();
            }
        }
    }

    public void SyncFromBigRadar(UIAirplane bigRadarPlane)
    {
        waypoints = new List<Vector2>(bigRadarPlane.waypoints);
        dispatchStatus = bigRadarPlane.dispatchStatus;
        isHolding = bigRadarPlane.isHolding;
        assignedRunway = bigRadarPlane.assignedRunway;
        isAligningToLand = bigRadarPlane.isAligningToLand;
        visuals?.RebuildRouteLayer(isLandingPhase);
        UpdateVisualRotation();
    }

    private void SyncRouteToGlobal()
    {
        if (FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.GetFlight(originalCallsign);
            if (fd != null)
            {
                fd.savedWaypoints = new List<Vector2>(waypoints);
                fd.assignedRunway = assignedRunway;
                fd.isAligningToLand = isAligningToLand;
                
                if (dispatchStatus != DispatchStatus.Pending)
                {
                    fd.decisionMade = true;
                    fd.approved = (dispatchStatus == DispatchStatus.Approved);
                }
            }
        }

        if (isBigRadarCopy && RadarManager.Instance != null)
        {
            var originalPlane = RadarManager.Instance.activeAirplanes.Find(p => p != null && p.originalCallsign == originalCallsign);
            if (originalPlane != null)
            {
                originalPlane.SyncFromBigRadar(this);
            }
        }
    }
}