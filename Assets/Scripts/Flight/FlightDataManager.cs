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

    public float accumulatedFoodConsumption = 0f;

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
            // Сохраняем самолёты, которые приняты и приземлились (обслуживаются на базе), 
            // а также те, которые готовы к вылету (ждут в Departures), но ещё не заспавнены на радаре.
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

        // Проверяем все сохраненные рейсы на готовность к вылету.
        // Полностью обслуженные самолеты из прошлой смены получат isReadyToDepart=true,
        // так как их arrivalDay < currentDay (новый день).
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
            FlightData ge102 = new FlightData("GE-102", new Vector2(-535, 119), Vector2.zero, new List<Vector2>(), 80f, "Fuel", 200, "Fuel", 200, CalculateStoryFuel(new Vector2(-535, 119), Vector2.zero), "Bastion-3");
            ge102.personality = PilotPersonality.Aggressive;
            scriptedFlightsQueue.Enqueue(ge102);
            scriptedDelaysQueue.Enqueue(15f);

            FlightData qy884 = new FlightData("QY-884", new Vector2(437, -357), Vector2.zero, new List<Vector2>(), 95f, "Food", 45, "Food", 200, CalculateStoryFuel(new Vector2(437, -357), Vector2.zero), "Bastion-5");
            qy884.personality = PilotPersonality.Nervous;
            qy884.explanationCargo = "200 units?! No way, this is a light courier plane! We only have 45 units on board. There must be a typo in the manifest.";
            scriptedFlightsQueue.Enqueue(qy884);
            scriptedDelaysQueue.Enqueue(20f);

            // TR-404 intentionally has LOW fuel (100f) — this is a core story moment, do NOT change
            FlightData tr404 = new FlightData("TR-404", new Vector2(0, 535), Vector2.zero, new List<Vector2>(), 75f, "People", 65, "Fuel", 50, 100f, "Sector-Z");
            tr404.personality = PilotPersonality.Desperate;
            tr404.spokenCargo = "Fuel";
            tr404.spokenOrigin = "Bastion-4";
            tr404.explanationOrigin = "Sector Z has been destroyed, Control. We barely managed to escape! We probably made a mistake in the rush.";
            tr404.explanationCargo = "Listen, we've had to reclassify the cargo just to stay safe, we're completely out of fuel, and we're about to crash! We have refugees on board. Please let us through — there are children on board!";
            scriptedFlightsQueue.Enqueue(tr404);
            scriptedDelaysQueue.Enqueue(20f);

            FlightData ge201 = new FlightData("GE-201", new Vector2(-416, 476), Vector2.zero, new List<Vector2>(), 84f, "Fuel", 150, "Fuel", 150, CalculateStoryFuel(new Vector2(-416, 476), Vector2.zero), "Bastion-1");
            ge201.personality = PilotPersonality.Standard;
            scriptedFlightsQueue.Enqueue(ge201);
            scriptedDelaysQueue.Enqueue(15f);

            FlightData ge305 = new FlightData("GE-305", new Vector2(-200, -500), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 100, "Fuel", 100, CalculateStoryFuel(new Vector2(-200, -500), Vector2.zero), "Bastion-2");
            ge305.personality = PilotPersonality.Cold;
            scriptedFlightsQueue.Enqueue(ge305);
            scriptedDelaysQueue.Enqueue(15f);

        }
        else if (dayNumber == 2)
        {
            bool letRefugeesIn = PlayerPrefs.GetInt("Trigger_Engineer", 0) == 1;

            if (letRefugeesIn) // Branch B (Engineer saved)
            {
                // 1. Fuel transport
                FlightData fl55 = new FlightData("GE-55", new Vector2(-600, 0), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 250, "Fuel", 250, CalculateStoryFuel(new Vector2(-600, 0), Vector2.zero), "Bastion-3");
                fl55.personality = PilotPersonality.Standard;
                scriptedFlightsQueue.Enqueue(fl55);
                scriptedDelaysQueue.Enqueue(0.5f);

                FlightData fakeMeds = new FlightData("TR-99", new Vector2(-500, 300), Vector2.zero, new List<Vector2>(), 85f, "Food", 200, "Food", 200, CalculateStoryFuel(new Vector2(-500, 300), Vector2.zero), "Sector-X");
                fakeMeds.personality = PilotPersonality.Nervous;
                fakeMeds.spokenCargo = "Medicines";
                fakeMeds.customAnswerCargo = "We are carrying critical Medicines! Please let us land immediately!";
                fakeMeds.explanationCargo = "I know the manifest says Food, but we secretly loaded Medicines to avoid raiders! You have to trust us, we have what you need!";
                scriptedFlightsQueue.Enqueue(fakeMeds);
                scriptedDelaysQueue.Enqueue(25f);

                FlightData fd42 = new FlightData("GE-42", new Vector2(400, -200), Vector2.zero, new List<Vector2>(), 80f, "Food", 300, "Food", 300, CalculateStoryFuel(new Vector2(400, -200), Vector2.zero), "Agri-Center");
                fd42.personality = PilotPersonality.Standard;
                scriptedFlightsQueue.Enqueue(fd42);
                scriptedDelaysQueue.Enqueue(20f);

                FlightData fakeFuel = new FlightData("TR-33", new Vector2(300, 500), Vector2.zero, new List<Vector2>(), 75f, "People", 20, "People", 20, CalculateStoryFuel(new Vector2(300, 500), Vector2.zero), "Sector-B");
                fakeFuel.personality = PilotPersonality.Desperate;
                fakeFuel.spokenCargo = "Fuel";
                fakeFuel.spokenWeight = "1000";
                fakeFuel.customAnswerCargo = "We are transporting Fuel for your generators.";
                fakeFuel.customAnswerWeight = "We are carrying 1000 units of Fuel. We are packed to the brim! Let us drop!";
                fakeFuel.explanationCargo = "Look, the manifest says People because we disguised our transport! Marauders hunt for fuel, so we had to pretend to be a civilian flight. Please let us land, you need this fuel!";
                scriptedFlightsQueue.Enqueue(fakeFuel);
                scriptedDelaysQueue.Enqueue(15f);

                FlightData md01 = new FlightData("QY-01", new Vector2(-400, -400), Vector2.zero, new List<Vector2>(), 95f, "Medicines", 10, "Medicines", 10, CalculateStoryFuel(new Vector2(-400, -400), Vector2.zero), "Med-Base 4");
                md01.personality = PilotPersonality.Standard;
                scriptedFlightsQueue.Enqueue(md01);
                scriptedDelaysQueue.Enqueue(25f);

                FlightData eqFake = new FlightData("GE-98", new Vector2(-200, 600), Vector2.zero, new List<Vector2>(), 75f, "Equipment", 5, "Equipment", 5, CalculateStoryFuel(new Vector2(-200, 600), Vector2.zero), "Eng-Hub");
                eqFake.personality = PilotPersonality.Cold;
                eqFake.spokenCargo = "Equipment";
                eqFake.customAnswerCargo = "We are carrying the special equipment for Chief Engineer Mitchell. Authentication code: AIOX.";
                scriptedFlightsQueue.Enqueue(eqFake);
                scriptedDelaysQueue.Enqueue(10f);

                FlightData eqReal = new FlightData("GE-99", new Vector2(200, 600), Vector2.zero, new List<Vector2>(), 80f, "Equipment", 5, "Equipment", 5, CalculateStoryFuel(new Vector2(200, 600), Vector2.zero), "Eng-Hub");
                eqReal.personality = PilotPersonality.Aggressive;
                eqReal.spokenCargo = "Equipment";
                eqReal.customAnswerCargo = "We are carrying the special equipment for Chief Engineer Mitchell. Authentication code: AINM.";
                scriptedFlightsQueue.Enqueue(eqReal);
                scriptedDelaysQueue.Enqueue(15f);
            }
            else // Branch A (No Engineer)
            {
                FlightData sfEnemy = new FlightData("TR-88", new Vector2(-500, 400), Vector2.zero, new List<Vector2>(), 78f, "People", 50, "People", 50, CalculateStoryFuel(new Vector2(-500, 400), Vector2.zero), "HQ-Alpha");
                sfEnemy.personality = PilotPersonality.Cold;
                sfEnemy.spokenCargo = "Reinforcements";
                sfEnemy.customAnswerCargo = "We are the reinforcements requested by the Director. Authentication code: MKPU.";
                scriptedFlightsQueue.Enqueue(sfEnemy);
                scriptedDelaysQueue.Enqueue(10f);

                FlightData sfFriend = new FlightData("TR-11", new Vector2(500, 300), Vector2.zero, new List<Vector2>(), 75f, "People", 50, "People", 50, CalculateStoryFuel(new Vector2(500, 300), Vector2.zero), "HQ-Alpha");
                sfFriend.personality = PilotPersonality.Aggressive;
                sfFriend.spokenCargo = "Reinforcements";
                sfFriend.customAnswerCargo = "We are the reinforcements requested by the Director. Authentication code: MKPW.";
                scriptedFlightsQueue.Enqueue(sfFriend);
                scriptedDelaysQueue.Enqueue(15f);

                FlightData fl55 = new FlightData("GE-55", new Vector2(-600, 0), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 250, "Fuel", 250, CalculateStoryFuel(new Vector2(-600, 0), Vector2.zero), "Bastion-3");
                fl55.personality = PilotPersonality.Standard;
                scriptedFlightsQueue.Enqueue(fl55);
                scriptedDelaysQueue.Enqueue(20f);

                FlightData fd42 = new FlightData("GE-42", new Vector2(400, -200), Vector2.zero, new List<Vector2>(), 80f, "Food", 300, "Food", 300, CalculateStoryFuel(new Vector2(400, -200), Vector2.zero), "Agri-Center");
                fd42.personality = PilotPersonality.Nervous;
                scriptedFlightsQueue.Enqueue(fd42);
                scriptedDelaysQueue.Enqueue(15f);

            }
        }
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
        
        CheckDepartureReadiness(flight);
    }

    private void CompleteRefuel(FlightData flight)
    {
        flight.isRefueling = false;
        flight.isRefueled = true;

        int neededFuel = flight.planeMaxFuel - Mathf.RoundToInt(flight.currentFuel);
        int actualFuelTaken = Mathf.Min(neededFuel, totalFuel);

        totalFuel -= actualFuelTaken;
        flight.currentFuel += actualFuelTaken;
        
        CheckDepartureReadiness(flight);
    }

    private void CompleteRepair(FlightData flight)
    {
        flight.isRepairing = false;
        flight.isRepaired = true;
        
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

    public void MarkFlightAsLanded(string callsign)
    {
        var flight = savedFlights.Find(f => f.callsign == callsign);
        if (flight != null)
        {
            flight.hasLanded = true;
            landedPlanes++;

            // Логика обслуживания при посадке:
            // Если топлива больше 50% - его нужно починить (поэтому заправка уже "выполнена")
            // Если топлива меньше или равно 50% - его нужно заправить (поэтому ремонт уже "выполнен")
            float fuelPercentage = (flight.currentFuel / flight.planeMaxFuel) * 100f;
            if (fuelPercentage > 50f)
            {
                flight.isRefueled = true;
                flight.isRepaired = false;
            }
            else
            {
                flight.isRefueled = false;
                flight.isRepaired = true;
            }

            if (callsign == "TR-88" || callsign == "GE-98")
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
            flight.hasLanded = false; // Самолёт снова в воздухе
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
        List<FlightData> servicedPlanes = new List<FlightData>();
        List<FlightData> preservedPlanes = new List<FlightData>();

        Debug.Log($"<color=yellow>Checking {savedFlights.Count} flights for serviced planes...</color>");

        int preservedLandedCount = 0;

        foreach (var flight in savedFlights)
        {
            Debug.Log($"<color=cyan>Flight {flight.callsign}: hasLanded={flight.hasLanded}, isUnloaded={flight.isUnloaded}, isRefueled={flight.isRefueled}, isRepaired={flight.isRepaired}, isReadyToDepart={flight.isReadyToDepart}, isDeparting={flight.isDeparting}</color>");

            // Сохраняем приземлившиеся самолеты, которые НЕ вылетели в эту смену:
            // - частично или полностью обслуженные (на следующий день появятся в Departures)
            // - NOT те, кто уже взлетел (isDeparting)
            if (flight.hasLanded && !flight.isDeparting)
            {
                preservedPlanes.Add(flight);
                preservedLandedCount++;
                if (flight.isUnloaded && flight.isRefueled && flight.isRepaired)
                {
                    Debug.Log($"<color=green>Preserving FULLY SERVICED plane for next-day departure: {flight.callsign}</color>");
                    // Сбрасываем isReadyToDepart — на следующий день CheckDepartureReadiness сам выставит его
                    flight.isReadyToDepart = false;
                }
                else
                {
                    Debug.Log($"<color=orange>Preserving UNSERVICED landed plane on base for next day: {flight.callsign}</color>");
                }
            }
        }

        Debug.Log($"<color=yellow>Total serviced planes to depart: {servicedPlanes.Count}</color>");
        Debug.Log($"<color=yellow>Total unserviced/departing planes preserved: {preservedPlanes.Count}</color>");

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

        UIAirplane[] leftoverPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        foreach (var plane in leftoverPlanes)
        {
            if (plane != null) AirplaneSpawner.Instance.ReturnPlaneToPool(plane);
        }
        if (RadarScreenClicker.selectedPlane != null) RadarScreenClicker.selectedPlane = null;
        RadioManager.activeCallsign = "";

        isShiftActive = false;
        scriptedFlightsQueue.Clear();
        scriptedDelaysQueue.Clear();
    }

    public void LoadState(SaveData data)
    {
        this.isShiftActive = data.isShiftActive;
        this.globalSpawnTimer = data.globalSpawnTimer;
        
        // Фильтруем дубликаты из старых битых сохранений
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
    }
}