using UnityEngine;

/// <summary>
/// Автоматически устанавливает ограничение в 60 FPS при запуске игры.
/// Не требует прикрепления к объектам на сцене.
/// </summary>
public static class GlobalOptimizer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        // Для мобильных устройств отключаем VSync, так как мы вручную задаем targetFrameRate
        QualitySettings.vSyncCount = 0;
        
        // Ограничиваем FPS до 60, чтобы телефон не перегревался и работал стабильно
        Application.targetFrameRate = 60;
        
        Debug.Log("[GlobalOptimizer] Установлен лимит: 60 FPS. VSync отключен.");
    }
}
