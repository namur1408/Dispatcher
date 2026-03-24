using UnityEngine;

[System.Serializable]
public class FlightData
{
    public string callsign;
    public Vector2 position;
    public Vector2 target;
    public float speed;
    public string status;
    public bool decisionMade;   // было ли принято решение
    public bool approved;       // true = разрешено, false = запрещено

    public FlightData(string callsign, Vector2 position, Vector2 target, float speed)
    {
        this.callsign     = callsign;
        this.position     = position;
        this.target       = target;
        this.speed        = speed;
        this.status       = (target == Vector2.zero) ? "APPROACHING" : "TRANSIT";
        this.decisionMade = false;
        this.approved     = false;
    }
}
