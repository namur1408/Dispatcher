using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RadarPanelsManager : MonoBehaviour
{
    public static RadarPanelsManager Instance;

    [Header("Windows (Parents)")]
    public GameObject arrivalsWindow;
    public GameObject transitsWindow;
    public GameObject departuresWindow;

    [Header("Window Content Containers")]
    public Transform arrivalsContent;
    public Transform transitsContent;
    public Transform departuresContent;

    [Header("Prefabs")]
    public GameObject listEntryPrefab; // A prefab with TextMeshProUGUI to show flight details
    
    [System.Serializable]
    public struct RunwayButtonConfig
    {
        public Button button;
        public string runwayId;
    }

    [Header("Runway Selection Panel")]
    public GameObject runwaySelectionPanel;
    public List<RunwayButtonConfig> runwayButtons = new List<RunwayButtonConfig>();
    
    private FlightData selectedFlightForRunway;

    private float refreshTimer = 1f;

    private void Awake()
    {
        Instance = this;
        if (runwaySelectionPanel != null) runwaySelectionPanel.SetActive(false);
        
        SetupRunwayButtons();
    }

    private void SetupRunwayButtons()
    {
        Debug.Log($"[Runway] SetupRunwayButtons: found {runwayButtons.Count} buttons");
        foreach (var config in runwayButtons)
        {
            if (config.button != null && !string.IsNullOrEmpty(config.runwayId))
            {
                string rId = config.runwayId;
                config.button.onClick.AddListener(() =>
                {
                    Debug.Log($"[Runway] Button clicked for runway: {rId}");
                    AssignRunway(rId);
                });
                Debug.Log($"[Runway] Listener added to button for runway: {rId}");
            }
            else
            {
                Debug.LogWarning($"[Runway] Button config invalid: button={config.button}, id={config.runwayId}");
            }
        }
    }

    public void ToggleArrivalsWindow() => arrivalsWindow.SetActive(!arrivalsWindow.activeSelf);
    public void ToggleTransitsWindow() => transitsWindow.SetActive(!transitsWindow.activeSelf);
    public void ToggleDeparturesWindow() => departuresWindow.SetActive(!departuresWindow.activeSelf);

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0)
        {
            refreshTimer = 1f;
            RefreshLists();
        }
    }

    private void RefreshLists()
    {
        if (FlightDataManager.Instance == null) return;

        // Temporarily rescue the runway selection panel so it doesn't get destroyed
        if (runwaySelectionPanel != null && runwaySelectionPanel.activeSelf)
        {
            runwaySelectionPanel.transform.SetParent(this.transform, false);
        }

        ClearContainer(arrivalsContent);
        ClearContainer(transitsContent);
        ClearContainer(departuresContent);

        GameObject selectedEntryObject = null;

        foreach (var flight in FlightDataManager.Instance.savedFlights)
        {
            GameObject newEntry = null;
            if (flight.isDeparting)
            {
                newEntry = CreateEntry(flight, departuresContent);
            }
            else if (flight.targetPosition != Vector2.zero)
            {
                newEntry = CreateEntry(flight, transitsContent);
            }
            else
            {
                if (!flight.hasLanded)
                {
                    newEntry = CreateEntry(flight, arrivalsContent);
                }
            }

            if (newEntry != null && selectedFlightForRunway != null && flight.callsign == selectedFlightForRunway.callsign)
            {
                selectedEntryObject = newEntry;
            }
        }

        // Re-parent the runway selection panel as a sibling below the newly created entry
        if (selectedEntryObject != null && runwaySelectionPanel != null && runwaySelectionPanel.activeSelf)
        {
            runwaySelectionPanel.transform.SetParent(selectedEntryObject.transform.parent, false);
            runwaySelectionPanel.transform.SetSiblingIndex(selectedEntryObject.transform.GetSiblingIndex() + 1);
        }
        else if (runwaySelectionPanel != null && runwaySelectionPanel.activeSelf)
        {
            runwaySelectionPanel.SetActive(false);
            selectedFlightForRunway = null;
            runwaySelectionPanel.transform.SetParent(this.transform, false);
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container)
        {
            if (runwaySelectionPanel != null && child.gameObject == runwaySelectionPanel) continue;
            Destroy(child.gameObject);
        }
    }

    private GameObject CreateEntry(FlightData data, Transform container)
    {
        if (container == null || listEntryPrefab == null) return null;

        GameObject entry = Instantiate(listEntryPrefab, container);
        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
        Button btn = entry.GetComponent<Button>();

        if (text != null)
        {
            string dest = data.isDeparting ? data.departureDestination : (data.targetPosition == Vector2.zero ? "BASE" : "TRANSIT");
            string status = data.assignedRunway != "" ? $"[RWY {data.assignedRunway}]" : "";
            text.text = $"{data.callsign} | {dest} | {status}";
            
            // Highlight if approved
            if (data.approved && string.IsNullOrEmpty(data.assignedRunway))
            {
                text.color = Color.green;
            }
        }

        if (btn != null)
        {
            btn.onClick.AddListener(() => OnFlightClicked(data, entry));
        }

        return entry;
    }

    private void OnFlightClicked(FlightData data, GameObject clickedEntry)
    {
        // Allow runway assignment if it's an arrival and approved, OR if it's departing and has no runway yet
        if ((!data.isDeparting && data.approved && string.IsNullOrEmpty(data.assignedRunway)) || 
            (data.isDeparting && string.IsNullOrEmpty(data.assignedRunway)))
        {
            // Toggle off if clicking the same flight
            if (selectedFlightForRunway == data && runwaySelectionPanel != null && runwaySelectionPanel.activeSelf)
            {
                runwaySelectionPanel.SetActive(false);
                selectedFlightForRunway = null;
                runwaySelectionPanel.transform.SetParent(this.transform, false);
                return;
            }

            selectedFlightForRunway = data;
            if (runwaySelectionPanel != null)
            {
                Debug.Log($"[Runway] Showing panel for: {data.callsign}");
                runwaySelectionPanel.transform.SetParent(clickedEntry.transform.parent, false);
                runwaySelectionPanel.transform.SetSiblingIndex(clickedEntry.transform.GetSiblingIndex() + 1);
                runwaySelectionPanel.SetActive(true);

                // Check button interactability
                foreach (var cfg in runwayButtons)
                {
                    if (cfg.button != null)
                        Debug.Log($"[Runway] Button {cfg.runwayId} interactable={cfg.button.interactable}, active={cfg.button.gameObject.activeInHierarchy}");
                }
            }
        }
    }

    private void AssignRunway(string runwayId)
    {
        if (selectedFlightForRunway == null) return;

        // Occupancy check
        if (RunwayManager.Instance != null && RunwayManager.Instance.IsRunwayOccupied(runwayId))
        {
            Debug.LogWarning($"[Runway] Runway {runwayId} is occupied!");
            return;
        }

        // 1. Update data
        selectedFlightForRunway.assignedRunway = runwayId;

        // 2. Direct search for airplane in the scene
        UIAirplane[] allPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        foreach (var plane in allPlanes)
        {
            if (plane != null && (plane.originalCallsign == selectedFlightForRunway.callsign || 
                                 (plane.callsignText != null && plane.callsignText.text == selectedFlightForRunway.callsign)))
            {
                plane.SetAssignedRunway(runwayId, !selectedFlightForRunway.isDeparting);
                break;
            }
        }

        // 3. UI Cleanup
        if (runwaySelectionPanel != null)
        {
            runwaySelectionPanel.SetActive(false);
            runwaySelectionPanel.transform.SetParent(this.transform, false);
        }
        selectedFlightForRunway = null;
        RefreshLists();
    }
}
