using UnityEngine;
using System.Collections.Generic;

public class FlightDataManager : SingletonMB<FlightDataManager>
{
    protected override bool ShouldPersist => true;

    public List<FlightData> savedFlights = new List<FlightData>();
    public Dictionary<string, FlightInterrogationState> interrogationStates = new Dictionary<string, FlightInterrogationState>();

    [Header("Base Stats")]
    public int landedPlanes = 0;
    public int maxPlanes = 5;

    public int totalMedicines = 9;
    public int totalPeople = 180;
    public int totalFood = 850;
    public int totalFuel = 1500;

    [Header("Warehouse Maximums")]
    public int maxPeople = 300;
    public int maxFuel = 1500;
    public int maxMedicines = 20;
    public int maxFood = 850;

    [Header("Consumption Settings")]
    public float foodPerPersonPerMinute = 0.2f;
    public bool losePeopleWhenStarving = true;

    // Day Summary Tracking Variables
    public int startFuelDay;
    public int startFoodDay;
    public int startPeopleDay;
    public int startMedsDay;

    [Header("Shift Spawning State")]
    public bool isShiftActive = false;
    public float globalSpawnTimer = 3f;



    public Queue<FlightData> scriptedFlightsQueue = new Queue<FlightData>();
    public Queue<float> scriptedDelaysQueue = new Queue<float>();

    public float accumulatedFoodConsumption = 0f;

