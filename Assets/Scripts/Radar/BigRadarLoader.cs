using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BigRadarLoader : MonoBehaviour
{
    public GameObject airplanePrefab;
    public Transform radarContent;
    public string mainSceneName = "SampleScene";

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
    }

    public void RestoreFlights()
    {
        if (FlightDataManager.Instance == null || FlightDataManager.Instance.savedFlights.Count == 0) return;

        foreach (FlightData data in FlightDataManager.Instance.savedFlights)
        {
            GameObject newPlane = Instantiate(airplanePrefab, radarContent, false);
            UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();

            if (planeScript != null)
            {
                planeScript.InitializeFromData(data);
            }
        }
    }

    public void SaveAndReturnToDesk()
    {
        UIAirplane[] allPlanesOnScene = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        activePlanes.Clear();
        activePlanes.AddRange(allPlanesOnScene);

        Debug.Log($"[BigRadarLoader] Пытаемся сохранить {activePlanes.Count} самолетов перед выходом...");

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.UpdateFlights(activePlanes);
            Debug.Log($"[BigRadarLoader] В глобальной базе теперь: {FlightDataManager.Instance.savedFlights.Count} записей.");
        }
        else
        {
            Debug.LogError("[BigRadarLoader] ОШИБКА: FlightDataManager не найден на сцене!");
        }

        SceneManager.LoadScene(mainSceneName);
    }
}