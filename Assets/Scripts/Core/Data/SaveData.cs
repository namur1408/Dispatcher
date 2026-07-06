using System.Collections.Generic;

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

    // Use two lists for serializing the dictionary
    public List<string> interrogationKeys = new List<string>();
    public List<FlightInterrogationState> interrogationValues = new List<FlightInterrogationState>();

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
