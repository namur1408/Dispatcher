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

    public bool isUnloaded;
    public bool isRefueled;
    public int currentFuel;
    public int planeMaxFuel;

    public bool isUnloading;
    public float unloadTimer;
    public bool isRefueling;
    public float refuelTimer;

    public FlightData(string callsign, Vector2 position, Vector2 target, List<Vector2> incomingWaypoints, float speed, string cargo)
    {
        this.callsign = callsign;
        this.position = position;
        this.target = target;
        this.savedWaypoints = new List<Vector2>(incomingWaypoints);
        this.speed = speed;
        this.cargo = cargo;

        if (cargo == "Medicines") this.cargoAmount = Random.Range(1, 4);
        else if (cargo == "People") this.cargoAmount = Random.Range(30, 71);
        else if (cargo == "Food") this.cargoAmount = Random.Range(100, 251);
        else if (cargo == "Fuel") this.cargoAmount = Random.Range(200, 451);
        else this.cargoAmount = 0;

        this.status = (target == Vector2.zero) ? "APPROACHING" : "TRANSIT";
        this.decisionMade = false;
        this.approved = false;

        this.isUnloaded = false;
        this.isRefueled = false;
        this.planeMaxFuel = Random.Range(300, 500);
        this.currentFuel = this.planeMaxFuel / 2;

        this.isUnloading = false;
        this.unloadTimer = 0f;
        this.isRefueling = false;
        this.refuelTimer = 0f;
    }
}