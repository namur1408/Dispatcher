using UnityEngine;

/// <summary>
/// Automatically sets the framerate cap to 60 FPS when starting the game.
/// No MonoBehaviour attachment required.
/// </summary>
public static class GlobalOptimizer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        // Disable VSync on mobile devices to allow manual targetFrameRate control.
        QualitySettings.vSyncCount = 0;
        
        // Cap framerate to 60 FPS to prevent device overheating and ensure stability.
        Application.targetFrameRate = 60;
        
        Debug.Log("[GlobalOptimizer] Установлен лимит: 60 FPS. VSync отключен.");
    }
}
