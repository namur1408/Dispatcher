using UnityEngine;
using System.Collections.Generic;

public class FlightDataManager : MonoBehaviour
{
    public static FlightDataManager Instance;
    public List<FlightData> savedFlights = new List<FlightData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateFlights(List<UIAirplane> airplanes)
    {
        savedFlights.Clear();
        foreach (var plane in airplanes)
        {
            if (plane != null && plane.callsignText != null)
            {
                savedFlights.Add(new FlightData(
                    plane.callsignText.text,
                    plane.GetLogicalPosition(),
                    plane.targetPosition,
                    plane.speed
                ));
            }
        }
    }
}