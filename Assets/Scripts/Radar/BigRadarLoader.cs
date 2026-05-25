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
            FlightDataManager.Instance.UpdateFlights(activePlanes);
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

        CheckForConflicts();
    }

    // ── Sync ─────────────────────────────────────────────────────────────────

    void SyncWithFlightDataManager()
    {
        var savedFlights = FlightDataManager.Instance.savedFlights;

        // 1. Add planes that are in FlightDataManager but not on big radar yet
        foreach (var data in savedFlights)
        {
            if (data.hasLanded || data.isReadyToDepart) continue;

            if (!planeMap.ContainsKey(data.callsign))
            {
                SpawnPlane(data);
            }
        }

        // 2. Remove planes from big radar that are no longer active in FlightDataManager
        List<string> toRemove = new List<string>();
        foreach (var kv in planeMap)
        {
            if (kv.Value == null) { toRemove.Add(kv.Key); continue; }

            var fd = savedFlights.Find(f => f.callsign == kv.Key);
            if (fd == null || fd.hasLanded)
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
        if (airplanePrefab == null || radarContent == null) return;

        GameObject go = Instantiate(airplanePrefab, radarContent, false);
        UIAirplane plane = go.GetComponent<UIAirplane>();
        if (plane == null) { Destroy(go); return; }

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
            if (data.hasLanded || data.isReadyToDepart) continue;
            SpawnPlane(data);
        }
    }

    void ClearAll()
    {
        foreach (var kv in planeMap)
            if (kv.Value != null) Destroy(kv.Value.gameObject);

        planeMap.Clear();
        activePlanes.Clear();
    }

    // ── Conflict detection ────────────────────────────────────────────────────

    private void CheckForConflicts()
    {
        bool anyWarning = false;

        foreach (var plane in activePlanes)
            if (plane != null) plane.SetWarning(false);

        for (int i = 0; i < activePlanes.Count; i++)
        {
            for (int j = i + 1; j < activePlanes.Count; j++)
            {
                UIAirplane a = activePlanes[i];
                UIAirplane b = activePlanes[j];
                if (a == null || b == null) continue;

                float dist = Vector2.Distance(
                    a.GetComponent<RectTransform>().anchoredPosition,
                    b.GetComponent<RectTransform>().anchoredPosition);

                if (dist < warningDistance)
                {
                    a.SetWarning(true);
                    b.SetWarning(true);
                    anyWarning = true;
                }
            }
        }

        isGlobalWarningActive = anyWarning;
    }

    // ── Departures (called from RadarPanelsManager) ───────────────────────────

    public void SpawnDepartingNow(FlightData data)
    {
        if (airplanePrefab == null || radarContent == null) return;

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
                RadarManager.Instance.RegisterAirplane(plane);
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