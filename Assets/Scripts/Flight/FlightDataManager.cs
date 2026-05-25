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

                        // Immediately update its UIAirplane instance on the radar so it turns around
                        if (RadarManager.Instance != null && RadarManager.Instance.activeAirplanes != null)
                        {
                            var plane = RadarManager.Instance.activeAirplanes.Find(p => p != null && p.callsignText != null && p.callsignText.text == flight.callsign);
                            if (plane != null)
                            {
                                plane.Deny();
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
            if (oldFlight.isReadyToDepart || (oldFlight.decisionMade && oldFlight.approved && oldFlight.hasLanded))
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
            FlightData ge102 = new FlightData("GE-102", new Vector2(-535, 119), Vector2.zero, new List<Vector2>(), 80f, "Fuel", 200, "Fuel", 200, 250f, "Bastion-3");
            scriptedFlightsQueue.Enqueue(ge102);
            scriptedDelaysQueue.Enqueue(15f);

            FlightData qy884 = new FlightData("QY-884", new Vector2(437, -357), Vector2.zero, new List<Vector2>(), 95f, "Food", 200, "Food", 200, 250f, "Bastion-5");
            scriptedFlightsQueue.Enqueue(qy884);
            scriptedDelaysQueue.Enqueue(20f);

            FlightData tr404 = new FlightData("TR-404", new Vector2(0, 535), Vector2.zero, new List<Vector2>(), 75f, "People", 65, "Food", 50, 100f, "Sector-Z");
            tr404.spokenCargo = "Food";
            tr404.spokenOrigin = "Bastion-4";
            tr404.explanationOrigin = "Sector Z has been destroyed, Control. We barely managed to escape! We probably made a mistake in the rush.";
            tr404.explanationCargo = "Listen, we've had to reclassify the cargo just to stay safe, we're completely out of fuel, and we're about to crash! We have refugees on board. Please let us through — there are children on board!";
            scriptedFlightsQueue.Enqueue(tr404);
            scriptedDelaysQueue.Enqueue(20f);

            FlightData ge201 = new FlightData("GE-201", new Vector2(-416, 476), Vector2.zero, new List<Vector2>(), 84f, "Fuel", 150, "Fuel", 150, 250f, "Bastion-1");
            scriptedFlightsQueue.Enqueue(ge201);
            scriptedDelaysQueue.Enqueue(15f);

            FlightData ge305 = new FlightData("GE-305", new Vector2(-200, -500), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 100, "Fuel", 100, 250f, "Bastion-2");
            scriptedFlightsQueue.Enqueue(ge305);
            scriptedDelaysQueue.Enqueue(15f);
        }
        else if (dayNumber == 2)
        {
            bool letRefugeesIn = PlayerPrefs.GetInt("Trigger_Engineer", 0) == 1;

            if (letRefugeesIn) // Branch B (Engineer saved)
            {
                FlightData md01 = new FlightData("QY-01", new Vector2(-400, -400), Vector2.zero, new List<Vector2>(), 95f, "Medicines", 10, "Medicines", 10, 200f, "Med-Base 4");
                scriptedFlightsQueue.Enqueue(md01);
                scriptedDelaysQueue.Enqueue(15f);

                // Спецтехника (EQ-99 -> GE-99) прилетает только если мы приняли инженера!
                FlightData eq99 = new FlightData("GE-99", new Vector2(0, 600), Vector2.zero, new List<Vector2>(), 80f, "Equipment", 5, "Equipment", 5, 250f, "Eng-Hub");
                scriptedFlightsQueue.Enqueue(eq99);
                scriptedDelaysQueue.Enqueue(20f);

                FlightData fl55 = new FlightData("GE-55", new Vector2(-600, 0), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 250, "Fuel", 250, 300f, "Bastion-3");
                scriptedFlightsQueue.Enqueue(fl55);
                scriptedDelaysQueue.Enqueue(20f);

                FlightData fd42 = new FlightData("GE-42", new Vector2(400, -200), Vector2.zero, new List<Vector2>(), 80f, "Food", 300, "Food", 300, 250f, "Agri-Center");
                scriptedFlightsQueue.Enqueue(fd42);
                scriptedDelaysQueue.Enqueue(15f);
            }
            else // Branch A (No Engineer)
            {
                // Friend SF (TR passenger planes)
                FlightData sfFriend = new FlightData("TR-11", new Vector2(500, 300), Vector2.zero, new List<Vector2>(), 75f, "People", 50, "People", 50, 150f, "HQ-Alpha");
                sfFriend.spokenCargo = "We are the reinforcements requested by the Director. Authentication code: QYEW.";
                scriptedFlightsQueue.Enqueue(sfFriend);
                scriptedDelaysQueue.Enqueue(10f);

                // Enemy SF
                FlightData sfEnemy = new FlightData("TR-88", new Vector2(-500, 400), Vector2.zero, new List<Vector2>(), 78f, "People", 50, "People", 50, 150f, "HQ-Alpha");
                sfEnemy.spokenCargo = "We are the reinforcements requested by the Director. Authentication code: QYEV.";
                scriptedFlightsQueue.Enqueue(sfEnemy);
                scriptedDelaysQueue.Enqueue(20f);

                FlightData fl55 = new FlightData("GE-55", new Vector2(-600, 0), Vector2.zero, new List<Vector2>(), 82f, "Fuel", 250, "Fuel", 250, 300f, "Bastion-3");
                scriptedFlightsQueue.Enqueue(fl55);
                scriptedDelaysQueue.Enqueue(20f);

                FlightData fd42 = new FlightData("GE-42", new Vector2(400, -200), Vector2.zero, new List<Vector2>(), 80f, "Food", 300, "Food", 300, 250f, "Agri-Center");
                scriptedFlightsQueue.Enqueue(fd42);
                scriptedDelaysQueue.Enqueue(15f);
            }
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

                if (RadarManager.Instance != null && RadarManager.Instance.activeAirplanes != null)
                {
                    var plane = RadarManager.Instance.activeAirplanes.Find(p => p != null && p.originalCallsign == callsign);
                    if (plane != null)
                    {
                        if (isApproved) plane.Approve();
                        else plane.Deny();
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
        List<FlightData> preservedPlanes = new List<FlightData>();

        Debug.Log($"<color=yellow>Checking {savedFlights.Count} flights for serviced planes...</color>");

        int preservedLandedCount = 0;

        foreach (var flight in savedFlights)
        {
            Debug.Log($"<color=cyan>Flight {flight.callsign}: hasLanded={flight.hasLanded}, isUnloaded={flight.isUnloaded}, isRefueled={flight.isRefueled}, isRepaired={flight.isRepaired}, isReadyToDepart={flight.isReadyToDepart}, isDeparting={flight.isDeparting}</color>");

            // 1. Если самолет готов к вылету или уже вылетает, сохраняем его на следующий день как вылетающий
            if (flight.isReadyToDepart || flight.isDeparting)
            {
                preservedPlanes.Add(flight);
                Debug.Log($"<color=green>Preserving departing flight for next day: {flight.callsign}</color>");
            }
            // 2. Если самолет приземлился:
            else if (flight.hasLanded)
            {
                // Если он полностью обслужен, он улетает (переходит в Departures)
                if (flight.isUnloaded && flight.isRefueled && flight.isRepaired)
                {
                    servicedPlanes.Add(flight);
                    Debug.Log($"<color=green>Found serviced plane for departure: {flight.callsign}</color>");
                }
                // Если НЕ полностью обслужен, он остается на базе
                else
                {
                    preservedPlanes.Add(flight);
                    preservedLandedCount++;
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

    public void LoadState(SaveData data)
    {
        this.isShiftActive = data.isShiftActive;
        this.globalSpawnTimer = data.globalSpawnTimer;
        
        this.savedFlights = data.savedFlights;
        
        this.scriptedFlightsQueue = new Queue<FlightData>(data.pendingFlights);
        this.scriptedDelaysQueue = new Queue<float>(data.pendingDelays);
        
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

    private void EnqueueDepartingPlanes(List<FlightData> servicedPlanes)
    {
        // Вместо постановки в очередь спавна, помечаем самолёты как "готовые к вылету".
        // Они появятся в панели Departures на радаре — спавн произойдёт только после
        // того, как игрок назначит им полосу вылета.
        foreach (var plane in servicedPlanes)
        {
            FlightData departingPlane = new FlightData(
                plane.callsign,
                Vector2.zero,
                Vector2.zero,
                new List<Vector2>(),
                plane.speed,
                "None",
                0
            );
            departingPlane.currentFuel = plane.planeMaxFuel;
            departingPlane.planeMaxFuel = plane.planeMaxFuel;
            departingPlane.manifestCargo = "None";
            departingPlane.manifestCargoAmount = 0;
            // Используем manifestOrigin предыдущей смены как "пункт назначения" для UI
            departingPlane.manifestOrigin = plane.manifestOrigin;
            departingPlane.departureDestination = string.IsNullOrEmpty(plane.manifestOrigin) ? "UNKNOWN" : plane.manifestOrigin;

            // Ключевой флаг: самолёт ждёт назначения полосы через Departures
            departingPlane.isReadyToDepart = true;
            departingPlane.hasLanded = true;
            departingPlane.isUnloaded = true;
            departingPlane.isRefueled = true;
            departingPlane.isRepaired = true;

            savedFlights.Add(departingPlane);
            Debug.Log($"<color=cyan>Marked ready-to-depart: {departingPlane.callsign} → destination: {departingPlane.departureDestination}</color>");
        }

        Debug.Log($"<color=cyan>Total ready-to-depart planes in savedFlights: {savedFlights.Count}</color>");
    }
}