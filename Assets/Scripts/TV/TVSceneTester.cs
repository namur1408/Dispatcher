using UnityEngine;
using System.Collections.Generic;

public class TVSceneTester : MonoBehaviour
{
    void Start()
    {
        // Небольшая задержка (0.1 сек), чтобы FlightDataManager и TVDisplayInfo 
        // точно успели загрузиться и сделать свои начальные настройки в Awake/Start
        Invoke("GenerateTestFlights", 0.1f);
    }

    void GenerateTestFlights()
    {
        if (FlightDataManager.Instance == null)
        {
            Debug.LogError("[TVSceneTester] 'FlightDataManager' is missing from the scene! Please add it.");
            return;
        }

        var fdm = FlightDataManager.Instance;

        // Очищаем список на всякий случай
        fdm.savedFlights.Clear();

        string[] cargoTypes = { "Medicines", "People", "Food", "Fuel" };

        for (int i = 0; i < 10; i++)
        {
            string callsign = $"TST-{Random.Range(100, 999)}";

            // Если цель (target) равна Vector2.zero, то в FlightData статус будет "APPROACHING". 
            // Если любой другой вектор — будет "TRANSIT". Делаем 50/50.
            Vector2 target = (Random.value > 0.5f) ? Vector2.zero : new Vector2(100, 100);

            float speed = Random.Range(200f, 450f);
            string cargo = cargoTypes[Random.Range(0, cargoTypes.Length)];

            // Создаем фейковый самолет
            FlightData testFlight = new FlightData(
                callsign,
                Vector2.zero, // Текущая позиция (для ТВ не важна)
                target,
                new List<Vector2>(), // Пустой список вейпоинтов
                speed,
                cargo
            );

            // Добавляем его в менеджер
            fdm.savedFlights.Add(testFlight);
        }

        Debug.Log("[TVSceneTester] Successfully generated 10 test flights!");

        // Ищем наш телевизор на сцене и заставляем его обновить список
        TVDisplayInfo tv = FindObjectOfType<TVDisplayInfo>();
        if (tv != null)
        {
            tv.ShowFlightsView();
        }
        else
        {
            Debug.LogWarning("[TVSceneTester] TVDisplayInfo not found in the scene.");
        }
    }
}