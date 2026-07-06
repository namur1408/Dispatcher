using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RadarManager : SingletonMB<RadarManager>
{
    public Transform listContainer;
    public GameObject entryPrefab;

    public List<UIAirplane> activeAirplanes = new List<UIAirplane>();

    private float conflictCheckTimer = 0f;
    private const float CONFLICT_CHECK_INTERVAL = 0.15f;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
    }

    void Update()
    {
        conflictCheckTimer -= Time.deltaTime;
        if (conflictCheckTimer <= 0f)
        {
            conflictCheckTimer = CONFLICT_CHECK_INTERVAL;
            CheckForConflicts();
        }
    }

    private void CheckForConflicts()
    {
        bool anyWarning = false;
        float warningDistanceSq = FlightConstants.ConflictWarningDistance * FlightConstants.ConflictWarningDistance;

        int count = activeAirplanes.Count;
        if (count == 0) return;

        // Compute positions once
        Vector2[] positions = new Vector2[count];
        for (int i = 0; i < count; i++)
            if (activeAirplanes[i] != null) positions[i] = activeAirplanes[i].GetLogicalPosition();

        // Compute final warning states FIRST, then apply
        // This prevents the false->true double-color-update that causes flickering
        bool[] newWarnings = new bool[count];

        for (int i = 0; i < count; i++)
        {
            if (activeAirplanes[i] == null || activeAirplanes[i].isLandingPhase || activeAirplanes[i].isTakingOff) continue;
            for (int j = i + 1; j < count; j++)
            {
                if (activeAirplanes[j] == null || activeAirplanes[j].isLandingPhase || activeAirplanes[j].isTakingOff) continue;
                float dx = positions[i].x - positions[j].x;
                float dy = positions[i].y - positions[j].y;
                float distSq = dx * dx + dy * dy;

                // Hysteresis to prevent flickering at the boundary
                bool currentlyInDanger = activeAirplanes[i].isInDanger || activeAirplanes[j].isInDanger;
                float hyst = FlightConstants.ConflictHysteresisDistance;
                float thresholdSq = currentlyInDanger ? (hyst * hyst) : warningDistanceSq;

                if (distSq < thresholdSq)
                {
                    newWarnings[i] = true;
                    newWarnings[j] = true;
                    anyWarning = true;
                }
            }
        }

        // Apply - SetWarning skips UpdateHitboxColor if state unchanged
        for (int i = 0; i < count; i++)
            if (activeAirplanes[i] != null) activeAirplanes[i].SetWarning(newWarnings[i]);

        // Critical fuel check
        if (!anyWarning)
        {
            for (int i = 0; i < count; i++)
            {
                var plane = activeAirplanes[i];
                if (plane == null || plane.isLandingPhase || plane.isTakingOff || plane.isOutOfFuel) continue;
                if (plane.currentFuel > 0f && plane.currentFuel <= FlightConstants.LowFuelThreshold)
                {
                    anyWarning = true;
                    break;
                }
            }
        }

        BigRadarLoader.isGlobalWarningActive = anyWarning;
    }

    /// <summary>
    /// Analogue of BigRadarLoader.RebuildAll() - creates a UIAirplane from FlightDataManager
    /// and spawns planes that were in the air.
    /// </summary>
    public void RebuildFromFlightData()
    {
        if (FlightDataManager.Instance == null) return;

        // Removing current visual objects from the radar
        for (int i = activeAirplanes.Count - 1; i >= 0; i--)
        {
            if (activeAirplanes[i] != null) AirplaneSpawner.Instance.ReturnPlaneToPool(activeAirplanes[i]);
        }
        activeAirplanes.Clear();

        // Restoring from FlightDataManager - just like BigRadarLoader does
        foreach (var data in FlightDataManager.Instance.savedFlights)
        {
            if (data.hasLanded || data.isReadyToDepart) continue;
            SpawnAndRegister(data);
        }

        StartCoroutine(ApplyDecisionsNextFrame());
    }

    private void SpawnAndRegister(FlightData data)
    {
        AirplaneSpawner spawner = AirplaneSpawner.Instance;
        if (spawner == null) spawner = FindFirstObjectByType<AirplaneSpawner>();
        if (spawner == null) return;

        UIAirplane planeScript = spawner.GetPlaneFromPool(spawner.radarContent);
        if (planeScript != null)
        {
            planeScript.InitializeFromData(data);
            RegisterAirplane(planeScript);
        }
    }

    IEnumerator ApplyDecisionsNextFrame()
    {
        yield return null;

        foreach (var flight in FlightDataManager.Instance.savedFlights)
        {
            if (!flight.decisionMade) continue;
            UIAirplane target = activeAirplanes.Find(p =>
                p != null && p.callsignText != null && p.callsignText.text == flight.callsign);

            if (target == null) continue;

            if (flight.approved) target.Approve();
            else target.Deny();
        }
    }

    public void RegisterAirplane(UIAirplane airplane)
    {
        if (activeAirplanes.Contains(airplane)) return;

        activeAirplanes.Add(airplane);

        if (listContainer != null && entryPrefab != null)
        {
            GameObject entry = Instantiate(entryPrefab, listContainer);
            FlightListEntry entryScript = entry.GetComponent<FlightListEntry>();
            if (entryScript != null) entryScript.Setup(airplane);
        }
    }

    public void UnregisterAirplane(UIAirplane airplane)
    {
        activeAirplanes.Remove(airplane);
    }

    public void SaveToGlobalManager()
    {
        if (FlightDataManager.Instance != null)
            FlightDataManager.Instance.UpdateFlights(activeAirplanes);
    }

    public void SelectAirplane(UIAirplane selectedPlane)
    {
        foreach (var plane in activeAirplanes)
            plane.SetHighlight(plane == selectedPlane);
    }

    public int GetPlanesCount() => activeAirplanes.Count;

    public void SpawnDepartingNow(FlightData data)
    {
        AirplaneSpawner spawner = AirplaneSpawner.Instance;
        if (spawner == null) spawner = FindFirstObjectByType<AirplaneSpawner>();
        if (spawner == null) return;

        data.isReadyToDepart = false;
        data.isDeparting = true;
        data.hasBeenPinged = true;

        // Airplanes from saves could have speed 0 - set to default
        if (data.speed <= 0f) data.speed = 1f;

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
        data.savedWaypoints.Clear();

        UIAirplane planeScript = spawner.GetPlaneFromPool(spawner.radarContent);
        if (planeScript != null)
        {
            planeScript.InitializeFromData(data);
            RegisterAirplane(planeScript);

            if (!string.IsNullOrEmpty(data.assignedRunway))
            {
                planeScript.SetAssignedRunway(data.assignedRunway, false);
                data.savedWaypoints  = planeScript.GetWaypoints();
                data.isTakingOff     = planeScript.isTakingOff;
                data.takeoffStartPos = planeScript.takeoffStartPos;
            }
            else
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                planeScript.SetFlightPath(Vector2.zero,
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * planeScript.despawnRadius);
                data.savedWaypoints = planeScript.GetWaypoints();
            }
        }
    }
}