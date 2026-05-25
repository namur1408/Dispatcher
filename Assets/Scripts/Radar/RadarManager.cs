using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RadarManager : MonoBehaviour
{
    public static RadarManager Instance;

    public Transform listContainer;
    public GameObject entryPrefab;

    public List<UIAirplane> activeAirplanes = new List<UIAirplane>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Ничего не делаем здесь.
        // Восстановление самолетов теперь делается через RebuildFromFlightData(),
        // который вызывается из StoryManager после LoadState().
    }

    void Update()
    {
        CheckForConflicts();
    }

    private void CheckForConflicts()
    {
        bool anyWarning = false;
        float warningDistance = 125f; // Same as BigRadarLoader

        // We first clear warnings for all small radar planes
        foreach (var plane in activeAirplanes)
            if (plane != null) plane.SetWarning(false);

        for (int i = 0; i < activeAirplanes.Count; i++)
        {
            for (int j = i + 1; j < activeAirplanes.Count; j++)
            {
                UIAirplane a = activeAirplanes[i];
                UIAirplane b = activeAirplanes[j];
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

        // If BigRadarLoader is active, it will handle isGlobalWarningActive itself.
        // If it's not active, RadarManager handles it.
        // Since both radars share the same coordinate logic mostly, we can just let 
        // RadarManager always update it, OR we can sync them.
        BigRadarLoader.isGlobalWarningActive = anyWarning;
    }

    /// <summary>
    /// Точно как BigRadarLoader.RebuildAll() — читаем FlightDataManager
    /// и спавним все самолеты, которые ещё в воздухе.
    /// </summary>
    public void RebuildFromFlightData()
    {
        if (FlightDataManager.Instance == null) return;

        // Убиваем все текущие визуальные объекты на радаре
        foreach (var plane in activeAirplanes)
            if (plane != null) Destroy(plane.gameObject);
        activeAirplanes.Clear();

        // Читаем прямо из FlightDataManager — так же как это делает BigRadarLoader
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

        GameObject newPlane = Instantiate(spawner.airplanePrefab, spawner.radarContent);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();
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
}