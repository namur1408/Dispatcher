using UnityEngine;
using System.Collections.Generic;

public class AirplaneSpawner : MonoBehaviour
{
    public static AirplaneSpawner Instance;

    [Header("Settings")]
    public GameObject airplanePrefab;
    public Transform radarContent;
    public int maxAirplanes = 5;

    public float minSpawnTime = 5f;
    public float maxSpawnTime = 12f;
    public float spawnRadius = 640f;

    [Range(0f, 1f)]
    public float landingProbability = 0.5f;

    [Header("Collision Detection")]
    public float minSpawnDistance = 200f;
    private bool storyPlaneCrashed = false;

    void Awake()
    {
        Instance = this;
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
        Vector2 startPos = data.position;
        Vector2 targetPos = data.targetPosition;

        if (IsPositionOccupied(startPos, contentParent))
        {
            startPos = FindSafeSpawnPosition(startPos, contentParent);
        }

        GameObject newPlane = Instantiate(airplanePrefab, contentParent, false);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();

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
        Vector2 startPos = GetRandomSpawnPosition(contentParent);
        Vector2 targetPos = Vector2.zero;

        if (!onlyLanding && Random.value >= landingProbability)
        {
            float angle = Mathf.Atan2(startPos.y, startPos.x);
            float endAngle = angle + Random.Range(120f, 240f) * Mathf.Deg2Rad;
            targetPos = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * (spawnRadius + 200f);
        }

        string[] prefixes = { "GE", "TR", "QY" };
        string prefix = prefixes[Random.Range(0, prefixes.Length)];
        string callsign = $"{prefix}-{Random.Range(100, 999)}";

        float speed = 80f;
        string cargo = "None";
        int amount = 10;
        float fuel = 200f;

        string[] goods = { "Food", "Fuel", "Medicines" };

        if (prefix == "GE")
        {
            speed = Random.Range(60, 84);
            cargo = goods[Random.Range(0, goods.Length)];
            amount = Random.Range(51, 500);
            fuel = Random.Range(180f, 250f);
        }
        else if (prefix == "TR")
        {
            speed = Random.Range(70, 78);
            cargo = "People";
            amount = Random.Range(20, 250);
            fuel = Random.Range(150f, 220f);
        }
        else if (prefix == "QY")
        {
            speed = Random.Range(81, 105);
            cargo = goods[Random.Range(0, goods.Length)];
            amount = Random.Range(1, 50);
            fuel = Random.Range(160f, 240f);
        }

        FlightData randomData = new FlightData(callsign, startPos, targetPos, new List<Vector2>(), speed, cargo, amount);
        randomData.currentFuel = fuel;

        GameObject newPlane = Instantiate(airplanePrefab, contentParent, false);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();

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
        if (radarContent != null && radarContent.gameObject.activeInHierarchy)
            return radarContent;

        BigRadarLoader loader = FindFirstObjectByType<BigRadarLoader>();
        if (loader != null && loader.radarContent != null)
            return loader.radarContent;

        return null;
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
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
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
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;

            if (!IsPositionOccupied(position, contentParent))
            {
                return position;
            }
        }

        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * spawnRadius;
    }

    public void NotifyPlaneCrashed(FlightData crashedData)
    {
        if (crashedData == null) return;
        
        string[] prefixes = { "GE", "TR", "QY" };
        string prefix = crashedData.callsign.Length >= 2 ? crashedData.callsign.Substring(0, 2) : "GE";
        if (prefix != "GE" && prefix != "TR" && prefix != "QY") prefix = prefixes[Random.Range(0, prefixes.Length)];
        string newCallsign = $"{prefix}-{Random.Range(100, 999)}";

        FlightData replacement = new FlightData(
            newCallsign,
            GetRandomSpawnPosition(GetActiveRadarContent()),
            Vector2.zero,
            new List<Vector2>(),
            crashedData.speed,
            crashedData.cargo,
            crashedData.cargoAmount
        );

        replacement.manifestCargo = crashedData.manifestCargo;
        replacement.manifestCargoAmount = crashedData.manifestCargoAmount;
        replacement.manifestOrigin = crashedData.manifestOrigin;
        
        replacement.spokenCargo = crashedData.spokenCargo;
        replacement.spokenOrigin = crashedData.spokenOrigin;
        replacement.spokenWeight = crashedData.spokenWeight;
        replacement.spokenSpeed = crashedData.spokenSpeed;
        
        replacement.customExplanation = crashedData.customExplanation;
        replacement.explanationCargo = crashedData.explanationCargo;
        replacement.explanationOrigin = crashedData.explanationOrigin;
        replacement.explanationWeight = crashedData.explanationWeight;
        replacement.explanationSpeed = crashedData.explanationSpeed;
        
        replacement.customAnswerCargo = crashedData.customAnswerCargo;
        replacement.customAnswerOrigin = crashedData.customAnswerOrigin;
        replacement.customAnswerWeight = crashedData.customAnswerWeight;
        replacement.customAnswerSpeed = crashedData.customAnswerSpeed;

        replacement.customQuestionCargo = crashedData.customQuestionCargo;
        replacement.customQuestionOrigin = crashedData.customQuestionOrigin;
        replacement.customQuestionWeight = crashedData.customQuestionWeight;
        replacement.customQuestionSpeed = crashedData.customQuestionSpeed;

        replacement.planeMaxFuel = crashedData.planeMaxFuel;
        replacement.currentFuel = replacement.planeMaxFuel;
        
        replacement.departureDestination = crashedData.departureDestination;

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.scriptedFlightsQueue.Enqueue(replacement);
            FlightDataManager.Instance.scriptedDelaysQueue.Enqueue(15f);
            Debug.Log($"<color=orange>CRASH RECOVERY: Queued replacement for {crashedData.callsign} as {newCallsign}</color>");
        }
    }
}