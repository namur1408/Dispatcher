using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class SaveData
{
    public int currentDay;
    public bool isShiftActive;
    public float globalSpawnTimer;
    
    // FlightDataManager state
    public List<FlightData> savedFlights = new List<FlightData>();
    public List<FlightData> pendingFlights = new List<FlightData>();
    public List<float> pendingDelays = new List<float>();

    public int totalFuel;
    public int totalFood;
    public int totalPeople;
    public int totalMedicines;

    public int startFuelDay;
    public int startFoodDay;
    public int startPeopleDay;
    public int startMedsDay;

    public int maxPlanes;
    public int landedPlanes;
    public float accumulatedFoodConsumption;
    
    public List<EmailData> savedEmails = new List<EmailData>();
}

public static class GameSaveManager
{
    private static string SavePath => Application.persistentDataPath + "/savedata.json";

    // Used to pass data across scenes when continuing
    public static SaveData loadedData;

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void SaveGame()
    {
        if (FlightDataManager.Instance == null) return;
        
        SaveData data = new SaveData();
        data.currentDay = StoryManager.currentDay;
        
        data.isShiftActive = FlightDataManager.Instance.isShiftActive;
        data.globalSpawnTimer = FlightDataManager.Instance.globalSpawnTimer;
        
        // Обновляем float-списки координат вейпоинтов перед сохранением
        // (JsonUtility не умеет сериализовать List<Vector2> напрямую)
        foreach (var flight in FlightDataManager.Instance.savedFlights)
            flight.UpdateSerializedWaypoints();
        
        data.savedFlights = FlightDataManager.Instance.savedFlights;
        
        data.pendingFlights = new List<FlightData>(FlightDataManager.Instance.scriptedFlightsQueue);
        data.pendingDelays = new List<float>(FlightDataManager.Instance.scriptedDelaysQueue);
        
        data.totalFuel = FlightDataManager.Instance.totalFuel;
        data.totalFood = FlightDataManager.Instance.totalFood;
        data.totalPeople = FlightDataManager.Instance.totalPeople;
        data.totalMedicines = FlightDataManager.Instance.totalMedicines;
        
        data.startFuelDay = FlightDataManager.Instance.startFuelDay;
        data.startFoodDay = FlightDataManager.Instance.startFoodDay;
        data.startPeopleDay = FlightDataManager.Instance.startPeopleDay;
        data.startMedsDay = FlightDataManager.Instance.startMedsDay;
        
        data.maxPlanes = FlightDataManager.Instance.maxPlanes;
        data.landedPlanes = FlightDataManager.Instance.landedPlanes;
        data.accumulatedFoodConsumption = FlightDataManager.Instance.accumulatedFoodConsumption;
        
        data.savedEmails = new List<EmailData>(AegisMailApp.globalInbox);
        
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Game saved to " + SavePath);
    }

    public static SaveData LoadGame()
    {
        if (HasSave())
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error loading save data: " + e.Message);
            }
        }
        return null;
    }

    public static void DeleteSave()
    {
        if (HasSave())
        {
            File.Delete(SavePath);
        }
    }
}
