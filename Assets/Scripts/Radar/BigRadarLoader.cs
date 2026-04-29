using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BigRadarLoader : MonoBehaviour
{
    public GameObject airplanePrefab;
    public Transform radarContent;
    public string mainSceneName = "SampleScene";

    [Header("Conflict Alert Settings")]
    public float warningDistance = 250f;

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

    private void CheckForConflicts()
    {
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
                }
            }
        }
    }

    public void RestoreFlights()
    {
        if (FlightDataManager.Instance == null || FlightDataManager.Instance.savedFlights.Count == 0) return;

        foreach (FlightData data in FlightDataManager.Instance.savedFlights)
        {
            if (data.hasLanded) continue;
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

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.UpdateFlights(new List<UIAirplane>(allPlanesOnScene));
        }

        SceneManager.LoadScene(mainSceneName);
    }
}