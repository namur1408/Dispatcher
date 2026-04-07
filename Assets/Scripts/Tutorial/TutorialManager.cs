using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject airplanePrefab;


    [Header("Plane Callsigns")]
    public string[] tutorialCallsigns = { "GE-672", "QY-467", "KO-677" };

    public static int tutorialStep = 0;
    public static float stepTimer = 0f;
    public static bool isTutorialActive = true;

    void Update()
    {
        if (!isTutorialActive) return;
        Transform currentRadarContent = FindRadarContent();
        if (currentRadarContent == null) return;

        if (tutorialStep == 0)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= 10f)
            {
                SpawnSpecificPlane(new Vector2(-624, 200), new Vector2(800, 200), tutorialCallsigns[0], currentRadarContent);

                SpawnSpecificPlane(new Vector2(-500, 500), Vector2.zero, tutorialCallsigns[1], currentRadarContent);

                Debug.Log("[Tutorial] Появились первые два рейса. Столкновение по касательной!");

                tutorialStep = 1;
                stepTimer = 0f;
            }
        }
        else if (tutorialStep == 1)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= 35f)
            {
                SpawnSpecificPlane(new Vector2(800, 0), Vector2.zero, tutorialCallsigns[2], currentRadarContent);

                Debug.Log($"[Tutorial] Появился рейс {tutorialCallsigns[2]}. Нужно нажать Отказ (Deny).");

                tutorialStep = 2;
                isTutorialActive = false;
            }
        }
    }

    Transform FindRadarContent()
    {
        AirplaneSpawner spawner = FindFirstObjectByType<AirplaneSpawner>();
        if (spawner != null) return spawner.radarContent;

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
            planeScript.SetCallsign(customCallsign);
            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.RegisterAirplane(planeScript);
            }
        }
    }
}