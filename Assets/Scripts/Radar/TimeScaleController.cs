using UnityEngine;

public class TimeScaleController : MonoBehaviour
{
    public static TimeScaleController Instance;

    [Header("Time Scale Settings")]
    public float normalSpeed = 1f;
    public float fastSpeed = 2f;
    public float veryFastSpeed = 4f;

    private float currentSpeed = 1f;
    private int speedLevel = 0;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // If external code (like a tutorial resuming) explicitly sets Time.timeScale to 1f, 
        // we should sync our internal state so the UI updates correctly to 1x.
        if (Time.timeScale == 1f && currentSpeed != 1f)
        {
            speedLevel = 0;
            currentSpeed = 1f;
        }
    }

    void Start()
    {
        SetTimeScale(normalSpeed);
    }

    public void IncreaseSpeed()
    {
        if (Time.timeScale == 0f) return;

        speedLevel++;
        if (speedLevel > 2) speedLevel = 2;

        UpdateSpeed();
    }

    public void DecreaseSpeed()
    {
        if (Time.timeScale == 0f) return;

        speedLevel--;
        if (speedLevel < 0) speedLevel = 0;

        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        switch (speedLevel)
        {
            case 0:
                SetTimeScale(normalSpeed);
                break;
            case 1:
                SetTimeScale(fastSpeed);
                break;
            case 2:
                SetTimeScale(veryFastSpeed);
                break;
        }
    }

    public void SetTimeScale(float scale)
    {
        currentSpeed = scale;
        Time.timeScale = scale;
        Debug.Log($"Time scale set to: {scale}x");
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public int GetSpeedLevel()
    {
        return speedLevel;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
