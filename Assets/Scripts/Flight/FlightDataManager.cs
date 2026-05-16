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

    [Header("Video Mode")]
    [Tooltip("Включи этот флаг чтобы вместо сюжетных рейсов заспавнился один демо-рейс для видео")]
    public bool videoMode = false;

    // Настройки демо-рейса (видны в инспекторе при videoMode = true)
    [Tooltip("Callsign демо самолета")]
    public string videoCallsign = "TR-777";
    [Tooltip("Что РЕАЛЬНО везет (скрытый груз)")]
    public string videoRealCargo = "People";
    [Tooltip("Что написано в манифесте (фейковый груз)")]
    public string videoManifestCargo = "People";
    [Tooltip("Что СКАЖЕТ пилот при вопросе о грузе")]
    public string videoSpokenCargo = "Spare Parts";
    [Tooltip("Откуда, по манифесту")]
    public string videoManifestOrigin = "Bastion-4";
    [Tooltip("Что скажет пилот при вопросе об источнике")]
    public string videoSpokenOrigin = "Bastion-4";
    [Tooltip("Что скажет пилот при вопросе о весе")]
    public string videoSpokenWeight = "";
    [Tooltip("Что скажет пилот при вопросе о скорости")]
    public string videoSpokenSpeed = "";
    [Tooltip("Объяснение пилота при разоблачении груза")]
    public string videoExplanationCargo = "We were... We had no choice. These people needed to be evacuated. Please, just let us land.";
    [Tooltip("Объяснение пилота при разоблачении источника")]
    public string videoExplanationOrigin = "";
    [Tooltip("Кастомный ОТВЕТ пилота на вопрос о грузе (если пусто — авто-фраза)")]
    public string videoCustomAnswerCargo = "";
    [Tooltip("Кастомный ОТВЕТ пилота на вопрос об источнике (если пусто — авто-фраза)")]
    public string videoCustomAnswerOrigin = "";
    [Tooltip("Кастомный ОТВЕТ пилота на вопрос о весе (если пусто — авто-фраза)")]
    public string videoCustomAnswerWeight = "";
    [Tooltip("Кастомный ОТВЕТ пилота на вопрос о скорости (если пусто — авто-фраза)")]
    public string videoCustomAnswerSpeed = "";
    [Header("Video Mode — Вопросы ДИСПЕТЧЕРА")]
    [Tooltip("Что напишет диспетчер при запросе о грузе (если пусто — авто-фраза)")]
    public string videoCustomQuestionCargo = "";
    [Tooltip("Что напишет диспетчер при запросе об источнике (если пусто — авто-фраза)")]
    public string videoCustomQuestionOrigin = "";
    [Tooltip("Что напишет диспетчер при запросе о весе (если пусто — авто-фраза)")]
    public string videoCustomQuestionWeight = "";
    [Tooltip("Что напишет диспетчер при запросе о скорости (если пусто — авто-фраза)")]
    public string videoCustomQuestionSpeed = "";

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
        // Don't spawn mission planes during tutorial
        if (TutorialManager.isTutorialActive)
        {
            Debug.Log("<color=yellow>StartDaySpawning skipped - tutorial is active</color>");
            return;
        }

        isShiftActive = true;

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

        if (AirplaneSpawner.Instance != null)
        {
            AirplaneSpawner.Instance.ResetStoryPlaneFlag();
        }

        if (videoMode)
        {
            // --- РЕЖИМ ВИДЕО ---
            // Один демо-рейс с настраиваемыми параметрами
            FlightData videoFlight = new FlightData(
                videoCallsign,
                new Vector2(-476, 0),
                Vector2.zero,
                new List<Vector2>(),
                75f,
                videoRealCargo, 65,
                videoManifestCargo, 65,
                200f,
                videoManifestOrigin
            );
            videoFlight.spokenCargo   = videoSpokenCargo;
            videoFlight.spokenOrigin  = videoSpokenOrigin;
            if (!string.IsNullOrEmpty(videoSpokenWeight)) videoFlight.spokenWeight = videoSpokenWeight;
            if (!string.IsNullOrEmpty(videoSpokenSpeed))  videoFlight.spokenSpeed  = videoSpokenSpeed;
            videoFlight.explanationCargo  = videoExplanationCargo;
            videoFlight.explanationOrigin = videoExplanationOrigin;
            videoFlight.customAnswerCargo  = videoCustomAnswerCargo;
            videoFlight.customAnswerOrigin = videoCustomAnswerOrigin;
            videoFlight.customAnswerWeight = videoCustomAnswerWeight;
            videoFlight.customAnswerSpeed  = videoCustomAnswerSpeed;
            videoFlight.customQuestionCargo  = videoCustomQuestionCargo;
            videoFlight.customQuestionOrigin = videoCustomQuestionOrigin;
            videoFlight.customQuestionWeight = videoCustomQuestionWeight;
            videoFlight.customQuestionSpeed  = videoCustomQuestionSpeed;
            scriptedFlightsQueue.Enqueue(videoFlight);
            scriptedDelaysQueue.Enqueue(5f);

            Debug.Log("<color=cyan>[VIDEO MODE] Demo flight enqueued: " + videoCallsign + "</color>");
        }
        else if (dayNumber == 1)
        {
            FlightData ge102 = new FlightData("GE-102", new Vector2(-535, 119), Vector2.zero, new List<Vector2>(), 80f, "Fuel", 500, "Fuel", 500, 250f, "Bastion-3");
            scriptedFlightsQueue.Enqueue(ge102);
            scriptedDelaysQueue.Enqueue(20f);

            scriptedFlightsQueue.Enqueue(new FlightData("AX-999", new Vector2(-476, -357), new Vector2(416, 595), new List<Vector2>(), 100f, "None", 0, "None", 0, 9999f, "Unknown"));
            scriptedDelaysQueue.Enqueue(25f);

            FlightData qy884 = new FlightData("QY-884", new Vector2(437, -357), Vector2.zero, new List<Vector2>(), 95f, "Medicines", 2, "Medicines", 2, 160f, "Bastion-5");
            scriptedFlightsQueue.Enqueue(qy884);
            scriptedDelaysQueue.Enqueue(25f);

            scriptedFlightsQueue.Enqueue(new FlightData("ZX-771", new Vector2(416, 476), new Vector2(-238, -535), new List<Vector2>(), 100f, "None", 0, "None", 0, 9999f, "Unknown"));
            scriptedDelaysQueue.Enqueue(20f);

            FlightData tr404 = new FlightData("TR-404", new Vector2(0, 535), Vector2.zero, new List<Vector2>(), 75f, "People", 65, "Food", 50, 100f, "Sector-Z");
            tr404.spokenCargo = "Food";
            tr404.spokenOrigin = "Bastion-4";
            tr404.explanationOrigin = "Sector Z has been destroyed, Control. We barely managed to escape! We probably made a mistake in the rush.";
            tr404.explanationCargo = "Listen, we've had to reclassify the cargo just to stay safe, we're completely out of fuel, and we're about to crash! We have refugees on board. Please let us through — there are children on board!";
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
            float fuelPercentage = (flight.currentFuel / flight.planeMaxFuel) * 100f;

            if (fuelPercentage > 50f)
            {
                flight.isRefueling = true;
                flight.refuelTimer = REFUEL_TIME;
            }
            else
            {
                flight.isRefueled = true;
            }
        }
    }

    public void StartRepairing(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null && !flight.isRepaired && !flight.isRepairing && flight.isUnloaded)
        {
            float fuelPercentage = (flight.currentFuel / flight.planeMaxFuel) * 100f;

            if (fuelPercentage <= 50f)
            {
                flight.isRepairing = true;
                flight.repairTimer = REPAIR_TIME;
            }
            else
            {
                flight.isRepaired = true;
            }
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
    }

    private void CompleteRepair(FlightData flight)
    {
        flight.isRepairing = false;
        flight.isRepaired = true;
    }

    public void MarkFlightAsLanded(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null)
        {
            flight.hasLanded = true;
            landedPlanes++;
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

    public void RemoveDepartedPlane(string callsign)
    {
        savedFlights.RemoveAll(f => f.callsign == callsign);
    }

    public void ResetForNewShift(int startFuel, int startFood, int startPeople, int startMeds)
    {
        List<FlightData> servicedPlanes = new List<FlightData>();

        Debug.Log($"<color=yellow>Checking {savedFlights.Count} flights for serviced planes...</color>");

        foreach (var flight in savedFlights)
        {
            Debug.Log($"<color=cyan>Flight {flight.callsign}: hasLanded={flight.hasLanded}, isUnloaded={flight.isUnloaded}, isRefueled={flight.isRefueled}, isRepaired={flight.isRepaired}</color>");

            if (flight.hasLanded && flight.isUnloaded)
            {
                servicedPlanes.Add(flight);
                Debug.Log($"<color=green>Found serviced plane for departure: {flight.callsign}</color>");
            }
        }

        Debug.Log($"<color=yellow>Total serviced planes to depart: {servicedPlanes.Count}</color>");

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

        if (servicedPlanes.Count > 0)
        {
            EnqueueDepartingPlanes(servicedPlanes);
        }
    }

    private void EnqueueDepartingPlanes(List<FlightData> servicedPlanes)
    {
        float departureDelay = 5f;

        foreach (var plane in servicedPlanes)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 exitPoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 900f;

            FlightData departingPlane = new FlightData(
                plane.callsign,
                Vector2.zero,
                exitPoint,
                new List<Vector2>(),
                plane.speed,
                "None",
                0
            );
            departingPlane.currentFuel = plane.planeMaxFuel;
            departingPlane.manifestCargo = "None";
            departingPlane.manifestCargoAmount = 0;
            departingPlane.manifestOrigin = "Bastion-1";

            scriptedFlightsQueue.Enqueue(departingPlane);
            scriptedDelaysQueue.Enqueue(departureDelay);

            Debug.Log($"<color=cyan>Enqueued departing plane: {departingPlane.callsign} with delay {departureDelay}s, exit point: {exitPoint}</color>");

            departureDelay += 8f;
        }

        Debug.Log($"<color=cyan>Total planes in queue after enqueue: {scriptedFlightsQueue.Count}</color>");
    }
}