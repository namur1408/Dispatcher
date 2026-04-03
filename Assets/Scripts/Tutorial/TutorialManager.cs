using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject airplanePrefab;

    // Эти переменные общие для всей игры. Они не сбрасываются при смене сцен!
    public static int tutorialStep = 0;
    public static float stepTimer = 0f;
    public static bool isTutorialActive = true;

    void Update()
    {
        if (!isTutorialActive) return;

        // Ищем контейнер радара в текущей сцене
        Transform currentRadarContent = FindRadarContent();
        if (currentRadarContent == null) return;

        // ШАГ 0: Ждем 10 СЕКУНД (вместо 3) и спавним первые два самолета
        if (tutorialStep == 0)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= 10f) // <--- Таймер теперь 10 секунд
            {
                // Транзит: теперь спавнится сильно левее (-800)
                SpawnSpecificPlane(new Vector2(-600, 400), new Vector2(1400, 400), "TRN-01", currentRadarContent);

                // Посадка: теперь спавнится сильно выше (1000)
                // (От 1000 до 200 ровно 800 пикселей, как и у первого самолета!)
                SpawnSpecificPlane(new Vector2(0, 1000), Vector2.zero, "LND-02", currentRadarContent);

                Debug.Log("[Tutorial] Появились первые два рейса. Угроза столкновения!");

                tutorialStep = 1; // Переходим к следующему шагу
                stepTimer = 0f;   // Обнуляем таймер
            }
        }
        // ШАГ 1: Ждем 25 секунд и спавним третий самолет
        else if (tutorialStep == 1)
        {
            stepTimer += Time.deltaTime;

            // Если они теперь появляются дальше, им нужно больше времени, чтобы долететь. 
            // Давай сделаем появление третьего самолета чуть позже, например через 35 секунд
            if (stepTimer >= 35f)
            {
                // Третий самолет тоже отодвинем подальше (например, на X = 800)
                SpawnSpecificPlane(new Vector2(800, 0), Vector2.zero, "BAD-03", currentRadarContent);

                Debug.Log("[Tutorial] Появился рейс BAD-03. Нужно нажать Отказ (Deny).");

                tutorialStep = 2; // Обучение закончено
                isTutorialActive = false; // Выключаем туториал навсегда
            }
        }
    }

    // Умный поиск контейнера: скрипт сам поймет, на какой он сцене
    Transform FindRadarContent()
    {
        // Пытаемся найти спавнер из Сцены 1
        AirplaneSpawner spawner = FindFirstObjectByType<AirplaneSpawner>();
        if (spawner != null) return spawner.radarContent;

        // Пытаемся найти лоадер из Сцены 2 (Большой радар)
        BigRadarLoader loader = FindFirstObjectByType<BigRadarLoader>();
        if (loader != null) return loader.radarContent;

        return null;
    }

    void SpawnSpecificPlane(Vector2 startPos, Vector2 targetPos, string customCallsign, Transform contentParent)
    {
        GameObject newPlane = Instantiate(airplanePrefab, contentParent, false);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();

        if (planeScript != null)
        {
            planeScript.SetFlightPath(startPos, targetPos);
            planeScript.callsignText.text = customCallsign;

            // Если мы на Сцене 1, нужно зарегистрировать самолет в менеджере
            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.RegisterAirplane(planeScript);
            }
            // На Сцене 2 BigRadarLoader сам подхватит самолет в своем Update()
        }
    }
}