using UnityEngine;
using TMPro;

public class TVDisplayInfo : MonoBehaviour
{
    public Transform tvListContainer;
    public GameObject tvEntryPrefab;

    void Start()
    {
        DisplaySavedFlights();
    }

    void DisplaySavedFlights()
    {
        if (FlightDataManager.Instance == null) return;

        foreach (Transform child in tvListContainer) Destroy(child.gameObject);


        CreateLine("<color=#00FF00>== RADAR ACTIVE LOG ==</color>");
        CreateLine("----------------------------");

        foreach (var data in FlightDataManager.Instance.savedFlights)
        {
            GameObject entry = Instantiate(tvEntryPrefab, tvListContainer);
            TextMeshProUGUI textComp = entry.GetComponentInChildren<TextMeshProUGUI>();

            if (textComp != null)
            {
                string stColor = data.status == "APPROACHING" ? "<color=yellow>" : "<color=cyan>";
                textComp.text = $"[ {data.callsign} ]  {stColor}{data.status}</color>  SPD: {data.speed:F1}";
            }

            FlightListEntry entryScript = entry.GetComponent<FlightListEntry>();
            if (entryScript != null) entryScript.isStaticEntry = true;

            UnityEngine.UI.Button btn = entry.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.interactable = false;
        }
    }

    void CreateLine(string content)
    {
        GameObject line = Instantiate(tvEntryPrefab, tvListContainer);
        TextMeshProUGUI t = line.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.text = content;
        line.GetComponent<UnityEngine.UI.Button>().enabled = false;
    }
}