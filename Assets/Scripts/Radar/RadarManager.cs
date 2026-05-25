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