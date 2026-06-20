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
    public bool isInDanger = false;
    private float dangerTimer = 0f;
    public bool inStorm = false;
    private string realCallsign;

    [HideInInspector] public bool isBigRadarCopy = false;

    [Header("Fuel Mechanics")]
    public float currentFuel = 100f;
    public float distancePerFuelUnit = 6f;
    public float emergencyTimer = 20f;
    private float fuelRangeFromRouteOrigin; // Absolute max distance from routeOriginPosition, set once per route
    private Vector2 routeOriginPosition;    // Plane position when route fuel budget was calculated
    private bool isOutOfFuel = false;
    private Vector2 lastPosition;

    [Header("Audio")]
    public AudioClip pingSound;
    [Range(0f, 1f)] public float pingVolume = 0.6f;
    [Tooltip("Звук при клике на самолет")]
    public AudioClip airplaneClickSound;
    [Range(0f, 1f)] public float airplaneClickVolume = 1f;
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

    private Collider2D[] myColliders;


    public void SetCollidersActive(bool active)
    {
        if (myColliders == null) myColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var col in myColliders) col.enabled = active;
    }

    /// <summary>
    /// Устанавливает видимость визуальных элементов самолёта.
    /// Используется при взлёте, посадке и восстановлении из сейва.
    /// </summary>
    private void SetVisualState(bool visible, float alpha = 1f)
    {
        if (canvasGroup != null) canvasGroup.alpha = alpha;
        if (callsignText != null) callsignText.gameObject.SetActive(visible);
        foreach (var marker in activeMarkers) if (marker != null) marker.SetActive(visible);
        foreach (var segment in lineSegments) if (segment != null) segment.SetActive(visible);
        if (hitboxVisual != null) hitboxVisual.gameObject.SetActive(visible);
    }


    public void ResetPlane()
    {
        wasInitialized = false;
        isTakingOff = false;
        isLandingPhase = false;
        isDeparting = false;
        isAligningToLand = false;
        isHolding = false;
        isOutOfFuel = false;
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
        foreach (var marker in activeMarkers) if (marker != null) Destroy(marker);
        activeMarkers.Clear();
        foreach (var seg in lineSegments) if (seg != null) Destroy(seg);
        lineSegments.Clear();
        SetCollidersActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (callsignText != null) callsignText.gameObject.SetActive(true);
        if (hitboxVisual != null) hitboxVisual.gameObject.SetActive(true);
        emergencyTimer = 20f;
        currentFuel = 100f; // Default, will be overwritten by InitializeFromData
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
                    RectTransform rwRect = rw.GetComponent<RectTransform>();
                    Vector2 rwPos = rwRect != null ? rwRect.anchoredPosition : Vector2.zero;
                    Vector2 approachPoint = rw.GetAlignmentPoint(rwId, rwPos);
                    Vector2 runwayDir = rw.GetDirection(rwId);
                    
                    // ВАЖНО: Умная фильтрация старых маркеров.
                    // Если последние маркеры маршрута уходят слишком глубоко в базу (0,0), 
                    // либо находятся слишком близко к полосе, мы их удаляем, 
                    // чтобы самолет не делал "крюк", а плавно переходил на глиссаду.
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
            isAligningToLand = false;
            speed = 68f; // Departure speed (reduced for realism)
            UpdateInternalSpeed();
            
            // Визуальный эффект взлёта: тусклость + скрываем текст/маршрут (как при посадке)
            SetVisualState(false, 0.3f);
            
            if (FlightDataManager.Instance != null)
            {
                var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == originalCallsign);
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
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        myColliders = GetComponentsInChildren<Collider2D>(true);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
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
        RecalcFuelRange();

        UpdateInternalSpeed();
        // Регистрируем только если не зарегистрировали раньше вручную (например, SpawnDepartingNow)
        // Копии большого радара НЕ регистрируются — у BigRadarLoader своя система конфликтов
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

        currentFuel = data.currentFuel;
        RecalcFuelRange();
        isOutOfFuel = (currentFuel <= 0);

        isHolding = false;
        waypoints = new List<Vector2>(data.savedWaypoints);

        hasBeenPinged = data.hasBeenPinged;
        // Вылетающие самолеты всегда видимы сразу — без ожидания пинга радара
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
        RebuildRouteLayer();

        isTakingOff = data.isTakingOff;
        takeoffStartPos = data.takeoffStartPos;
        if (isTakingOff)
        {
            SetCollidersActive(false);
            // Визуальное состояние взлёта при восстановлении
            SetVisualState(false, 0.3f);
        }

        isLandingPhase = data.isLandingPhase;
        if (isLandingPhase)
        {
            // HIDE UI - Make it look like it's landing
            SetVisualState(false, 0.2f);

            // Disable ALL colliders so it's a "ghost"
            SetCollidersActive(false);
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

        if (dispatchStatus != DispatchStatus.Pending && !isDeparting) return;

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
        SyncRouteToGlobal();
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
            PlayAirplaneClickSound();
            waypoints.RemoveAt(index);
            RebuildRouteLayer();
            UpdateVisualRotation();
            SyncRouteToGlobal();
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
        HandleDangerTimer();
        HandleRunwayOccupancyAbort();
        HandleTakeoff();
        HandleFuelConsumption();
        HandleFuelEmergency();
        HandleLowFuelWarning();
        HandleStormDetection();
        HandleMovement();
        HandlePing();
        FadeOut();
        if (transform.parent != null) CheckZoomVisibility(transform.parent.localScale.x);
        HandleDespawnCheck();
    }

    // ── Уменьшаем таймер опасности каждый кадр ──────────────────────────────
    private void HandleDangerTimer()
    {
        if (dangerTimer > 0f) dangerTimer -= Time.deltaTime;
    }

    // ── Прерываем посадку, если ВПП заняли другим бортом ────────────────────
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

    // ── Проверяем, улетел ли борт достаточно далеко от ВПП взлёта ───────────
    private void HandleTakeoff()
    {
        if (!isTakingOff) return;

        // Восстанавливаем начальную позицию взлёта, если она была потеряна
        if (takeoffStartPos == Vector2.zero && !string.IsNullOrEmpty(assignedRunway) && RunwayManager.Instance != null)
        {
            Runway rw = RunwayManager.Instance.GetRunwayByID(assignedRunway);
            if (rw != null)
            {
                RectTransform rwRect = rw.GetComponent<RectTransform>();
                if (rwRect != null)
                {
                    takeoffStartPos = rwRect.anchoredPosition;
                    // Смещаем на микро-значение, чтобы не попасть в ноль повторно
                    if (takeoffStartPos == Vector2.zero) takeoffStartPos = new Vector2(0.001f, 0.001f);
                }
            }
        }

        float dist = takeoffStartPos != Vector2.zero ? Vector2.Distance(logicalPosition, takeoffStartPos) : 0f;

        // Борт считается взлетевшим, когда отлетел более чем на 150 единиц
        if (takeoffStartPos != Vector2.zero && dist > 150f)
        {
            isTakingOff = false;
            SetCollidersActive(true);
            if (callsignText != null) callsignText.gameObject.SetActive(true);
            foreach (var marker in activeMarkers) if (marker != null) marker.SetActive(true);
            if (hitboxVisual != null) hitboxVisual.gameObject.SetActive(true);
            RebuildRouteLayer();

            if (FlightDataManager.Instance != null)
                FlightDataManager.Instance.FreeBaseSlot(originalCallsign);
        }
    }

    // ── Расходуем топливо пропорционально пройденному расстоянию ────────────
    private void HandleFuelConsumption()
    {
        float distanceMoved = Vector2.Distance(logicalPosition, lastPosition);
        lastPosition = logicalPosition;

        if (isOutOfFuel || distanceMoved <= 0) return;

        float fuelConsumed = distanceMoved / distancePerFuelUnit;
        currentFuel -= fuelConsumed;

        if (currentFuel <= 0)
        {
            currentFuel = 0;
            isOutOfFuel = true;
            _actualSpeed *= 0.3f;
            UpdateHitboxColor();
        }
    }

    // ── Отсчёт катастрофы при полном израсходовании топлива ─────────────────
    private void HandleFuelEmergency()
    {
        if (!isOutOfFuel) return;

        emergencyTimer -= Time.deltaTime;

        // Мигающий MAYDAY
        string targetText = (Mathf.FloorToInt(Time.time * 3) % 2 == 0) ? "MAYDAY" : "";
        if (callsignText.text != targetText) callsignText.text = targetText;

        if (emergencyTimer > 0) return;

        // Время вышло — борт потерян
        if (FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == originalCallsign);
            if (fd != null && AirplaneSpawner.Instance != null)
                AirplaneSpawner.Instance.NotifyPlaneCrashed(fd);
            FlightDataManager.Instance.RemoveDepartedPlane(originalCallsign);
        }

        if (RadarScreenClicker.selectedPlane == this)
            if (BigRadarTerminal.Instance != null) BigRadarTerminal.Instance.ClearSelection();

        if (AirplaneSpawner.Instance != null)
            AirplaneSpawner.Instance.ReturnPlaneToPool(this);
        else
            Destroy(gameObject);
    }

    // ── Моргаем красным при критически низком топливе ───────────────────────
    private void HandleLowFuelWarning()
    {
        if (isOutOfFuel || currentFuel > 30f || currentFuel <= 0f) return;
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

    // ── Определяем, попал ли борт в шторм, и реагируем ──────────────────────
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
            UpdateHitboxColor();
        }
        else if (!currentlyInStorm && inStorm)
        {
            inStorm = false;
            if (!isOutOfFuel) callsignText.text = realCallsign;
            UpdateHitboxColor();
        }
    }

    // ── Двигаем борт: holding pattern или по waypoints ───────────────────────
    private void HandleMovement()
    {
        float currentSpeed = inStorm ? (_actualSpeed * 0.5f) : _actualSpeed;

        if (isHolding)
        {
            float angularSpeed = (currentSpeed / holdingRadius) * Mathf.Rad2Deg;
            currentHoldingAngle += angularSpeed * Time.deltaTime;
            Vector2 circleTarget = holdingCenter + new Vector2(
                Mathf.Cos(currentHoldingAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentHoldingAngle * Mathf.Deg2Rad)) * holdingRadius;
            logicalPosition = Vector2.MoveTowards(logicalPosition, circleTarget, currentSpeed * Time.deltaTime);
            return;
        }

        if (waypoints.Count == 0) return;

        Vector2 currentTarget = waypoints[0];

        // Входим в holding pattern у центра радара, если решение ещё не принято
        bool isWaitingForRunway = dispatchStatus == DispatchStatus.Approved && string.IsNullOrEmpty(assignedRunway);
        if (waypoints.Count == 1 && (dispatchStatus == DispatchStatus.Pending || isWaitingForRunway) && currentTarget == Vector2.zero)
        {
            if (Vector2.Distance(logicalPosition, currentTarget) <= holdingRadius)
            {
                if (!isOutOfFuel) StartHolding(currentTarget);
                return;
            }
        }

        logicalPosition = Vector2.MoveTowards(logicalPosition, currentTarget, currentSpeed * Time.deltaTime);

        if (Vector2.Distance(logicalPosition, currentTarget) >= 5f) return;

        // Достигли текущей точки маршрута
        if (waypoints.Count > 1)
        {
            waypoints.RemoveAt(0);
            RebuildRouteLayer();
            return;
        }

        // Достигли ПОСЛЕДНЕЙ точки маршрута
        HandleWaypointReached();
    }

    // ── Логика достижения последней точки (посадка или деспавн) ─────────────
    private void HandleWaypointReached()
    {
        // Случай A: это была точка выравнивания перед ВПП
        if (isAligningToLand)
        {
            isAligningToLand = false;
            isLandingPhase = true;
            waypoints.Clear();
            SetVisualState(false, 0.2f);
            SetCollidersActive(false);

            // Теперь летим прямо на ВПП
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

        // Случай B: достигли реальной ВПП — проверяем посадку
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
            if (FlightDataManager.Instance != null) FlightDataManager.Instance.MarkFlightAsLanded(realCallsign);
            if (VideoLandingManager.Instance != null) VideoLandingManager.Instance.RequestLandingVideo();
            if (RunwayManager.Instance != null) RunwayManager.Instance.OccupyRunway(assignedRunway, 15f);
            AirplaneSpawner.Instance.ReturnPlaneToPool(this);
        }
        else
        {
            // Иных условий нет — деспавним
            AirplaneSpawner.Instance.ReturnPlaneToPool(this);
        }
    }

    // ── Деспавн при выходе за границу радара ─────────────────────────────────
    private void HandleDespawnCheck()
    {
        if (Vector2.Distance(Vector2.zero, logicalPosition) <= despawnRadius) return;

        if (FlightDataManager.Instance != null)
        {
            var flight = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == originalCallsign);
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

    private void StartHolding(Vector2 center)
    {
        isHolding = true;
        holdingCenter = center;

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
            UpdateVisualRotation();
            UpdateHitboxColor();
            // Во время взлёта альфу не сбрасываем в 1 — самолет должен оставаться тусклым
            if (canvasGroup != null) canvasGroup.alpha = (isLandingPhase || isTakingOff) ? 0.3f : 1f;

            // Линия маршрута обновляется ТОЛЬКО здесь — синхронно с прыжком самолета
            if (!isTakingOff && !isLandingPhase && lineSegments.Count > 0 && !isHolding)
                UpdateFirstSegment();

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
        if (isLandingPhase || isTakingOff)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0.3f;
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

    public void UpdateInternalSpeed() => _actualSpeed = speed / 29f;

    private void CheckZoomVisibility(float zoom)
    {
        bool show = zoom >= showTextZoomThreshold;
        if (callsignText.gameObject.activeSelf != show) callsignText.gameObject.SetActive(show);
    }

    /// <summary>
    /// Фиксирует абсолютный бюджет дальности от текущей позиции самолёта.
    /// Вызывается при спавне, загрузке из сейва и перестройке маршрута.
    /// Маркер зелёной/красной зоны привязан к этому бюджету и НЕ двигается в полёте.
    /// </summary>
    private void RecalcFuelRange()
    {
        routeOriginPosition = logicalPosition;
        fuelRangeFromRouteOrigin = currentFuel * distancePerFuelUnit;
    }

    private void RebuildRouteLayer()
    {
        // Пересчитываем бюджет дальности от текущей позиции при перестройке маршрута
        RecalcFuelRange();

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
        if (waypoints.Count == 0) return;

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
        RebuildRouteLayer();
        UpdateHitboxColor();
        SyncRouteToGlobal();
    }

    public void SetHighlight(bool h)
    {
        isSelected = h;
        UpdateHitboxColor();
    }

    private void PlayAirplaneClickSound()
    {
        if (ButtonSoundManager.instance != null && airplaneClickSound != null)
        {
            ButtonSoundManager.instance.PlaySpecialSound(airplaneClickSound, ButtonSoundManager.instance.volume * airplaneClickVolume);
        }
        else if (airplaneClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(airplaneClickSound, airplaneClickVolume);
        }
        else if (ButtonSoundManager.instance != null)
        {
            ButtonSoundManager.instance.PlayDefaultClick();
        }
    }

    public void TriggerSelection()
    {
        if (inStorm) return;
        PlayAirplaneClickSound();

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
        if (AirplaneSpawner.Instance != null && FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == originalCallsign);
            if (fd != null) 
            {
                AirplaneSpawner.Instance.NotifyPlaneCrashed(fd);
            }
            // Удаляем самолет из списков менеджера (чтобы он пропал из терминала и не занимал место)
            FlightDataManager.Instance.RemoveDepartedPlane(originalCallsign);
        }

        // Note: Emergency collision logic removed from tutorial.
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

    /// <summary>
    /// Уничтожает все визуальные элементы маршрута (сегменты линий и маркеры вейпоинтов).
    /// Вызывается при возврате самолёта в пул и при уничтожении объекта.
    /// </summary>
    public void CleanupRouteVisuals()
    {
        if (lineSegments != null)
        {
            foreach (GameObject seg in lineSegments) if (seg != null) Destroy(seg);
            lineSegments.Clear();
        }

        if (activeMarkers != null)
        {
            foreach (GameObject marker in activeMarkers) if (marker != null) Destroy(marker);
            activeMarkers.Clear();
        }
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
            dangerTimer = 2f; // Держим предупреждение минимум 2 секунды
            if (!isInDanger)
            {
                isInDanger = true;
                UpdateHitboxColor();
            }
        }
        else
        {
            if (isInDanger && dangerTimer <= 0f)
            {
                isInDanger = false;
                UpdateHitboxColor();
            }
        }
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

        // Вычисляем сколько расстояния самолет уже пролетел от routeOriginPosition
        // и вычитаем из абсолютного бюджета — это оставшаяся зеленая дальность от текущей позиции
        float distFromOriginToPlane = Vector2.Distance(routeOriginPosition, rectTransform.anchoredPosition);
        float maxFlightDistance = Mathf.Max(0f, fuelRangeFromRouteOrigin - distFromOriginToPlane);
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

    public void SyncFromBigRadar(UIAirplane bigRadarPlane)
    {
        waypoints = new List<Vector2>(bigRadarPlane.waypoints);
        dispatchStatus = bigRadarPlane.dispatchStatus;
        isHolding = bigRadarPlane.isHolding;
        assignedRunway = bigRadarPlane.assignedRunway;
        isAligningToLand = bigRadarPlane.isAligningToLand;
        RebuildRouteLayer();
        UpdateVisualRotation();
    }

    private void SyncRouteToGlobal()
    {
        if (FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == originalCallsign);
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
