using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RadarManager : MonoBehaviour
{
    public static RadarManager Instance;

    public Transform listContainer;
    public GameObject entryPrefab;

    private List<UIAirplane> activeAirplanes = new List<UIAirplane>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Восстанавливаем самолёты из сохранённых данных
        if (FlightDataManager.Instance != null && FlightDataManager.Instance.savedFlights.Count > 0)
        {
            foreach (var data in FlightDataManager.Instance.savedFlights)
                RestoreAirplane(data);

            // Применяем решения диспетчера через кадр (дать самолётам зарегистрироваться)
            StartCoroutine(ApplyDecisionsNextFrame());
        }
    }

    void RestoreAirplane(FlightData data)
    {
        AirplaneSpawner spawner = FindFirstObjectByType<AirplaneSpawner>();
        if (spawner == null) return;

        GameObject newPlane   = Instantiate(spawner.airplanePrefab, spawner.radarContent);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();
        if (planeScript != null)
            planeScript.InitializeFromData(data);
    }

    IEnumerator ApplyDecisionsNextFrame()
    {
        yield return null; // ждём один кадр — все Start() завершатся

        foreach (var flight in FlightDataManager.Instance.savedFlights)
        {
            if (!flight.decisionMade) continue;

            // Ищем самолёт по callsign
            UIAirplane target = activeAirplanes.Find(p =>
                p != null && p.callsignText != null && p.callsignText.text == flight.callsign);

            if (target == null) continue;

            if (flight.approved) target.Approve();
            else                 target.Deny();
        }
    }

    public void RegisterAirplane(UIAirplane airplane)
    {
        if (activeAirplanes.Contains(airplane)) return;

        activeAirplanes.Add(airplane);

        if (listContainer != null && entryPrefab != null)
        {
            GameObject entry = Instantiate(entryPrefab, listContainer);
            FlightListEntry entryScript = entry.GetComponent<FlightListEntry>();
            if (entryScript != null) entryScript.Setup(airplane);
        }
    }

    public void UnregisterAirplane(UIAirplane airplane)
    {
        activeAirplanes.Remove(airplane);
    }

    // Сохраняет все активные самолёты перед переходом на другую сцену
    public void SaveToGlobalManager()
    {
        if (FlightDataManager.Instance != null)
            FlightDataManager.Instance.UpdateFlights(activeAirplanes);
    }

    public void SelectAirplane(UIAirplane selectedPlane)
    {
        foreach (var plane in activeAirplanes)
            plane.SetHighlight(plane == selectedPlane);
    }

    public int GetPlanesCount() => activeAirplanes.Count;
}
