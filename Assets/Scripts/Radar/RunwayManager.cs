using UnityEngine;
using System.Collections.Generic;

public class RunwayManager : SingletonMB<RunwayManager>
{
    public List<Runway> runways = new List<Runway>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
    }

    private void OnEnable()
    {
        GameEvents.OnFlightLanded += HandleFlightLanded;
    }

    private void OnDisable()
    {
        GameEvents.OnFlightLanded -= HandleFlightLanded;
    }

    private void HandleFlightLanded(FlightData flight)
    {
        if (flight != null && !string.IsNullOrEmpty(flight.assignedRunway))
        {
            OccupyRunway(flight.assignedRunway, FlightConstants.UnloadTime);
        }
    }

    public void RegisterRunway(Runway rw)
    {
        if (!runways.Contains(rw))
        {
            runways.Add(rw);
        }
    }

    public Runway GetRunwayByID(string id)
    {
        foreach (var rw in runways)
        {
            if (rw.id1 == id || rw.id2 == id)
                return rw;
        }
        return null;
    }

    public bool IsRunwayOccupied(string id)
    {
        Runway rw = GetRunwayByID(id);
        if (rw != null)
        {
            return rw.isOccupied;
        }
        return true; // Assume occupied if not found to be safe
    }

    public void OccupyRunway(string id, float time)
    {
        Runway rw = GetRunwayByID(id);
        if (rw != null)
        {
            rw.SetOccupied(time);
        }
    }
}
