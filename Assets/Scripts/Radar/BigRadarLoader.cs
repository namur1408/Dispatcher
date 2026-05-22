using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BigRadarLoader : MonoBehaviour
{
    public GameObject airplanePrefab;
    public Transform radarContent;
    public string mainSceneName = "SampleScene";

    [Header("Conflict Alert Settings")]
    public float warningDistance = 125f;

    private List<UIAirplane> activePlanes = new List<UIAirplane>();

    void Start()
    {
        RestoreFlights();
    }

    void Update()
    {
        UIAirplane[] allPlanesOnScene = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        activePlanes.Clear();
        activePlanes.AddRange(allPlanesOnScene);

        if (BigRadarTerminal.Instance != null)
        {
            BigRadarTerminal.Instance.SetPlaneCount(activePlanes.Count);
        }

        CheckForConflicts();
    }

    public static bool isGlobalWarningActive = false;

    private void CheckForConflicts()
    {
        bool anyWarning = false;

        foreach (var plane in activePlanes)
        {
            if (plane != null) plane.SetWarning(false);
        }

        for (int i = 0; i < activePlanes.Count; i++)
        {
            for (int j = i + 1; j < activePlanes.Count; j++)
            {
                UIAirplane planeA = activePlanes[i];
                UIAirplane planeB = activePlanes[j];

                if (planeA == null || planeB == null) continue;

                float distance = Vector2.Distance(
                    planeA.GetComponent<RectTransform>().anchoredPosition,
                    planeB.GetComponent<RectTransform>().anchoredPosition
                );

                if (distance < warningDistance)
                {
                    planeA.SetWarning(true);
                    planeB.SetWarning(true);
                    anyWarning = true;
                }
            }
        }

        isGlobalWarningActive = anyWarning;
    }

    public void RestoreFlights()
    {
        if (FlightDataManager.Instance == null || FlightDataManager.Instance.savedFlights.Count == 0) return;

        foreach (FlightData data in FlightDataManager.Instance.savedFlights)
        {
            // Самолёты, готовые к вылету, показываются в Departures-панели.
            // Спавн произойдёт только после того, как игрок назначит полосу.
            if (data.isReadyToDepart)
            {
                Debug.Log($"<color=yellow>[BigRadarLoader] Skipping auto-spawn for ready-to-depart: {data.callsign}</color>");
                continue;
            }

            if (data.isDeparting)
            {
                // Самолёт УЖЕ вылетает и находится в воздухе (игрок выходил на стол во время его полета).
                // Просто восстанавливаем его на сохраненной позиции с его траекторией.
                GameObject newPlane = Instantiate(airplanePrefab, radarContent, false);
                UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();

                if (planeScript != null)
                {
                    planeScript.InitializeFromData(data);
                    if (RadarManager.Instance != null)
                    {
                        RadarManager.Instance.RegisterAirplane(planeScript);
                    }
                }
            }
            else if (!data.hasLanded)
            {
                GameObject newPlane = Instantiate(airplanePrefab, radarContent, false);
                UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();

                if (planeScript != null)
                {
                    planeScript.InitializeFromData(data);
                }
            }
        }
    }

    /// <summary>
    /// Спавнит самолёт в центре радара и запускает вылет по выбранной полосе.
    /// Вызывается из RadarPanelsManager после выбора игроком полосы вылета.
    /// </summary>
    public void SpawnDepartingNow(FlightData data)
    {
        if (airplanePrefab == null || radarContent == null) return;

        data.isReadyToDepart = false;
        data.isDeparting = true;

        Vector2 spawnPos = Vector2.zero;
        if (!string.IsNullOrEmpty(data.assignedRunway) && RunwayManager.Instance != null)
        {
            Runway rw = RunwayManager.Instance.GetRunwayByID(data.assignedRunway);
            if (rw != null)
            {
                RectTransform rwRect = rw.GetComponent<RectTransform>();
                if (rwRect != null) spawnPos = rwRect.anchoredPosition;
            }
        }
        data.position = spawnPos;

        GameObject newPlane = Instantiate(airplanePrefab, radarContent, false);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();

        if (planeScript != null)
        {
            planeScript.InitializeFromData(data);

            // SetAssignedRunway с isLanding=false → самолёт улетает по направлению полосы
            if (!string.IsNullOrEmpty(data.assignedRunway))
            {
                planeScript.SetAssignedRunway(data.assignedRunway, false);
            }
            else
            {
                // Запасной вариант: улетает в случайном направлении
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector2 exitPoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * planeScript.despawnRadius;
                planeScript.SetFlightPath(Vector2.zero, exitPoint);
            }

            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.RegisterAirplane(planeScript);
            }

            Debug.Log($"<color=green>[BigRadarLoader] Spawned departing plane: {data.callsign} on runway {data.assignedRunway}</color>");
        }
    }

    public void SaveAndReturnToDesk()
    {

        UIAirplane[] allPlanesOnScene = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.UpdateFlights(new List<UIAirplane>(allPlanesOnScene));
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene(mainSceneName);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}