    // Maintenance timers have been moved to FlightConstants as UnloadTime / RefuelTime / RepairTime.

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
    }

    private void OnEnable()
    {
        GameEvents.OnFlightLanded += HandleFlightLanded;
    }

    private void OnDisable()
    {
        GameEvents.OnFlightLanded -= HandleFlightLanded;
    }

    void Update()
    {
        for (int i = 0; i < savedFlights.Count; i++)
        {
            var flight = savedFlights[i];

            if (flight.unloading.Tick(Time.deltaTime)) CompleteUnload(flight);
            if (flight.refueling.Tick(Time.deltaTime)) CompleteRefuel(flight);
            if (flight.repairing.Tick(Time.deltaTime)) CompleteRepair(flight);
        }

        if (landedPlanes >= maxPlanes)
        {
            UIAirplane[] allPlanes = null;
            for (int i = 0; i < savedFlights.Count; i++)
            {
                var flight = savedFlights[i];
                // Check if this is an arrival flight and is still in the air
                if (flight.targetPosition == Vector2.zero && !flight.hasLanded)
                {
                    // If not already denied, deny it!
                    if (!flight.decisionMade || flight.approved)
                    {
                        flight.decisionMade = true;
                        flight.approved = false;

                        if (allPlanes == null && RadarManager.Instance != null) allPlanes = RadarManager.Instance.activeAirplanes.ToArray();
                        if (allPlanes != null)
                        {
                            foreach (var plane in allPlanes)
                            {
                                if (plane != null && plane.originalCallsign == flight.callsign)
                                {
                                    plane.Deny();
                                }
                            }
                        }
                    }
                }
            }
        }
    }



    public FlightData GetFlight(string callsign)
    {
        if (string.IsNullOrEmpty(callsign)) return null;
        return savedFlights.Find(f => f.callsign == callsign);
    }

    public FlightInterrogationState GetOrCreateInterrogationState(string callsign)
    {
        if (string.IsNullOrEmpty(callsign)) return null;
        if (!interrogationStates.ContainsKey(callsign))
        {
            interrogationStates[callsign] = new FlightInterrogationState(callsign);
        }
        return interrogationStates[callsign];
    }

    public float GetCurrentFoodConsumptionPerMinute()
    {
        return totalPeople * foodPerPersonPerMinute;
    }

    public void UpdateFlights(List<UIAirplane> airplanes, bool allowDeletions = true)
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
                existing.UpdateSerializedWaypoints();
                existing.hasBeenPinged = plane.hasBeenPinged;
                existing.currentFuel = Mathf.RoundToInt(plane.currentFuel);
                existing.isInStorm = plane.inStorm;

                existing.assignedRunway = plane.assignedRunway;
                existing.isAligningToLand = plane.isAligningToLand;
                existing.isDeparting = plane.isDeparting;
                existing.departureDestination = plane.departureDestination;

                existing.isTakingOff = plane.isTakingOff;
                existing.takeoffStartPos = plane.takeoffStartPos;

                existing.isLandingPhase = plane.isLandingPhase;

                if (plane.dispatchStatus == UIAirplane.DispatchStatus.Approved) 
                { 
                    existing.decisionMade = true; 
                    existing.approved = true; 
                }
                else if (plane.dispatchStatus == UIAirplane.DispatchStatus.Denied) 
                { 
                    existing.decisionMade = true; 
                    existing.approved = false; 
                }
                // DO NOT reset existing decisions if the plane in the scene is still 'Pending'
                // This prevents UI screens (like the Terminal) from having their decisions overwritten.

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

                newData.hasBeenPinged = plane.hasBeenPinged;
                newData.currentFuel = Mathf.RoundToInt(plane.currentFuel);
                newData.isInStorm = plane.inStorm;

                newData.assignedRunway = plane.assignedRunway;
                newData.isAligningToLand = plane.isAligningToLand;
                newData.isDeparting = plane.isDeparting;
                newData.departureDestination = plane.departureDestination;

                newData.isTakingOff = plane.isTakingOff;
                newData.takeoffStartPos = plane.takeoffStartPos;

                newData.isLandingPhase = plane.isLandingPhase;

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
                else
                {
                    newData.decisionMade = false;
                    newData.approved = false;
                }

                updatedList.Add(newData);
            }
        }

        foreach (var oldFlight in savedFlights)
        {
            // We save aircraft that have been accepted and landed (serviced at the base), 
            // as well as those that are ready to take off (waiting in Departures), but have not yet spawned on the radar.
            if (!allowDeletions || oldFlight.isReadyToDepart || (oldFlight.decisionMade && oldFlight.approved && oldFlight.hasLanded))
            {
                if (!updatedList.Exists(f => f.callsign == oldFlight.callsign))
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

        // We check all saved flights for readiness for departure.
        // Fully serviced aircraft from the previous shift will receive isReadyToDepart=true,
        // since their arrivalDay < currentDay (new day).
        foreach (var flight in savedFlights)
        {
            CheckDepartureReadiness(flight);
        }

        Debug.Log($"<color=magenta>StartDaySpawning called for day {dayNumber}. Queue count before: {scriptedFlightsQueue.Count}</color>");

        Queue<FlightData> departingPlanes = new Queue<FlightData>(scriptedFlightsQueue);
        Queue<float> departingDelays = new Queue<float>(scriptedDelaysQueue);

        scriptedFlightsQueue.Clear();
        scriptedDelaysQueue.Clear();

        while (departingPlanes.Count > 0)
        {
            FlightData plane = departingPlanes.Dequeue();
            scriptedFlightsQueue.Enqueue(plane);
            Debug.Log($"<color=magenta>Re-enqueued departing plane: {plane.callsign}</color>");
            if (departingDelays.Count > 0)
            {
                scriptedDelaysQueue.Enqueue(departingDelays.Dequeue());
            }
        }

        Debug.Log($"<color=magenta>Queue count after re-enqueue: {scriptedFlightsQueue.Count}</color>");


        IDayLogic currentDayLogic = DayLogicProvider.GetDayLogic(dayNumber);
        currentDayLogic.EnqueueFlights(scriptedFlightsQueue, scriptedDelaysQueue, CalculateStoryFuel);
        globalSpawnTimer = 3f;
    }

    /// <summary>
    /// Calculates fuel for a story plane based on route distance + safety buffer.
    /// Matches the same formula as AirplaneSpawner for random planes.
    /// </summary>
    private float CalculateStoryFuel(Vector2 startPos, Vector2 targetPos)
    {
        const float FUEL_PER_DISTANCE_UNIT = 6f;
        float routeDistance = targetPos == Vector2.zero
            ? startPos.magnitude
            : Vector2.Distance(startPos, targetPos);
        float minFuel = routeDistance / FUEL_PER_DISTANCE_UNIT;
        // Story planes get a fixed generous buffer (×3.0) — consistent, no randomness
        float fuel = minFuel * 3.0f;
        return Mathf.Clamp(fuel, 150f, 600f);
    }

    public void AddDecision(string callsign, bool isApproved)
    {
        for (int i = 0; i < savedFlights.Count; i++)
        {
            if (savedFlights[i].callsign == callsign)
            {
                savedFlights[i].decisionMade = true;
                savedFlights[i].approved = isApproved;

                if (RadarManager.Instance != null)
                {
                    UIAirplane[] allPlanes = RadarManager.Instance.activeAirplanes.ToArray();
                    foreach (var plane in allPlanes)
                    {
                        if (plane != null && plane.originalCallsign == callsign)
                        {
                            if (isApproved) plane.Approve();
                            else plane.Deny();
                        }
                    }
                }
                return;
            }
        }
    }

    public void StartUnloading(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null) flight.StartUnloading(FlightConstants.UnloadTime);
    }

    public void StartRefueling(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null && flight.isUnloaded) flight.StartRefueling(FlightConstants.RefuelTime);
    }

    public void StartRepairing(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null && flight.isUnloaded) flight.StartRepairing(FlightConstants.RepairTime);
    }

    private void CompleteUnload(FlightData flight)
    {
        // unloading.Tick() already set isComplete; just handle the resource delivery
        string c = flight.cargo;
        if (c == "Medicines") totalMedicines = Mathf.Min(totalMedicines + flight.cargoAmount, maxMedicines);
        else if (c == "People") totalPeople = Mathf.Min(totalPeople + flight.cargoAmount, maxPeople);
        else if (c == "Food") totalFood = Mathf.Min(totalFood + flight.cargoAmount, maxFood);
        else if (c == "Fuel") totalFuel = Mathf.Min(totalFuel + flight.cargoAmount, maxFuel);

        CheckDepartureReadiness(flight);
    }

    private void CompleteRefuel(FlightData flight)
    {
        // refueling.Tick() already set isComplete; transfer fuel from base reserves to plane
        int neededFuel     = flight.planeMaxFuel - Mathf.RoundToInt(flight.currentFuel);
        int actualFuelTaken = Mathf.Min(neededFuel, totalFuel);

        totalFuel          -= actualFuelTaken;
        flight.currentFuel += actualFuelTaken;

        CheckDepartureReadiness(flight);
    }

    private void CompleteRepair(FlightData flight)
    {
        // repairing.Tick() already set isComplete
        CheckDepartureReadiness(flight);
    }

    private void CheckDepartureReadiness(FlightData flight)
    {
        if (flight.isUnloaded && flight.isRefueled && flight.isRepaired && flight.hasLanded)
        {
            if (flight.arrivalDay > 0 && flight.arrivalDay < StoryManager.currentDay)
            {
                flight.isReadyToDepart = true;
                flight.assignedRunway = "";
                if (string.IsNullOrEmpty(flight.departureDestination) || flight.departureDestination == "UNKNOWN")
                {
                    flight.departureDestination = string.IsNullOrEmpty(flight.manifestOrigin) ? "UNKNOWN" : flight.manifestOrigin;
                }
            }
        }
    }

    private void HandleFlightLanded(FlightData flight)
    {
        if (flight != null && savedFlights.Contains(flight))
        {
            flight.hasLanded = true;
            landedPlanes++;

            // Landing service logic:
            // If the fuel level is more than 50%, it needs to be repaired (therefore the refueling is already “done”)
            // If the fuel level is less than or equal to 50%, it needs to be refueled (so the repair has already been “done”)
            float fuelPercentage = (flight.currentFuel / flight.planeMaxFuel) * 100f;
            if (fuelPercentage > 50f)
            {
                flight.SkipRefuel();
            }
            else
            {
                flight.SkipRepair();
            }

            if (flight.callsign == Callsigns.TR_88 || flight.callsign == Callsigns.GE_98)
            {
                StartCoroutine(EnemySFLandedRoutine());
            }

            if (HintManager.Instance != null) HintManager.Instance.TriggerUnloadPlaneHint();
        }
    }

    private System.Collections.IEnumerator EnemySFLandedRoutine()
    {
        yield return new WaitForSeconds(15f);
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.TriggerGameOverCaptured();
        }
    }

    public bool ShouldPlaneDepart(FlightData flight)
    {
        return flight.hasLanded && flight.isUnloaded && flight.isRefueled && flight.isRepaired;
    }

    public Vector2 GetDepartureTarget(FlightData flight)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 360f;
    }

    public void FreeBaseSlot(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null && flight.hasLanded)
        {
            flight.hasTakenOff = true;
            flight.hasLanded = false; // The plane is in the air again
            landedPlanes = Mathf.Max(0, landedPlanes - 1);
            Debug.Log($"<color=lime>[FlightDataManager] {callsign} took off. Base slot freed. landedPlanes: {landedPlanes}</color>");
        }
    }

    public void RemoveDepartedPlane(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null && flight.hasLanded)
        {
            landedPlanes = Mathf.Max(0, landedPlanes - 1);
            Debug.Log($"<color=lime>[FlightDataManager] {callsign} departed. landedPlanes: {landedPlanes}</color>");
        }
        savedFlights.RemoveAll(f => f.callsign == callsign);
    }

    public void ResetForNewShift(int startFuel, int startFood, int startPeople, int startMeds)
    {
        List<FlightData> preservedPlanes = new List<FlightData>();

        int preservedLandedCount = 0;

        foreach (var flight in savedFlights)
        {
            // We save the landed planes that did NOT take off this shift:
            // - partially or fully serviced (will appear in Departures the next day)
            // - NOT those who have already taken off (isDeparting)
            if (flight.hasLanded && !flight.isDeparting)
            {
                preservedPlanes.Add(flight);
                preservedLandedCount++;

                // We reassess readiness for shipment taking into account the arrival of a new day
                flight.isReadyToDepart = false;
                CheckDepartureReadiness(flight);
            }
        }

        savedFlights.Clear();
        savedFlights.AddRange(preservedPlanes);

        landedPlanes = preservedLandedCount;
        accumulatedFoodConsumption = 0f;

        totalFuel = startFuel;
        totalFood = startFood;
        totalPeople = startPeople;
        totalMedicines = startMeds;

        startFuelDay = startFuel;
        startFoodDay = startFood;
        startPeopleDay = startPeople;
        startMedsDay = startMeds;

        UIAirplane[] leftoverPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var plane in leftoverPlanes)
        {
            if (plane != null)
            {
                plane.CleanupRouteVisuals(); // Always clean up dangling line segments
                if (plane.gameObject.activeSelf)
                    AirplaneSpawner.Instance.ReturnPlaneToPool(plane);
            }
        }
        if (RadarScreenClicker.selectedPlane != null) RadarScreenClicker.selectedPlane = null;
        RadioManager.activeCallsign = "";

        isShiftActive = false;
        scriptedFlightsQueue.Clear();
        scriptedDelaysQueue.Clear();

        // Clear interrogation states for the new shift
        interrogationStates.Clear();
    }

    public void LoadState(SaveData data)
    {
        this.isShiftActive = data.isShiftActive;
        this.globalSpawnTimer = data.globalSpawnTimer;
        
        // Filtering duplicates from old broken saves
        this.savedFlights = new System.Collections.Generic.List<FlightData>();
        System.Collections.Generic.HashSet<string> seenCallsigns = new System.Collections.Generic.HashSet<string>();
        
        foreach (var f in data.savedFlights)
        {
            if (!seenCallsigns.Contains(f.callsign))
            {
                seenCallsigns.Add(f.callsign);
                this.savedFlights.Add(f);
            }
        }
        
        this.scriptedFlightsQueue = new Queue<FlightData>();
        this.scriptedDelaysQueue = new Queue<float>();
        
        for (int i = 0; i < data.pendingFlights.Count; i++)
        {
            var f = data.pendingFlights[i];
            float d = (i < data.pendingDelays.Count) ? data.pendingDelays[i] : 5f;
            
            if (!seenCallsigns.Contains(f.callsign))
            {
                seenCallsigns.Add(f.callsign);
                this.scriptedFlightsQueue.Enqueue(f);
                this.scriptedDelaysQueue.Enqueue(d);
            }
        }
        
        this.totalFuel = data.totalFuel;
        this.totalFood = data.totalFood;
        this.totalPeople = data.totalPeople;
        this.totalMedicines = data.totalMedicines;
        
        this.startFuelDay = data.startFuelDay;
        this.startFoodDay = data.startFoodDay;
        this.startPeopleDay = data.startPeopleDay;
        this.startMedsDay = data.startMedsDay;
        
        this.maxPlanes = data.maxPlanes;
        this.landedPlanes = data.landedPlanes;
        this.accumulatedFoodConsumption = data.accumulatedFoodConsumption;

        if (this.savedFlights != null)
        {
            foreach (var flight in this.savedFlights)
            {
                flight.ReconstructWaypoints();
            }
        }

        this.interrogationStates.Clear();
        if (data.interrogationKeys != null && data.interrogationValues != null)
        {
            for (int i = 0; i < data.interrogationKeys.Count; i++)
            {
                if (i < data.interrogationValues.Count)
                {
                    this.interrogationStates[data.interrogationKeys[i]] = data.interrogationValues[i];
                }
            }
        }
    }
}