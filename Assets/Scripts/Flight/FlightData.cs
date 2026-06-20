using UnityEngine;
using System.Collections.Generic;

public enum PilotPersonality
{
    Standard,
    Aggressive,
    Nervous,
    Cold,
    Desperate
}

[System.Serializable]
public class FlightData
{
    public string callsign;
    public PilotPersonality personality = PilotPersonality.Standard;
    public Vector2 position;
    public Vector2 targetPosition;
    public List<Vector2> savedWaypoints = new List<Vector2>();

    // Для сериализации JsonUtility, так как List<Vector2> не сериализуется корректно
    public List<float> waypointXs = new List<float>();
    public List<float> waypointYs = new List<float>();

    public float speed;
    public float currentFuel;
    public int planeMaxFuel = 250;
    public string status = "APPROACHING";

    // Runway & Departure mechanics
    public string assignedRunway = "";
    public bool isAligningToLand = false;
    public bool isDeparting = false;
    public bool hasTakenOff = false;
    public string departureDestination = "";

    public string cargo;
    public int cargoAmount;

    public string manifestCargo;
    public int manifestCargoAmount;
    public string manifestOrigin;

    public string spokenCargo = "";
    public string spokenOrigin = "";
    public string spokenWeight = "";
    public string spokenSpeed = "";
    public string customExplanation = "";
    public string explanationCargo = "";
    public string explanationOrigin = "";
    public string explanationWeight = "";
    public string explanationSpeed = "";

    // Кастомные ответы на конкретные вопросы (если пусто — используется авто-фраза)
    public string customAnswerCargo = "";
    public string customAnswerOrigin = "";
    public string customAnswerWeight = "";
    public string customAnswerSpeed = "";

    // Кастомные вопросы диспетчера (если пусто — используется авто-фраза)
    public string customQuestionCargo = "";
    public string customQuestionOrigin = "";
    public string customQuestionWeight = "";
    public string customQuestionSpeed = "";

    public bool isCargoKnown = false;
    public bool decisionMade = false;
    public bool approved = false;
    public bool hasLanded = false;
    public bool hasBeenPinged = false;
    public string chatHistory = "";

    public bool isInStorm = false;

    public bool askedCargo;
    public bool askedOrigin;
    public bool askedWeight;
    public bool askedSpeed;

    public bool isFolderTorn = false;
    public Vector2 manifestPos = new Vector2(-380, 80);
    public Vector2 radarPos = new Vector2(-150, -20);
    public Vector2 cheatSheetPos = new Vector2(210, 140);
    public Vector2 pilotReportPos = new Vector2(100, -120);

    public bool isUnloading = false;
    public bool isUnloaded = false;
    public float unloadTimer = 0f;

    public void StartUnloading(float duration) { if (!isUnloaded) { isUnloading = true; unloadTimer = duration; } }
    public void UpdateUnloadTimer(float dt) { unloadTimer -= dt; }
    public void CompleteUnload() { isUnloading = false; isUnloaded = true; }

    public bool isRefueling = false;
    public bool isRefueled = false;
    public float refuelTimer = 0f;

    public void StartRefueling(float duration) { if (!isRefueled) { isRefueling = true; refuelTimer = duration; } }
    public void UpdateRefuelTimer(float dt) { refuelTimer -= dt; }
    public void CompleteRefuel() { isRefueling = false; isRefueled = true; }
    public void SkipRefuel() { isRefueled = true; isRefueling = false; }

    public bool isRepairing = false;
    public bool isRepaired = false;
    public float repairTimer = 0f;

    public void StartRepairing(float duration) { if (!isRepaired) { isRepairing = true; repairTimer = duration; } }
    public void UpdateRepairTimer(float dt) { repairTimer -= dt; }
    public void CompleteRepair() { isRepairing = false; isRepaired = true; }
    public void SkipRepair() { isRepaired = true; isRepairing = false; }

    // Самолёт обслужен и ожидает назначения полосы вылета через панель Departures.
    // При isReadyToDepart=true самолёт отображается в Departures, но НЕ спавнится автоматически.
    public bool isReadyToDepart = false;

    public int arrivalDay = 0;

    public bool isTakingOff = false;
    public Vector2 takeoffStartPos;

    public bool isLandingPhase = false;

    public FlightData(string cs, Vector2 pos, Vector2 target, List<Vector2> wps, float spd, string cg, int cgAmount = -1)
    {
        callsign = cs;
        position = pos;
        targetPosition = target;
        savedWaypoints = wps;
        speed = spd;
        cargo = cg;
        cargoAmount = cgAmount == -1 ? Random.Range(10, 100) : cgAmount;
        currentFuel = Random.Range(100, planeMaxFuel);

        manifestCargo = cargo;
        manifestCargoAmount = cargoAmount;
        manifestOrigin = "Bastion-" + Random.Range(1, 10);
        
        arrivalDay = StoryManager.currentDay;
        UpdateSerializedWaypoints();
    }

    public FlightData(string cs, Vector2 pos, Vector2 target, List<Vector2> wps, float spd,
                      string realCargo, int realAmount,
                      string fakeCargo, int fakeAmount, float fuel,
                      string originPort)
    {
        callsign = cs;
        position = pos;
        targetPosition = target;
        savedWaypoints = wps;
        speed = spd;
        cargo = realCargo;
        cargoAmount = realAmount;
        currentFuel = fuel;

        manifestCargo = fakeCargo;
        manifestCargoAmount = fakeAmount;
        manifestOrigin = originPort;

        arrivalDay = StoryManager.currentDay;
        UpdateSerializedWaypoints();
    }

    public void UpdateSerializedWaypoints()
    {
        waypointXs.Clear();
        waypointYs.Clear();
        if (savedWaypoints != null)
        {
            foreach (var wp in savedWaypoints)
            {
                waypointXs.Add(wp.x);
                waypointYs.Add(wp.y);
            }
        }
    }

    public void ReconstructWaypoints()
    {
        savedWaypoints.Clear();
        if (waypointXs != null && waypointYs != null)
        {
            for (int i = 0; i < Mathf.Min(waypointXs.Count, waypointYs.Count); i++)
            {
                savedWaypoints.Add(new Vector2(waypointXs[i], waypointYs[i]));
            }
        }
    }

    // Для загрузки уже существующих
    public FlightData() 
    { 
        savedWaypoints = new List<Vector2>();
        waypointXs = new List<float>();
        waypointYs = new List<float>();
    }
}