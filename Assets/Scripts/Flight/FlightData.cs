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

    // For JsonUtility serialization since List<Vector2> is not serializing correctly
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

    // Custom answers to specific questions (if empty, an auto-phrase is used)
    public string customAnswerCargo = "";
    public string customAnswerOrigin = "";
    public string customAnswerWeight = "";
    public string customAnswerSpeed = "";

    // Custom dispatcher questions (if empty, an auto-phrase is used)
    public string customQuestionCargo = "";
    public string customQuestionOrigin = "";
    public string customQuestionWeight = "";
    public string customQuestionSpeed = "";

    public bool decisionMade = false;
    public bool approved = false;
    public bool hasLanded = false;
    public bool hasBeenPinged = false;

    public bool isInStorm = false;

    // ──────────────────────────────────────────────────────────
    // Ground maintenance processes (unloading / refueling / repairs)
    // Each process is encapsulated in a TimedProcess.
    // Wrapper properties are retained for compatibility with existing code.
    // ──────────────────────────────────────────────────────────
    public TimedProcess unloading = new TimedProcess();
    public TimedProcess refueling = new TimedProcess();
    public TimedProcess repairing = new TimedProcess();

    // Wrappers for convenience (use where you just need to read the state)
    public bool isUnloading  => unloading.isActive;
    public bool isUnloaded   => unloading.isComplete;
    public bool isRefueling  => refueling.isActive;
    public bool isRefueled   => refueling.isComplete;
    public bool isRepairing  => repairing.isActive;
    public bool isRepaired   => repairing.isComplete;

    // Wrapper methods - preserve the same public API
    public void StartUnloading(float duration)  => unloading.Start(duration);
    public void StartRefueling(float duration)  => refueling.Start(duration);
    public void StartRepairing(float duration)  => repairing.Start(duration);
    public void SkipRefuel()                    => refueling.Skip();
    public void SkipRepair()                    => repairing.Skip();

    // The aircraft has been serviced and is awaiting departure runway assignment through the Departures panel.
    // When isReadyToDepart=true, the aircraft is displayed in Departures, but will NOT spawn automatically.
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

    // To download existing ones
    public FlightData() 
    { 
        savedWaypoints = new List<Vector2>();
        waypointXs = new List<float>();
        waypointYs = new List<float>();
    }
}