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
    public bool decisionMade;   
    public bool approved;

    public FlightData(string callsign, Vector2 position, Vector2 target, List<Vector2> incomingWaypoints, float speed)
    {
        this.callsign     = callsign;
        this.position     = position;
        this.target       = target;
        this.savedWaypoints = new List<Vector2>(incomingWaypoints);
        this.speed        = speed;
        this.status       = (target == Vector2.zero) ? "APPROACHING" : "TRANSIT";
        this.decisionMade = false;
        this.approved     = false;
    }
}
