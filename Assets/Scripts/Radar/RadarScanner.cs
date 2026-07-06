using UnityEngine;

public class RadarScanner : MonoBehaviour
{
    public float rotationSpeed = 60f; 

    // Global angle so that all beams on all radars rotate absolutely synchronously
    public static float globalSweepAngle = 0f;
    private static int lastUpdateFrame = -1;

    void Update()
    {
        // We update the global angle once per frame, even if there are several scanners
        if (Time.frameCount != lastUpdateFrame)
        {
            globalSweepAngle -= rotationSpeed * Time.deltaTime;
            globalSweepAngle %= 360f;
            lastUpdateFrame = Time.frameCount;
        }
        
        transform.localRotation = Quaternion.Euler(0, 0, globalSweepAngle);
    }
}