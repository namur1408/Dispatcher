using UnityEngine;

public class AirplaneSpawner: MonoBehaviour
{
    [Header("Settings")]
    public GameObject airplanePrefab;
    public Transform radarContent;
    public int maxAirplanes = 5;

    public float minSpawnTime = 3f;   
    public float maxSpawnTime = 8f;    
    public float spawnRadius = 400f;   

    [Range(0f, 1f)]
    public float landingProbability = 0.5f; 

    private float timer;

    void Start()
    {
        SetRandomTimer();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            if (RadarManager.Instance != null && RadarManager.Instance.GetPlanesCount() < maxAirplanes)
            {
                SpawnAirplane(); 
            }
            SetRandomTimer();
        }
    }

    void SetRandomTimer()
    {
        timer = Random.Range(minSpawnTime, maxSpawnTime);
    }

    void SpawnAirplane()
    {
        float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 startPos = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * spawnRadius;
        Vector2 targetPos;
        if (Random.value < landingProbability)
        {
            targetPos = Vector2.zero;
        }
        else
        {
            float endAngle = startAngle + Random.Range(120f, 240f) * Mathf.Deg2Rad;
            targetPos = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * (spawnRadius + 200f);
        }
        GameObject newPlane = Instantiate(airplanePrefab, radarContent);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();
        if (planeScript != null)
        {
            planeScript.SetFlightPath(startPos, targetPos);
        }
    }
}
