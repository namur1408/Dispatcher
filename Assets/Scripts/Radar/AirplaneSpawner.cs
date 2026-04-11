using UnityEngine;

public class AirplaneSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject airplanePrefab;
    public Transform radarContent;
    public int maxAirplanes = 5;
    public bool disableRandomSpawns = false;

    public float minSpawnTime = 3f;
    public float maxSpawnTime = 8f;
    public float spawnRadius = 400f;

    [Range(0f, 1f)]
    public float landingProbability = 0.5f;

    [Header("Safety Settings")]
    public float minSpawnGap = 150f;
    public int spawnAttempts = 10;  

    private float timer;

    void Start()
    {
        SetRandomTimer();
    }

    void Update()
    {
        if (disableRandomSpawns) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            int currentCount = GetCurrentPlanesCount();

            if (currentCount < maxAirplanes)
            {
                SpawnAirplane();
            }
            SetRandomTimer();
        }
    }

    int GetCurrentPlanesCount()
    {
        if (RadarManager.Instance != null)
        {
            return RadarManager.Instance.GetPlanesCount();
        }
        else
        {
            if (radarContent != null)
            {
                return radarContent.GetComponentsInChildren<UIAirplane>().Length;
            }
            return 0;
        }
    }

    void SetRandomTimer()
    {
        timer = Random.Range(minSpawnTime, maxSpawnTime);
    }

    void SpawnAirplane()
    {
        Vector2 startPos = Vector2.zero;
        Vector2 targetPos = Vector2.zero;
        bool positionFound = false;

        for (int i = 0; i < spawnAttempts; i++)
        {
            float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            startPos = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * spawnRadius;

            if (IsPositionSafe(startPos))
            {
                positionFound = true;

                if (Random.value < landingProbability)
                {
                    targetPos = Vector2.zero;
                }
                else
                {
                    float endAngle = startAngle + Random.Range(120f, 240f) * Mathf.Deg2Rad;
                    targetPos = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * (spawnRadius + 200f);
                }
                break; 
            }
        }

        if (positionFound)
        {
            GameObject newPlane = Instantiate(airplanePrefab, radarContent, false);
            UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();

            if (planeScript != null)
            {
                planeScript.SetFlightPath(startPos, targetPos);

                if (RadarManager.Instance != null)
                {
                    RadarManager.Instance.RegisterAirplane(planeScript);
                }
            }
        }
        else
        {
            Debug.LogWarning("[AirplaneSpawner] �� ������� ����� ���������� ����� ��� ������!");
        }
    }

    bool IsPositionSafe(Vector2 potentialPos)
    {
        UIAirplane[] existingPlanes = radarContent.GetComponentsInChildren<UIAirplane>();

        foreach (UIAirplane plane in existingPlanes)
        {
            if (plane == null) continue;

            float distance = Vector2.Distance(potentialPos, plane.GetComponent<RectTransform>().anchoredPosition);

            if (distance < minSpawnGap)
            {
                return false; 
            }
        }

        return true; 
    }
}