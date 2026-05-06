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
    public float foodPerPersonPerMinute = 0.2f;
    public bool losePeopleWhenStarving = true;

    [Header("Shift Spawning State")]
    public bool isShiftActive = false;
    public float globalSpawnTimer = 3f;

    public Queue<FlightData> scriptedFlightsQueue = new Queue<FlightData>();
    public Queue<float> scriptedDelaysQueue = new Queue<float>();

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

        if (landedPlanes >= maxPlanes)
        {
            for (int i = 0; i < savedFlights.Count; i++)
            {
                var flight = savedFlights[i];
                if (flight.targetPosition == Vector2.zero && !flight.decisionMade)
                {
                    AddDecision(flight.callsign, false);
                }
            }
        }
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
        List<FlightData> updatedList = new List<FlightData>();

        foreach (var plane in airplanes)
        {
            if (plane == null) continue;

            string callsign = plane.originalCallsign;
            FlightData existing = savedFlights.Find(f => f.callsign == callsign);

            if (existing != null)
            {
                existing.position = plane.GetLogicalPosition();
                existing.savedWaypoints = plane.GetWaypoints();
                existing.hasBeenPinged = plane.hasBeenPinged;
                existing.currentFuel = Mathf.RoundToInt(plane.currentFuel);

                existing.isInStorm = (plane.callsignText.text == "NO SIGNAL");

                if (plane.dispatchStatus == UIAirplane.DispatchStatus.Approved) { existing.decisionMade = true; existing.approved = true; }
                else if (plane.dispatchStatus == UIAirplane.DispatchStatus.Denied) { existing.decisionMade = true; existing.approved = false; }

                updatedList.Add(existing);
            }
            else
            {
                FlightData newData = new FlightData(
                    callsign,
                    plane.GetLogicalPosition(),
                    plane.targetPosition,
                    plane.GetWaypoints(),
                    plane.speed,
                    plane.cargo
                );

                newData.isInStorm = (plane.callsignText.text == "NO SIGNAL");

                updatedList.Add(newData);
            }
        }

        foreach (var oldFlight in savedFlights)
        {
            if (oldFlight.decisionMade && oldFlight.approved && oldFlight.hasLanded)
            {
                bool isFullyProcessed = oldFlight.isUnloaded && oldFlight.isRefueled && oldFlight.isRepaired;
                if (!isFullyProcessed && !updatedList.Exists(f => f.callsign == oldFlight.callsign))
                {
                    updatedList.Add(oldFlight);
                }
            }
        }

        savedFlights = updatedList;
    }

    public void StartDaySpawning(int dayNumber)
    {
        isShiftActive = true;
        scriptedFlightsQueue.Clear();
        scriptedDelaysQueue.Clear();

        if (dayNumber == 1)
        {
            FlightData ge102 = new FlightData("GE-102", new Vector2(-900, 200), Vector2.zero, new List<Vector2>(), 80f, "Fuel", 150, "Fuel", 150, 250f, "Bastion-3");
            scriptedFlightsQueue.Enqueue(ge102);
            scriptedDelaysQueue.Enqueue(20f);

            scriptedFlightsQueue.Enqueue(new FlightData("AX-999", new Vector2(-800, -600), new Vector2(700, 1000), new List<Vector2>(), 100f, "None", 0, "None", 0, 9999f, "Unknown"));
            scriptedDelaysQueue.Enqueue(25f);

            FlightData qy884 = new FlightData("QY-884", new Vector2(736, -600), Vector2.zero, new List<Vector2>(), 95f, "Medicines", 2, "Medicines", 2, 160f, "Bastion-5");
            scriptedFlightsQueue.Enqueue(qy884);
            scriptedDelaysQueue.Enqueue(25f);

            scriptedFlightsQueue.Enqueue(new FlightData("ZX-771", new Vector2(700, 800), new Vector2(-400, -900), new List<Vector2>(), 100f, "None", 0, "None", 0, 9999f, "Unknown"));
            scriptedDelaysQueue.Enqueue(20f);

            FlightData tr404 = new FlightData("TR-404", new Vector2(0, 900), Vector2.zero, new List<Vector2>(), 75f, "People", 65, "Food", 50, 85f, "Sector-Z");
            tr404.spokenCargo = "Food";
            tr404.spokenOrigin = "Bastion-4";
            tr404.explanationOrigin = "Sector Z has been destroyed, Control. We barely managed to escape! We probably made a mistake in the rush.";
            tr404.explanationCargo = "Listen, we’ve had to reclassify the cargo just to stay safe, we’re completely out of fuel, and we’re about to crash! We have refugees on board. Please let us through—there are children on board!";
            scriptedFlightsQueue.Enqueue(tr404);
            scriptedDelaysQueue.Enqueue(15f);
        }
        globalSpawnTimer = 3f;
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

        int neededFuel = flight.planeMaxFuel - Mathf.RoundToInt(flight.currentFuel);
        int actualFuelTaken = Mathf.Min(neededFuel, totalFuel);

        totalFuel -= actualFuelTaken;
        flight.currentFuel += actualFuelTaken;

        if (flight.isRepaired) landedPlanes--;
    }

    private void CompleteRepair(FlightData flight)
    {
        flight.isRepairing = false;
        flight.isRepaired = true;

        if (flight.isRefueled) landedPlanes--;
    }

    public void MarkFlightAsLanded(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null) flight.hasLanded = true;
    }

    public void ResetForNewShift(int startFuel, int startFood, int startPeople, int startMeds)
    {
        savedFlights.Clear();
        landedPlanes = 0;
        accumulatedFoodConsumption = 0f;

        totalFuel = startFuel;
        totalFood = startFood;
        totalPeople = startPeople;
        totalMedicines = startMeds;

        UIAirplane[] leftoverPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        foreach (var plane in leftoverPlanes)
        {
            if (plane != null) Destroy(plane.gameObject);
        }
        if (RadarScreenClicker.selectedPlane != null) RadarScreenClicker.selectedPlane = null;

        isShiftActive = false;
        scriptedFlightsQueue.Clear();
        scriptedDelaysQueue.Clear();
    }
}