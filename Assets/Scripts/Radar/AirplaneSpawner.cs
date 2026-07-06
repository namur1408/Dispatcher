using UnityEngine;
using System.Collections.Generic;

public class AirplaneSpawner : SingletonMB<AirplaneSpawner>
{
    [Header("Settings")]
    public GameObject airplanePrefab;
    public Transform radarContent;
    public int maxAirplanes = 5;

    public float minSpawnTime = 35f;
    public float maxSpawnTime = 60f;
    public float spawnRadius = 640f;

    [Range(0f, 1f)]
    public float landingProbability = 0.5f;

    [Header("Collision Detection")]
    public float minSpawnDistance = 200f;
    private bool storyPlaneCrashed = false;

    private List<UIAirplane> planePool = new List<UIAirplane>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
    }

    public UIAirplane GetPlaneFromPool(Transform parent)
    {
        // Try to find an inactive plane first
        foreach (var plane in planePool)
        {
            if (plane != null && !plane.gameObject.activeInHierarchy)
            {
                plane.transform.SetParent(parent, false);
                plane.gameObject.SetActive(true);
                plane.ResetPlane();
                return plane;
            }
        }

        // Spawn a new plane if pool is exhausted
        GameObject newObj = Instantiate(airplanePrefab, parent, false);
        UIAirplane newPlane = newObj.GetComponent<UIAirplane>();
        if (newPlane != null)
        {
            planePool.Add(newPlane);
        }
        return newPlane;
    }

    public void ReturnPlaneToPool(UIAirplane plane)
    {
        if (plane != null)
        {
            // We clean the visual objects of the route before deactivating,
            // so as not to remain dangling on the radar after a scene change.
            plane.CleanupRouteVisuals();
            plane.gameObject.SetActive(false);
            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.UnregisterAirplane(plane);
            }
        }
    }

    void Update()
    {
        if (FlightDataManager.Instance == null || !FlightDataManager.Instance.isShiftActive) return;

        if (FlightDataManager.Instance.landedPlanes >= FlightDataManager.Instance.maxPlanes) return;

        FlightDataManager.Instance.globalSpawnTimer -= Time.deltaTime;

        if (FlightDataManager.Instance.globalSpawnTimer <= 0)
        {
            Transform currentContent = GetActiveRadarContent();
            if (currentContent == null) return;

            int currentCount = GetCurrentPlanesCount(currentContent);

            if (currentCount < maxAirplanes)
            {
                if (FlightDataManager.Instance.scriptedFlightsQueue.Count > 0)
                {
                    FlightData data = FlightDataManager.Instance.scriptedFlightsQueue.Dequeue();

                    float nextDelay = 5f;
                    if (FlightDataManager.Instance.scriptedDelaysQueue.Count > 0)
                    {
                        nextDelay = FlightDataManager.Instance.scriptedDelaysQueue.Dequeue();
                    }

                    SpawnStoryPlane(data, currentContent);

                    FlightDataManager.Instance.globalSpawnTimer = nextDelay;
                }
                else
                {
                    SpawnRandomAirplane(currentContent, storyPlaneCrashed);
                    FlightDataManager.Instance.globalSpawnTimer = Random.Range(minSpawnTime, maxSpawnTime);
                }
            }
            else
            {
                FlightDataManager.Instance.globalSpawnTimer = 3f;
            }
        }
    }

    void SpawnStoryPlane(FlightData data, Transform contentParent)
    {
        Vector2 startPos  = data.position;
        Vector2 targetPos = data.targetPosition;

        if (IsPositionOccupied(startPos, contentParent))
        {
            startPos = FindSafeSpawnPosition(startPos, contentParent);
        }

        UIAirplane planeScript = GetPlaneFromPool(contentParent);

        if (planeScript != null)
        {
            data.position = startPos;
            planeScript.InitializeFromData(data);
            planeScript.SetFlightPath(startPos, targetPos);
            if (FlightDataManager.Instance != null && !FlightDataManager.Instance.savedFlights.Contains(data))
            {
                FlightDataManager.Instance.savedFlights.Add(data);
            }

            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.RegisterAirplane(planeScript);
            }
        }
    }

    void SpawnRandomAirplane(Transform contentParent, bool onlyLanding = false)
    {
        Vector2 startPos  = GetRandomSpawnPosition(contentParent);
        Vector2 targetPos = Vector2.zero;

        if (!onlyLanding && Random.value >= landingProbability)
        {
            float angle    = Mathf.Atan2(startPos.y, startPos.x);
            float endAngle = angle + Random.Range(120f, 240f) * Mathf.Deg2Rad;
            targetPos = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * (spawnRadius + 200f);
        }

        string[] prefixes = { "GE", "TR", "QY" };
        string prefix    = prefixes[Random.Range(0, prefixes.Length)];
        string callsign  = $"{prefix}-{Random.Range(100, 999)}";

        float  speed  = 80f;
        string cargo  = "None";
        int    amount = 10;

        string[] goods = { "Food", "Fuel", "Medicines" };

        if (prefix == "GE")
        {
            speed  = Random.Range(60, 84);
            cargo  = goods[Random.Range(0, goods.Length)];
            amount = Random.Range(51, 500);
        }
        else if (prefix == "TR")
        {
            speed  = Random.Range(70, 78);
            cargo  = "People";
            amount = Random.Range(20, 250);
        }
        else if (prefix == "QY")
        {
            speed  = Random.Range(81, 105);
            cargo  = goods[Random.Range(0, goods.Length)];
            amount = Random.Range(1, 50);
        }

        // UIAirplane burns 1 unit of fuel for every FuelPerDistanceUnit units of distance.
        // We consider the minimum fuel for the route + a large safety margin.
        const float SAFETY_MULTIPLIER_MIN = 2.5f;
        const float SAFETY_MULTIPLIER_MAX = 3.5f;

        float routeDistance = targetPos == Vector2.zero
            ? startPos.magnitude                               // Landing: start → center (0,0)
            : Vector2.Distance(startPos, targetPos);          // Transit: start → goal

        float minFuelRequired  = routeDistance / FlightConstants.FuelPerDistanceUnit;
        float safetyMultiplier = Random.Range(SAFETY_MULTIPLIER_MIN, SAFETY_MULTIPLIER_MAX);
        float fuel             = Mathf.Clamp(minFuelRequired * safetyMultiplier, 120f, 600f);

        FlightData randomData = new FlightData(callsign, startPos, targetPos, new List<Vector2>(), speed, cargo, amount);
        randomData.currentFuel  = fuel;
        randomData.personality  = PilotDialogue.GetRandomPersonality();

        UIAirplane planeScript = GetPlaneFromPool(contentParent);

        if (planeScript != null)
        {
            planeScript.InitializeFromData(randomData);
            planeScript.SetFlightPath(startPos, targetPos);

            if (FlightDataManager.Instance != null && !FlightDataManager.Instance.savedFlights.Contains(randomData))
            {
                FlightDataManager.Instance.savedFlights.Add(randomData);
            }

            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.RegisterAirplane(planeScript);
            }
        }
    }

    Transform GetActiveRadarContent()
    {
        return radarContent;
    }

    int GetCurrentPlanesCount(Transform contentParent)
    {
        if (RadarManager.Instance != null) return RadarManager.Instance.GetPlanesCount();
        return contentParent.GetComponentsInChildren<UIAirplane>().Length;
    }

    bool IsPositionOccupied(Vector2 position, Transform contentParent)
    {
        UIAirplane[] existingPlanes = contentParent.GetComponentsInChildren<UIAirplane>();
        foreach (UIAirplane plane in existingPlanes)
        {
            if (Vector2.Distance(plane.GetLogicalPosition(), position) < minSpawnDistance)
            {
                return true;
            }
        }
        return false;
    }

    Vector2 FindSafeSpawnPosition(Vector2 originalPosition, Transform contentParent)
    {
        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 newPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;

            if (!IsPositionOccupied(newPos, contentParent))
            {
                return newPos;
            }
        }
        return originalPosition;
    }

    Vector2 GetRandomSpawnPosition(Transform contentParent)
    {
        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            float angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;

            if (!IsPositionOccupied(position, contentParent))
            {
                return position;
            }
        }

        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * spawnRadius;
    }

    public void RespawnLoadedPlanes(List<FlightData> loadedFlights)
    {
        Transform currentContent = GetActiveRadarContent();
        if (currentContent == null) return;

        foreach (var data in loadedFlights)
        {
            // We skip those who have already landed and are ready to take off - they are in Departures, not on the radar
            if (data.hasLanded || data.isReadyToDepart) continue;

            UIAirplane planeScript = GetPlaneFromPool(currentContent);

            if (planeScript != null)
            {
                planeScript.InitializeFromData(data);

                if (RadarManager.Instance != null)
                {
                    RadarManager.Instance.RegisterAirplane(planeScript);
                }
            }
        }
    }

    public void NotifyPlaneCrashed(FlightData crashedData)
    {
        if (crashedData == null) return;

        Debug.Log($"<color=red>CRASH: {crashedData.callsign} has crashed. No replacement will be spawned.</color>");
    }
}