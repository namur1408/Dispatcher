using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FlightData
{
    public string callsign;
    public Vector2 position;
    public Vector2 targetPosition;
    public List<Vector2> savedWaypoints = new List<Vector2>();
    public float speed;
    public float currentFuel;
    public int planeMaxFuel = 250;
    public string status = "APPROACHING";

    // Runway & Departure mechanics
    public string assignedRunway = "";
    public bool isAligningToLand = false;
    public bool isDeparting = false;
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
    public bool isInterrogationFinished;

    public bool isFolderTorn = false;
    public Vector2 manifestPos = new Vector2(-380, 80);
    public Vector2 radarPos = new Vector2(-150, -20);
    public Vector2 cheatSheetPos = new Vector2(210, 140);
    public Vector2 pilotReportPos = new Vector2(100, -120);

    public bool isUnloading;
    public bool isUnloaded;
    public float unloadTimer;

    public bool isRefueling;
    public bool isRefueled;
    public float refuelTimer;

    public bool isRepairing;
    public bool isRepaired;
    public float repairTimer;

    // Самолёт обслужен и ожидает назначения полосы вылета через панель Departures.
    // При isReadyToDepart=true самолёт отображается в Departures, но НЕ спавнится автоматически.
    public bool isReadyToDepart = false;

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
    }
}