using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages airplane icons on the BIG radar screen.
/// Stays synced with FlightDataManager every frame — no data copying needed.
/// Each radar (small & big) has its own UIAirplane objects.
/// </summary>
public class BigRadarLoader : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject airplanePrefab;
    public Transform radarContent;
    public string mainSceneName = "SampleScene";

    [Header("Conflict Alert Settings")]
    public float warningDistance = 125f;

    // Our own list of plane icons on the big radar
    private List<UIAirplane> activePlanes = new List<UIAirplane>();
    // Map callsign → our UIAirplane for fast lookup
    private Dictionary<string, UIAirplane> planeMap = new Dictionary<string, UIAirplane>();

    public static bool isGlobalWarningActive = false;

    private float conflictCheckTimer = 0f;
    private const float CONFLICT_CHECK_INTERVAL = 0.15f;

    // Pool to avoid Destroy/Instantiate on every canvas switch
    private List<UIAirplane> planePool = new List<UIAirplane>();

    void OnEnable()
    {
        // Full rebuild when screen opens
        RebuildAll();
    }

    void OnDisable()
    {
        // Save big radar state first so we have fresh positions for the small radar
        if (FlightDataManager.Instance != null && activePlanes != null && activePlanes.Count > 0)
        {
            FlightDataManager.Instance.UpdateFlights(activePlanes, false);
            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.RebuildFromFlightData();
            }
        }

        // Clear our visual icons when screen closes (they'll be rebuilt on next open)
        ClearAll();
    }

    void Update()
    {
        if (FlightDataManager.Instance == null || radarContent == null) return;

        SyncWithFlightDataManager();

        if (BigRadarTerminal.Instance != null)
            BigRadarTerminal.Instance.SetPlaneCount(activePlanes.Count);

        conflictCheckTimer -= Time.deltaTime;
        if (conflictCheckTimer <= 0f)
        {
            conflictCheckTimer = CONFLICT_CHECK_INTERVAL;
            CheckForConflicts();
        }
    }

    // ── Sync ─────────────────────────────────────────────────────────────────

    void SyncWithFlightDataManager()
    {
        var savedFlights = FlightDataManager.Instance.savedFlights;

        // 1. Add planes that are in FlightDataManager but not on big radar yet.
        // Показываем самолеты которые: ещё не сели ИЛИ уже вылетают (isDeparting).
        foreach (var data in savedFlights)
        {
            bool shouldShow = (!data.hasLanded || data.isDeparting) && !data.isReadyToDepart;
            if (!shouldShow) continue;

            if (!planeMap.ContainsKey(data.callsign))
            {
                SpawnPlane(data);
            }
        }

        // 2. Remove planes from big radar that are no longer active.
        List<string> toRemove = new List<string>();
        foreach (var kv in planeMap)
        {
            if (kv.Value == null) { toRemove.Add(kv.Key); continue; }

            var fd = savedFlights.Find(f => f.callsign == kv.Key);
            // Удаляем только если рейс вообще исчез ИЛИ сел и НЕ вылетает
            if (fd == null || (fd.hasLanded && !fd.isDeparting))
                toRemove.Add(kv.Key);
        }
        foreach (var key in toRemove)
        {
            if (planeMap.TryGetValue(key, out var plane) && plane != null)
                Destroy(plane.gameObject);
            planeMap.Remove(key);
        }

        // Rebuild activePlanes list (remove nulls)
        activePlanes.RemoveAll(p => p == null);
    }

    void SpawnPlane(FlightData data)
    {
        if (radarContent == null) return;

        UIAirplane plane = GetFromPool(radarContent);
        if (plane == null) return;

        plane.isBigRadarCopy = true;
        plane.InitializeFromData(data);

        activePlanes.Add(plane);
        planeMap[data.callsign] = plane;
    }

    // ── Full rebuild / clear ──────────────────────────────────────────────────

    void RebuildAll()
    {
        ClearAll();
        if (FlightDataManager.Instance == null) return;

        // Save small radar state first so we have fresh positions
        if (RadarManager.Instance != null)
            RadarManager.Instance.SaveToGlobalManager();

        foreach (var data in FlightDataManager.Instance.savedFlights)
        {
            bool shouldShow = (!data.hasLanded || data.isDeparting) && !data.isReadyToDepart;
            if (!shouldShow) continue;
            SpawnPlane(data);
        }
    }

    void ClearAll()
    {
        // Deactivate into pool instead of Destroy — eliminates the freeze on canvas switch
        foreach (var kv in planeMap)
        {
            if (kv.Value != null)
            {
                kv.Value.gameObject.SetActive(false);
                if (!planePool.Contains(kv.Value))
                    planePool.Add(kv.Value);
            }
        }
        planeMap.Clear();
        activePlanes.Clear();
    }

    private UIAirplane GetFromPool(Transform parent)
    {
        for (int i = 0; i < planePool.Count; i++)
        {
            if (planePool[i] != null && !planePool[i].gameObject.activeSelf)
            {
                planePool[i].transform.SetParent(parent, false);
                planePool[i].gameObject.SetActive(true);
                planePool[i].ResetPlane();
                return planePool[i];
            }
        }
        // Only Instantiate if pool is exhausted
        if (airplanePrefab == null) return null;
        GameObject go = Instantiate(airplanePrefab, parent, false);
        UIAirplane plane = go.GetComponent<UIAirplane>();
        if (plane != null) planePool.Add(plane);
        return plane;
    }

    // ── Conflict detection ────────────────────────────────────────────────────

    private void CheckForConflicts()
    {
        bool anyWarning = false;
        float warningDistanceSq = warningDistance * warningDistance;

        int count = activePlanes.Count;
        if (count == 0) return;

        // Compute positions once
        Vector2[] positions = new Vector2[count];
        for (int i = 0; i < count; i++)
            if (activePlanes[i] != null) positions[i] = activePlanes[i].GetLogicalPosition();

        // Compute final warning states FIRST, then apply — avoids double color update (flicker)
        bool[] newWarnings = new bool[count];

        for (int i = 0; i < count; i++)
        {
            if (activePlanes[i] == null || activePlanes[i].isLandingPhase || activePlanes[i].isTakingOff) continue;
            for (int j = i + 1; j < count; j++)
            {
                if (activePlanes[j] == null || activePlanes[j].isLandingPhase || activePlanes[j].isTakingOff) continue;
                float dx = positions[i].x - positions[j].x;
                float dy = positions[i].y - positions[j].y;
                float distSq = dx * dx + dy * dy;

                // Hysteresis to prevent flickering at the boundary
                bool currentlyInDanger = activePlanes[i].isInDanger || activePlanes[j].isInDanger;
                float thresholdSq = currentlyInDanger ? (135f * 135f) : warningDistanceSq;

                if (distSq < thresholdSq)
                {
                    newWarnings[i] = true;
                    newWarnings[j] = true;
                    anyWarning = true;
                }
            }
        }

        // Apply — SetWarning skips UpdateHitboxColor if state unchanged
        for (int i = 0; i < count; i++)
            if (activePlanes[i] != null) activePlanes[i].SetWarning(newWarnings[i]);

        isGlobalWarningActive = anyWarning;
    }

    // ── Departures (called from RadarPanelsManager) ───────────────────────────

    public void SpawnDepartingNow(FlightData data)
    {
        if (radarContent == null) return;

        data.isReadyToDepart = false;
        data.isDeparting = true;

        Vector2 spawnPos = Vector2.zero;
        if (!string.IsNullOrEmpty(data.assignedRunway) && RunwayManager.Instance != null)
        {
            Runway rw = RunwayManager.Instance.GetRunwayByID(data.assignedRunway);
            if (rw != null)
            {
                RectTransform rt = rw.GetComponent<RectTransform>();
                if (rt != null) spawnPos = rt.anchoredPosition;
            }
        }
        data.position = spawnPos;

        SpawnPlane(data);
        UIAirplane plane = planeMap.ContainsKey(data.callsign) ? planeMap[data.callsign] : null;
        if (plane != null)
        {
            if (!string.IsNullOrEmpty(data.assignedRunway))
                plane.SetAssignedRunway(data.assignedRunway, false);
            else
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                plane.SetFlightPath(Vector2.zero,
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * plane.despawnRadius);
            }

            if (RadarManager.Instance != null)
                RadarManager.Instance.UnregisterAirplane(plane);
        }
    }

    // ── Return to desk ────────────────────────────────────────────────────────

    [Header("Single Scene Return Mode")]
    public Camera returnCamera;
    public GameObject returnScreenRoot;
    public GameObject currentScreenRoot;

    public void SaveAndReturnToDesk()
    {
        Time.timeScale = 1f;
        if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();

        if (returnCamera != null || returnScreenRoot != null)
        {
            if (returnScreenRoot != null)
            {
                returnScreenRoot.SetActive(true);
                CanvasGroup cg = returnScreenRoot.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
                UnityEngine.UI.GraphicRaycaster[] grs = returnScreenRoot.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true);
                foreach (var gr in grs) gr.enabled = true;
            }
            if (returnCamera != null)
            {
                returnCamera.gameObject.SetActive(true);
            }
            if (currentScreenRoot != null) currentScreenRoot.SetActive(false);

            ZoomReturnManager zrm = FindAnyObjectByType<ZoomReturnManager>();
            if (zrm != null) zrm.TriggerReturnAnimation();
        }
        else
        {
            SceneManager.LoadScene(mainSceneName);
        }
    }

    // Legacy alias
    public void RestoreFlights() => RebuildAll();

    void OnDestroy() { Time.timeScale = 1f; }
}