using UnityEngine;
using TMPro;

public class MainScreenResources : MonoBehaviour
{
    public TextMeshProUGUI resourcesText;

    void Update()
    {
        if (FlightDataManager.Instance != null && resourcesText != null)
        {
            var fdm = FlightDataManager.Instance;

            string info = $"PEOPLE: <color=#FFD700>{fdm.totalPeople}</color>/{fdm.maxPeople}\n" +
                          $"FUEL: <color=#FFD700>{fdm.totalFuel}</color>/{fdm.maxFuel}L\n" +
                          $"FOOD: <color=#FFD700>{fdm.totalFood}</color>/{fdm.maxFood}KG\n" +
                          $"MEDS: <color=#FFD700>{fdm.totalMedicines}</color>/{fdm.maxMedicines}";

            if (resourcesText.text != info)
            {
                resourcesText.text = info;
            }
        }
        else if (resourcesText != null)
        {
            resourcesText.text = "LOADING DATA...";
        }
    }
}