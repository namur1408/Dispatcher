using UnityEngine;

public class AirplaneFuel
{
    private UIAirplane _plane;
    
    public float currentFuel;
    public bool isOutOfFuel;
    public float emergencyTimer;
    public float distancePerFuelUnit;

    public float fuelRangeFromRouteOrigin { get; private set; }
    public Vector2 routeOriginPosition { get; private set; }
    private Vector2 _lastPosition;

    public AirplaneFuel(UIAirplane plane, float distPerFuel)
    {
        _plane = plane;
        currentFuel = 100f; // Default, will be overwritten by InitializeFromData
        emergencyTimer = 20f;
        distancePerFuelUnit = distPerFuel;
    }

    public void InitFromData(float fuel)
    {
        currentFuel = fuel;
        isOutOfFuel = (currentFuel <= 0);
        RecalcFuelRange();
    }

    public void ResetFuel()
    {
        isOutOfFuel = false;
        emergencyTimer = 20f;
        currentFuel = 100f;
    }

    public void SetLastPosition(Vector2 pos)
    {
        _lastPosition = pos;
    }

    public void RecalcFuelRange()
    {
        routeOriginPosition = _plane.logicalPosition;
        fuelRangeFromRouteOrigin = currentFuel * distancePerFuelUnit;
    }

    public void HandleFuelConsumption(float actualSpeed)
    {
        Vector2 logicalPos = _plane.logicalPosition;
        float distanceMoved = Vector2.Distance(logicalPos, _lastPosition);
        _lastPosition = logicalPos;

        if (isOutOfFuel || distanceMoved <= 0) return;

        float fuelConsumed = distanceMoved / distancePerFuelUnit;
        currentFuel -= fuelConsumed;

        if (currentFuel <= 0)
        {
            currentFuel = 0;
            isOutOfFuel = true;
            _plane.UpdateInternalSpeed(); // Requires actualSpeed *= 0.3f inside UIAirplane
            _plane.UpdateHitboxColor();
        }
    }

    public void HandleFuelEmergency(float deltaTime)
    {
        if (!isOutOfFuel) return;

        emergencyTimer -= deltaTime;

        // Flashing MAYDAY
        string targetText = (Mathf.FloorToInt(Time.time * 3) % 2 == 0) ? "MAYDAY" : "";
        if (_plane.callsignText != null && _plane.callsignText.text != targetText) 
        {
            _plane.callsignText.text = targetText;
        }

        if (emergencyTimer > 0) return;

        // Time's up - the board is lost
        if (FlightDataManager.Instance != null)
        {
            var fd = FlightDataManager.Instance.GetFlight(_plane.originalCallsign);
            if (fd != null && AirplaneSpawner.Instance != null)
                AirplaneSpawner.Instance.NotifyPlaneCrashed(fd);
            FlightDataManager.Instance.RemoveDepartedPlane(_plane.originalCallsign);
        }

        if (RadarScreenClicker.selectedPlane == _plane)
        {
            if (BigRadarTerminal.Instance != null) BigRadarTerminal.Instance.ClearSelection();
        }

        if (AirplaneSpawner.Instance != null)
        {
            AirplaneSpawner.Instance.ReturnPlaneToPool(_plane);
        }
        else
        {
            Object.Destroy(_plane.gameObject);
        }
    }
}
