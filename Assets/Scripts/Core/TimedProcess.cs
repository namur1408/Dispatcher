/// <summary>
/// Encapsulates the state of one timed process (unloading, refueling, repair).
/// Replaces the triple duplicate isXxx / isXxxed / xxxTimer field block in FlightData.
/// Compatible with JsonUtility: all fields are public.
/// </summary>
[System.Serializable]
public class TimedProcess
{
    public bool isActive;
    public bool isComplete;
    public float timer;

    /// <summary>
    /// Starts a process for a specified duration. The repeated call is ignored if the process has already completed.
    /// </summary>
    public void Start(float duration)
    {
        if (!isComplete)
        {
            isActive   = true;
            timer      = duration;
        }
    }

    /// <summary>
    /// Advances the timer by dt seconds.
    /// Returns true when completed (one time).
    /// </summary>
    public bool Tick(float dt)
    {
        if (!isActive) return false;

        timer -= dt;
        if (timer <= 0f)
        {
            isActive   = false;
            isComplete = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Instantly marks the process as completed (for example, the plane landed with a full tank).
    /// </summary>
    public void Skip()
    {
        isActive   = false;
        isComplete = true;
    }

    /// <summary>Resets the process to its initial state.</summary>
    public void Reset()
    {
        isActive   = false;
        isComplete = false;
        timer      = 0f;
    }
}
