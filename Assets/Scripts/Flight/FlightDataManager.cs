using UnityEngine;
using System.Collections.Generic;

public class FlightDataManager : MonoBehaviour
{
    public static FlightDataManager Instance;

    public List<FlightData> savedFlights = new List<FlightData>();

    [Header("Base Stats")]
    public int landedPlanes = 0;
    public int maxPlanes = 5;

    public int totalMedicines = 9;
    public int totalPeople = 180;
    public int totalFood = 850;
    public int totalFuel = 1500;

    [Header("Warehouse Maximums")]
    public int maxPeople = 250;
    public int maxFuel = 1500;
    public int maxMedicines = 12;
    public int maxFood = 850;

    [Header("Consumption Settings")]
    [Tooltip("Сколько еды потребляет 1 человек за реальную минуту")]
    public float foodPerPersonPerMinute = 0.2f;
    [Tooltip("Терять ли людей, если еда закончилась?")]
    public bool losePeopleWhenStarving = true;

    private float accumulatedFoodConsumption = 0f;

    public const float UNLOAD_TIME = 15f;
    public const float REFUEL_TIME = 15f;
    public const float REPAIR_TIME = 20f;

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

    void Update()
    {
        for (int i = 0; i < savedFlights.Count; i++)
        {
            var flight = savedFlights[i];

            if (flight.isUnloading)
            {
                flight.unloadTimer -= Time.deltaTime;
                if (flight.unloadTimer <= 0) CompleteUnload(flight);
            }

            if (flight.isRefueling)
            {
                flight.refuelTimer -= Time.deltaTime;
                if (flight.refuelTimer <= 0) CompleteRefuel(flight);
            }

            if (flight.isRepairing)
            {
                flight.repairTimer -= Time.deltaTime;
                if (flight.repairTimer <= 0) CompleteRepair(flight);
            }
        }
        ProcessFoodConsumption();
    }

    private void ProcessFoodConsumption()
    {
        if (totalPeople > 0)
        {
            float consumptionRatePerSecond = foodPerPersonPerMinute / 60f;
            float consumptionThisFrame = (totalPeople * consumptionRatePerSecond) * Time.deltaTime;
            accumulatedFoodConsumption += consumptionThisFrame;

            if (accumulatedFoodConsumption >= 1f)
            {
                int foodToDeduct = Mathf.FloorToInt(accumulatedFoodConsumption);
                totalFood -= foodToDeduct;
                accumulatedFoodConsumption -= foodToDeduct;

                if (totalFood < 0)
                {
                    int unfedDemand = Mathf.Abs(totalFood);
                    totalFood = 0;

                    if (losePeopleWhenStarving)
                    {
                        totalPeople -= unfedDemand;
                        if (totalPeople < 0) totalPeople = 0;
                    }
                }
            }
        }
    }

    public float GetCurrentFoodConsumptionPerMinute()
    {
        return totalPeople * foodPerPersonPerMinute;
    }

    public void UpdateFlights(List<UIAirplane> airplanes)
    {
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

                newData.hasBeenPinged = plane.hasBeenPinged;

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

                var oldData = oldFlights.Find(f => f.callsign == newData.callsign);
                if (oldData != null)
                {
                    newData.hasLanded = oldData.hasLanded;

                    newData.isUnloaded = oldData.isUnloaded;
                    newData.isRefueled = oldData.isRefueled;
                    newData.currentFuel = oldData.currentFuel;
                    newData.planeMaxFuel = oldData.planeMaxFuel;
                    newData.cargoAmount = oldData.cargoAmount;

                    newData.isUnloading = oldData.isUnloading;
                    newData.unloadTimer = oldData.unloadTimer;
                    newData.isRefueling = oldData.isRefueling;
                    newData.refuelTimer = oldData.refuelTimer;
                    newData.isRepaired = oldData.isRepaired;
                    newData.isRepairing = oldData.isRepairing;
                    newData.repairTimer = oldData.repairTimer;
                }

                savedFlights.Add(newData);
            }
        }

        foreach (var oldFlight in oldFlights)
        {
            if (oldFlight.decisionMade && oldFlight.approved)
            {
                bool isFullyProcessed = oldFlight.isUnloaded && oldFlight.isRefueled && oldFlight.isRepaired;
                if (!isFullyProcessed && !savedFlights.Exists(f => f.callsign == oldFlight.callsign))
                {
                    savedFlights.Add(oldFlight);
                }
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
                if (isApproved) landedPlanes++;
                return;
            }
        }
    }


    public void StartUnloading(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null && !flight.isUnloaded && !flight.isUnloading)
        {
            flight.isUnloading = true;
            flight.unloadTimer = UNLOAD_TIME;
        }
    }

    public void StartRefueling(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null && !flight.isRefueled && !flight.isRefueling && flight.isUnloaded)
        {
            flight.isRefueling = true;
            flight.refuelTimer = REFUEL_TIME;
        }
    }

    public void StartRepairing(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null && !flight.isRepaired && !flight.isRepairing && flight.isUnloaded)
        {
            flight.isRepairing = true;
            flight.repairTimer = REPAIR_TIME;
        }
    }

    private void CompleteUnload(FlightData flight)
    {
        flight.isUnloading = false;
        flight.isUnloaded = true;

        string c = flight.cargo;
        if (c == "Medicines") totalMedicines = Mathf.Min(totalMedicines + flight.cargoAmount, maxMedicines);
        else if (c == "People") totalPeople = Mathf.Min(totalPeople + flight.cargoAmount, maxPeople);
        else if (c == "Food") totalFood = Mathf.Min(totalFood + flight.cargoAmount, maxFood);
        else if (c == "Fuel") totalFuel = Mathf.Min(totalFuel + flight.cargoAmount, maxFuel);
    }

    private void CompleteRefuel(FlightData flight)
    {
        flight.isRefueling = false;
        flight.isRefueled = true;

        int neededFuel = flight.planeMaxFuel - flight.currentFuel;
        int actualFuelTaken = Mathf.Min(neededFuel, totalFuel);

        totalFuel -= actualFuelTaken;
        flight.currentFuel += actualFuelTaken;

        if (flight.isRepaired)
        {
            landedPlanes--;
        }
    }

    private void CompleteRepair(FlightData flight)
    {
        flight.isRepairing = false;
        flight.isRepaired = true;

        if (flight.isRefueled)
        {
            landedPlanes--;
        }
    }

    public void MarkFlightAsLanded(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null)
        {
            flight.hasLanded = true;
        }
    }
}