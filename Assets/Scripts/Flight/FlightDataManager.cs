using UnityEngine;
using System.Collections.Generic;

public class FlightDataManager : MonoBehaviour
{
    public static FlightDataManager Instance;

    public List<FlightData> savedFlights = new List<FlightData>();

    [Header("Base Stats")]
    public int landedPlanes = 0;
    public int maxPlanes = 5;

    public int totalMedicines = 0;
    public int totalPeople = 0;
    public int totalFood = 0;
    public int totalFuel = 0;

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
        // Сохраняем старый список, чтобы не потерять статус разгрузки
        List<FlightData> oldFlights = new List<FlightData>(savedFlights);
        savedFlights.Clear();

        foreach (var plane in airplanes)
        {
            if (plane != null && plane.callsignText != null)
            {
                FlightData newData = new FlightData(
                    plane.callsignText.text,
                    plane.GetLogicalPosition(),
                    plane.targetPosition,
                    plane.GetWaypoints(),
                    plane.speed,
                    plane.cargo
                );

                if (plane.dispatchStatus == UIAirplane.DispatchStatus.Approved)
                {
                    newData.decisionMade = true;
                    newData.approved = true;
                }
                else if (plane.dispatchStatus == UIAirplane.DispatchStatus.Denied)
                {
                    newData.decisionMade = true;
                    newData.approved = false;
                }

                // Переносим статус разгрузки из старых данных в новые
                var oldData = oldFlights.Find(f => f.callsign == newData.callsign);
                if (oldData != null)
                {
                    newData.isUnloaded = oldData.isUnloaded;
                }

                savedFlights.Add(newData);
            }
        }
    }

    public void AddDecision(string callsign, bool isApproved)
    {
        for (int i = 0; i < savedFlights.Count; i++)
        {
            if (savedFlights[i].callsign == callsign)
            {
                savedFlights[i].decisionMade = true;
                savedFlights[i].approved = isApproved;
                if (isApproved)
                {
                    landedPlanes++;
                }

                Debug.Log($"[FlightDataManager] {callsign} -> {(isApproved ? "APPROVED" : "DENIED")}");
                return;
            }
        }
    }
}