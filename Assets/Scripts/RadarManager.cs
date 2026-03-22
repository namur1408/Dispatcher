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

    public void RegisterAirplane(UIAirplane airplane)
    {
        activeAirplanes.Add(airplane);

        GameObject entry = Instantiate(entryPrefab, listContainer);
        FlightListEntry entryScript = entry.GetComponent<FlightListEntry>();
        entryScript.Setup(airplane);
    }

    public void UnregisterAirplane(UIAirplane airplane)
    {
        activeAirplanes.Remove(airplane);
    }

    public void SelectAirplane(UIAirplane selectedPlane)
    {
        foreach (var plane in activeAirplanes)
        {
            plane.SetHighlight(plane == selectedPlane);
        }
    }

    public int GetPlanesCount()
    {
        return activeAirplanes.Count;
    }
}