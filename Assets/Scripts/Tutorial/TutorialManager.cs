using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Settings")]
    public GameObject airplanePrefab;

    [Header("Plane Callsigns")]
    public string[] tutorialCallsigns = { "GE-672", "QY-467", "TR-999" };

    public static int tutorialStep = 0;
    public static float stepTimer = 0f;
    public static bool isTutorialActive = true;
    public static bool tvTutorialVisited = false;

    void Awake()
    {
        Instance = this;
        bool isSkipped = PlayerPrefs.GetInt("SkipTutorial", 0) == 1;

        if (isSkipped || DeskTutorialManager.tutorialWasSkipped || PlayerPrefs.HasKey("StartDayNumber") || StoryManager.currentDay > 1)
        {
            isTutorialActive = false;
        }
        else
        {
            tutorialStep = 0;
            stepTimer = 0f;
            isTutorialActive = true;
        }
    }

    public void StopTutorial()
    {
        isTutorialActive = false;
        Debug.Log("<color=yellow>[TutorialManager]</color> Tutorial stopped.");
    }

    void Update()
    {
        if (!isTutorialActive) return;

        Transform currentRadarContent = FindRadarContent();
        if (currentRadarContent == null) return;

        if (tutorialStep == 0)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= 10f)
            {
                SpawnSpecificPlane(new Vector2(-370, 119), new Vector2(476, 119), tutorialCallsigns[0], currentRadarContent);
                SpawnSpecificPlane(new Vector2(-297, 297), Vector2.zero, tutorialCallsigns[1], currentRadarContent);
                Debug.Log("[Tutorial] Two tutorial planes spawned.");
                tutorialStep = 1;
                stepTimer = 0f;
            }
        }
        else if (tutorialStep == 1)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= 35f)
            {
                SpawnSpecificPlane(new Vector2(476, 0), Vector2.zero, tutorialCallsigns[2], currentRadarContent);
                Debug.Log($"[Tutorial] {tutorialCallsigns[2]} spawned. Player must Deny its entry.");
                tutorialStep = 2;
                isTutorialActive = false;
            }
        }
    }

    Transform FindRadarContent()
    {
        AirplaneSpawner spawner = FindFirstObjectByType<AirplaneSpawner>();
        if (spawner != null) return spawner.radarContent;
        BigRadarLoader loader = FindFirstObjectByType<BigRadarLoader>();
        if (loader != null) return loader.radarContent;
        return null;
    }

    public void SpawnSpecificPlane(Vector2 startPos, Vector2 targetPos, string customCallsign, Transform contentParent)
    {
        GameObject newPlane = Instantiate(airplanePrefab, contentParent, false);
        UIAirplane planeScript = newPlane.GetComponent<UIAirplane>();
        if (planeScript != null)
        {
            string cargo = "None";
            float speed = 80f;
            int amount = 100;
            if (customCallsign.StartsWith("GE")) { cargo = "Food"; speed = 80f; amount = 200; }
            else if (customCallsign.StartsWith("QY")) { cargo = "Medicines"; speed = 95f; amount = 30; }
            else if (customCallsign.StartsWith("TR")) { cargo = "Weapons"; speed = 75f; amount = 150; }

            FlightData newFlight = new FlightData(customCallsign, startPos, targetPos, new System.Collections.Generic.List<Vector2>(), speed, cargo, amount);
            
            if (targetPos != Vector2.zero)
            {
                newFlight.currentFuel = 9999f;
            }
            else
            {
                newFlight.planeMaxFuel = 800;
                newFlight.currentFuel = 800;
            }

            if (customCallsign == tutorialCallsigns[2]) {
                newFlight.manifestCargo = "People";
                newFlight.spokenCargo = "Food";
                newFlight.explanationCargo = "I'm sorry, sir, I misspoke. We have people there.";
            }

            planeScript.InitializeFromData(newFlight);

            if (FlightDataManager.Instance != null && !FlightDataManager.Instance.savedFlights.Contains(newFlight))
            {
                FlightDataManager.Instance.savedFlights.Add(newFlight);
            }

            planeScript.SetFlightPath(startPos, targetPos);
            planeScript.SetCallsign(customCallsign);
            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.RegisterAirplane(planeScript);
            }
        }
    }
}
