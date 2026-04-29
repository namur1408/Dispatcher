using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FlightData
{
    public string callsign;
    public Vector2 position;
    public Vector2 target;
    public List<Vector2> savedWaypoints;
    public float speed;
    public string status;
    public string cargo;
    public int cargoAmount;
    public bool decisionMade;
    public bool approved;
    public bool hasLanded;
    public bool hasBeenPinged;

    public bool isUnloaded;
    public bool isRefueled;
    public int currentFuel;
    public int planeMaxFuel;
    public bool isRefueling;
    public float refuelTimer;

    public bool isUnloading;
    public float unloadTimer;

    public bool isRepaired;
    public bool isRepairing;
    public float repairTimer;
    public FlightData(string callsign, Vector2 position, Vector2 target, List<Vector2> incomingWaypoints, float speed, string cargo)
    {
        this.callsign = callsign;
        this.position = position;
        this.target = target;
        this.savedWaypoints = new List<Vector2>(incomingWaypoints);
        this.speed = speed;
        this.cargo = cargo;
        this.hasBeenPinged = false;

        if (cargo == "Medicines") this.cargoAmount = Random.Range(1, 4);
        else if (cargo == "People") this.cargoAmount = Random.Range(30, 71);
        else if (cargo == "Food") this.cargoAmount = Random.Range(100, 251);
        else if (cargo == "Fuel") this.cargoAmount = Random.Range(200, 451);
        else this.cargoAmount = 0;

        this.status = (target == Vector2.zero) ? "APPROACHING" : "TRANSIT";
        this.decisionMade = false;
        this.approved = false;
        this.hasLanded = false;
        this.isUnloaded = false;
        this.isUnloading = false;
        this.unloadTimer = 0f;

        this.planeMaxFuel = Random.Range(300, 500);

        if (this.cargo == "Fuel")
        {
            this.currentFuel = Random.Range(this.planeMaxFuel - 15, this.planeMaxFuel + 1); 
            this.isRefueled = true;
            this.isRepaired = false;
        }
        else
        {
            this.currentFuel = this.planeMaxFuel / 2;
            this.isRefueled = false;
            this.isRepaired = true;
        }

        this.isRefueling = false;
        this.refuelTimer = 0f;

        this.isRepairing = false;
        this.repairTimer = 0f;
    }
}