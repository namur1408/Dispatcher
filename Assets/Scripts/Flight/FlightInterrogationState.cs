using UnityEngine;

[System.Serializable]
public class FlightInterrogationState
{
    public string callsign;
    
    public string chatHistory = "";
    
    public bool isCargoKnown = false;
    
    public bool askedCargo = false;
    public bool askedOrigin = false;
    public bool askedWeight = false;
    public bool askedSpeed = false;
    
    public bool isFolderTorn = false;
    
    public Vector2 manifestPos = new Vector2(-380, 80);
    public Vector2 radarPos = new Vector2(-150, -20);
    public Vector2 cheatSheetPos = new Vector2(210, 140);
    public Vector2 pilotReportPos = new Vector2(100, -120);

    public FlightInterrogationState(string callsign)
    {
        this.callsign = callsign;
    }
}
