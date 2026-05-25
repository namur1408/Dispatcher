using UnityEngine;

public class RadarScanner : MonoBehaviour
{
    public float rotationSpeed = 60f; 

    // Глобальный угол, чтобы все лучи на всех радарах крутились абсолютно синхронно
    public static float globalSweepAngle = 0f;
    private static int lastUpdateFrame = -1;

    void Update()
    {
        // Обновляем глобальный угол 1 раз за кадр, даже если сканеров несколько
        if (Time.frameCount != lastUpdateFrame)
        {
            globalSweepAngle -= rotationSpeed * Time.deltaTime;
            globalSweepAngle %= 360f;
            lastUpdateFrame = Time.frameCount;
        }
        
        transform.localRotation = Quaternion.Euler(0, 0, globalSweepAngle);
    }
}