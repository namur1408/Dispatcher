using System;
using UnityEngine;

/// <summary>
/// Global event bus for decoupled communication between managers.
/// Replaces direct Singleton calls (e.g., PauseManager calling RadarManager).
/// </summary>
public static class GameEvents
{
    // ==========================================
    // FLIGHT EVENTS
    // ==========================================
    
    /// <summary>Invoked when a flight successfully lands and unloads.</summary>
    public static event Action<FlightData> OnFlightLanded;
    public static void FlightLanded(FlightData data) => OnFlightLanded?.Invoke(data);

    /// <summary>Invoked when a flight is denied landing/entry.</summary>
    public static event Action<FlightData> OnFlightDenied;
    public static void FlightDenied(FlightData data) => OnFlightDenied?.Invoke(data);

    /// <summary>Invoked when a flight crashes or runs out of fuel.</summary>
    public static event Action<FlightData> OnFlightCrashed;
    public static void FlightCrashed(FlightData data) => OnFlightCrashed?.Invoke(data);
    
    /// <summary>Invoked when a flight successfully departs the airspace.</summary>
    public static event Action<FlightData> OnFlightDeparted;
    public static void FlightDeparted(FlightData data) => OnFlightDeparted?.Invoke(data);

    /// <summary>Invoked when a plane finishes all ground services and is ready for departure assignment.</summary>
    public static event Action<FlightData> OnFlightReadyToDepart;
    public static void FlightReadyToDepart(FlightData data) => OnFlightReadyToDepart?.Invoke(data);

    // ==========================================
    // RESOURCE EVENTS
    // ==========================================
    
    /// <summary>Invoked when base resources (Fuel, Food, People, Meds) change.</summary>
    public static event Action OnResourcesChanged;
    public static void ResourcesChanged() => OnResourcesChanged?.Invoke();

    // ==========================================
    // SYSTEM & TIME EVENTS
    // ==========================================
    
    public static event Action<int> OnDayStarted;
    public static void DayStarted(int day) => OnDayStarted?.Invoke(day);

    public static event Action<int> OnDayEnded;
    public static void DayEnded(int day) => OnDayEnded?.Invoke(day);

    public static event Action OnGamePaused;
    public static void GamePaused() => OnGamePaused?.Invoke();

    public static event Action OnGameResumed;
    public static void GameResumed() => OnGameResumed?.Invoke();
}
