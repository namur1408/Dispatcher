using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RadarManager : MonoBehaviour
{
    public static RadarManager Instance;

    public Transform listContainer;
    public GameObject entryPrefab;

    private List<UIAirplane> activeAirplanes = new List<UIAirplane>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (FlightDataManager.Instance != null && FlightDataManager.Instance.savedFlights.Count > 0)
        {
            Debug.Log($"[RadarManager] Возвращаемся со стола! Загружаем {FlightDataManager.Instance.savedFlights.Count} самолетов.");

            activeAirplanes.Clear();

            foreach (var data in FlightDataManager.Instance.savedFlights)
            {
                RestoreAirplane(data);
            }

            StartCoroutine(ApplyDecisionsNextFrame());
        }
        else
        {
            Debug.Log("[RadarManager] Нет сохраненных данных для загрузки. Радар чист.");
        }
    }

    void RestoreAirplane(FlightData data)
    {
        AirplaneSpawner spawner = FindFirstObjectByType<AirplaneSpawner>();
        if (spawner == null) return;

        GameObject newPlane   = Instantiate(spawner.airplanePrefab, spawner.radarContent);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();
        if (planeScript != null)
            planeScript.InitializeFromData(data);
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
            else                 target.Deny();
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
