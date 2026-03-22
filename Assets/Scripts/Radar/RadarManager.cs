using UnityEngine;
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
            foreach (var data in FlightDataManager.Instance.savedFlights)
            {
                RestoreAirplane(data);
            }
        }
    }

    void RestoreAirplane(FlightData data)
    {
        AirplaneSpawner spawner = FindObjectOfType<AirplaneSpawner>();
        if (spawner != null)
        {
            GameObject newPlane = Instantiate(spawner.airplanePrefab, spawner.radarContent);
            UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();
            if (planeScript != null)
            {
                planeScript.InitializeFromData(data);
            }
        }
    }

    public void RegisterAirplane(UIAirplane airplane)
    {
        if (!activeAirplanes.Contains(airplane))
        {
            activeAirplanes.Add(airplane);
            if (listContainer != null && entryPrefab != null)
            {
                GameObject entry = Instantiate(entryPrefab, listContainer);
                entry.GetComponent<FlightListEntry>().Setup(airplane);
            }
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