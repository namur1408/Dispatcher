using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TVDisplayInfo : MonoBehaviour
{
    [Header("Original UI Elements")]
    public Transform tvListContainer;
    public GameObject tvEntryPrefab;
    private List<string> hiddenFlights = new List<string>();
    public GameObject selectionPanelContainer;
    public TextMeshProUGUI detailedInfoText;
    public TextMeshProUGUI selectedLabel;
    public Button approveButton;
    public Button denyButton;
    public TextMeshProUGUI baseStatsText;

    [Header("Panels & Resources UI")]
    public GameObject flightsPanel;
    public GameObject resourcesPanel;
    public Button resourcesButton;
    public Button backToFlightsButton;
    public TextMeshProUGUI detailedResourcesText;

    [Header("Resource Settings")]
    public int maxResourceValue = 100;

    [Header("Colors")]
    public Color approveNormalColor = new Color(0.05f, 0.45f, 0.05f, 1f);
    public Color approvePressedColor = new Color(0.1f, 0.8f, 0.1f, 1f);
    public Color denyNormalColor = new Color(0.45f, 0.05f, 0.05f, 1f);
    public Color denyPressedColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    public Color disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);

    private int selectedIndex = -1;

    private const string COL_HEADER = "#00FF41";
    private const string COL_SEPARATOR = "#1A4A1A";
    private const string COL_CALLSIGN = "#00FF41";
    private const string COL_APPROACH = "#FFD700";
    private const string COL_TRANSIT = "#00BFFF";
    private const string COL_APPROVED = "#00FF41";
    private const string COL_DENIED = "#FF3030";
    private const string COL_SPEED = "#888888";
    private const string COL_SELECTED = "#FFFFFF";

    void Start()
    {
        if (approveButton != null) approveButton.onClick.AddListener(OnApproveClicked);
        if (denyButton != null) denyButton.onClick.AddListener(OnDenyClicked);

        if (resourcesButton != null) resourcesButton.onClick.AddListener(ShowResourcesView);
        if (backToFlightsButton != null) backToFlightsButton.onClick.AddListener(ShowFlightsView);

        StyleButtons();
        DisplayFlights();
        RefreshButtons();
        UpdateBaseStatsUI();

        ShowFlightsView();
    }

    void Update()
    {
        UpdateBaseStatsUI();

        if (resourcesPanel != null && resourcesPanel.activeSelf)
        {
            UpdateResourcesText();
        }
    }

    public void ShowResourcesView()
    {
        if (flightsPanel != null) flightsPanel.SetActive(false);
        if (resourcesPanel != null) resourcesPanel.SetActive(true);

        UpdateResourcesText();
    }

    public void ShowFlightsView()
    {
        if (resourcesPanel != null) resourcesPanel.SetActive(false);
        if (flightsPanel != null) flightsPanel.SetActive(true);

        DisplayFlights();
    }

    private void UpdateResourcesText()
    {
        if (detailedResourcesText == null)
        {
            Debug.LogError("[TVDisplayInfo] 'Detailed Resources Text' is NOT assigned in the Inspector!");
            return;
        }

        if (FlightDataManager.Instance == null)
        {
            Debug.LogError("[TVDisplayInfo] 'FlightDataManager' instance is missing from the scene!");
            return;
        }

        var fdm = FlightDataManager.Instance;

        string infoString = $"<color={COL_HEADER}><b>WAREHOUSE STATUS:</b>\n\n</color>";

        infoString += $"PEOPLE: <color=#FFD700><b>{fdm.totalPeople} / {maxResourceValue}</b></color>\n";
        infoString += $"FUEL: <color=#FFD700><b>{fdm.totalFuel} / {maxResourceValue}</b></color>\n";
        infoString += $"MEDICINES: <color=#FFD700><b>{fdm.totalMedicines} / {maxResourceValue}</b></color>\n";
        infoString += $"FOOD: <color=#FFD700><b>{fdm.totalFood} / {maxResourceValue}</b></color>\n\n";

        string capCol = fdm.landedPlanes >= fdm.maxPlanes ? COL_DENIED : COL_APPROVED;
        infoString += $"<color=white>RUNWAY CAPACITY:</color> <color={capCol}><b>{fdm.landedPlanes} / {fdm.maxPlanes}</b></color>\n";
        infoString += $"<color={COL_SEPARATOR}>──────────────────────────────────</color>";

        detailedResourcesText.text = infoString;
    }

    void StyleButtons()
    {
        StyleButton(approveButton, approveNormalColor, approvePressedColor);
        StyleButton(denyButton, denyNormalColor, denyPressedColor);

        SetButtonText(approveButton, "[ ALLOW ]");
        SetButtonText(denyButton, "[ DENY  ]");
    }

    void StyleButton(Button btn, Color normal, Color pressed)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Color.white, 0.2f);
        colors.pressedColor = pressed;
        colors.selectedColor = normal;
        colors.disabledColor = disabledColor;
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = normal;
    }

    void SetButtonText(Button btn, string text)
    {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;
        tmp.text = text;
        tmp.color = new Color(0f, 1f, 0.25f);
        tmp.fontSize = 60;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    void UpdateBaseStatsUI()
    {
        if (baseStatsText == null) return;
        var fdm = FlightDataManager.Instance;
        if (fdm == null) return;

        string capCol = fdm.landedPlanes >= fdm.maxPlanes ? COL_DENIED : COL_APPROVED;

        baseStatsText.text = $"<color=white>CAPACITY:</color> <color={capCol}><b>{fdm.landedPlanes} / {fdm.maxPlanes}</b></color>\n" +
                             $"<color=#FFD700>MED: {fdm.totalMedicines} | PPL: {fdm.totalPeople} | FOOD: {fdm.totalFood} | FUEL: {fdm.totalFuel}</color>";
    }

    void DisplayFlights()
    {
        if (FlightDataManager.Instance == null) return;

        foreach (Transform child in tvListContainer)
            Destroy(child.gameObject);

        CreateStyledLine($"<color={COL_HEADER}><b>╔══════════════════════════════╗</b></color>", 16);
        CreateStyledLine($"<color={COL_HEADER}><b>║     AIR TRAFFIC CONTROL      ║</b></color>", 16);
        CreateStyledLine($"<color={COL_HEADER}><b>╚══════════════════════════════╝</b></color>", 16);
        CreateStyledLine($"<color={COL_SEPARATOR}>──────────────────────────────────</color>", 13);

        var flights = FlightDataManager.Instance.savedFlights;

        if (flights.Count == 0)
        {
            CreateStyledLine($"<color={COL_SEPARATOR}>  [ NO ACTIVE FLIGHTS ]</color>", 15);
            return;
        }

        for (int i = 0; i < flights.Count; i++)
        {
            var data = flights[i];
            int index = i;

            if (hiddenFlights.Contains(data.callsign)) continue;

            GameObject entry = Instantiate(tvEntryPrefab, tvListContainer);

            RectTransform rt = entry.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 60f);

            TextMeshProUGUI txt = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.fontSize = 50;
                txt.alignment = TextAlignmentOptions.Left;

                if (data.decisionMade)
                {
                    string decCol = data.approved ? COL_APPROVED : COL_DENIED;
                    string decIcon = data.approved ? "ALLOWED" : "DENIED ";
                    txt.text = $"<color={COL_CALLSIGN}><b>{data.callsign}</b></color>  " +
                               $"<color={decCol}>[{decIcon}]</color>";
                }
                else
                {
                    string stCol = data.status == "APPROACHING" ? COL_APPROACH : COL_TRANSIT;
                    string stIcon = data.status == "APPROACHING" ? "↓ LAND" : "→ XSIT";
                    txt.text = $"<color={COL_CALLSIGN}><b>{data.callsign}</b></color>  " +
                               $"<color={stCol}>{stIcon}</color>  " +
                               $"<color={COL_SPEED}>SPD:{data.speed:F0}</color>";
                }

                Image img = entry.GetComponent<Image>();
                if (img != null)
                    img.color = (i % 2 == 0)
                        ? new Color(0f, 0.12f, 0f, 0.6f)
                        : new Color(0f, 0.08f, 0f, 0.4f);
            }

            FlightListEntry entryScript = entry.GetComponent<FlightListEntry>();
            if (entryScript != null) entryScript.isStaticEntry = true;

            Button btn = entry.GetComponent<Button>();
            if (btn != null)
            {
                if (!data.decisionMade)
                {
                    btn.interactable = true;
                    btn.onClick.AddListener(() => SelectFlight(index));

                    var colors = btn.colors;
                    colors.normalColor = Color.clear;
                    colors.highlightedColor = new Color(0f, 0.4f, 0f, 0.4f);
                    colors.pressedColor = new Color(0f, 0.6f, 0f, 0.5f);
                    colors.selectedColor = new Color(0f, 0.3f, 0f, 0.5f);
                    btn.colors = colors;
                }
                else
                {
                    btn.interactable = false;
                }
            }
        }

        CreateStyledLine($"<color={COL_SEPARATOR}>──────────────────────────────────</color>", 13);
        CreateStyledLine($"<color={COL_SPEED}>  TOTAL: {flights.Count} FLIGHT(S)</color>", 13);
    }

    void CreateStyledLine(string content, int fontSize = 14)
    {
        GameObject line = Instantiate(tvEntryPrefab, tvListContainer);

        RectTransform rt = line.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 28f);

        TextMeshProUGUI t = line.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null)
        {
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center;
        }

        Image img = line.GetComponent<Image>();
        if (img != null) img.color = Color.clear;

        Button btn = line.GetComponent<Button>();
        if (btn != null) btn.interactable = false;
    }

    void SelectFlight(int index)
    {
        var flights = FlightDataManager.Instance.savedFlights;
        if (index < 0 || index >= flights.Count) return;

        selectedIndex = index;
        string callsign = flights[index].callsign;
        var data = flights[index];

        if (selectedLabel != null)
        {
            string stCol = flights[index].status == "APPROACHING" ? COL_APPROACH : COL_TRANSIT;
            selectedLabel.text = $"<color={COL_HEADER}>SELECTED ►</color> " +
                                 $"<color={COL_SELECTED}><b>{callsign}</b></color>  " +
                                 $"<color={stCol}>{flights[index].status}</color>";
        }

        if (detailedInfoText != null)
        {
            string infoString = $"<color=white><b>FLIGHT DETAILS:</b>\n\n</color>";
            infoString += $"Callsign: <b>{callsign}</b>\n";
            infoString += $"Status: {data.status}\n";
            infoString += $"Speed: {data.speed:F0}\n";
            infoString += $"Cargo: <color=#FFD700><b>{data.cargo}</b></color>\n";

            detailedInfoText.text = infoString;
        }

        RefreshButtons();
    }

    void RefreshButtons()
    {
        bool canDecide = selectedIndex >= 0;

        bool hasSpace = true;
        if (FlightDataManager.Instance != null)
        {
            hasSpace = FlightDataManager.Instance.landedPlanes < FlightDataManager.Instance.maxPlanes;
        }

        bool canApprove = canDecide && hasSpace;

        if (selectionPanelContainer != null)
        {
            selectionPanelContainer.SetActive(canDecide);
        }

        if (approveButton != null)
        {
            approveButton.interactable = canApprove;
            Image img = approveButton.GetComponent<Image>();
            if (img != null) img.color = canApprove ? approveNormalColor : disabledColor;
        }

        if (denyButton != null)
        {
            denyButton.interactable = canDecide;
            Image img = denyButton.GetComponent<Image>();
            if (img != null) img.color = canDecide ? denyNormalColor : disabledColor;
        }

        if (!canDecide && selectedLabel != null)
            selectedLabel.text = $"<color={COL_SEPARATOR}>► SELECT FLIGHT FROM LIST</color>";
    }

    void OnApproveClicked()
    {
        if (selectedIndex < 0) return;
        var fdm = FlightDataManager.Instance;
        var flights = fdm.savedFlights;
        if (selectedIndex >= flights.Count) return;

        if (fdm.landedPlanes >= fdm.maxPlanes) return;

        string callsign = flights[selectedIndex].callsign;

        // Я закомментировал и удалил проверку на Tutorial
        // Больше ничего не блокирует нажатие "ALLOW"!

        fdm.AddDecision(callsign, true);
        if (selectedLabel != null)
            selectedLabel.text = $"<color={COL_APPROVED}><b>✔ {callsign} — LANDING APPROVED</b></color>";

        if (TVTutorialManager.Instance != null)
        {
            TVTutorialManager.Instance.NotifyFlightAllowed(callsign);
        }

        selectedIndex = -1;
        RefreshButtons();
        DisplayFlights();
    }

    void OnDenyClicked()
    {
        if (selectedIndex < 0) return;
        var flights = FlightDataManager.Instance.savedFlights;
        if (selectedIndex >= flights.Count) return;

        string callsign = flights[selectedIndex].callsign;
        FlightDataManager.Instance.AddDecision(callsign, false);

        if (selectedLabel != null)
            selectedLabel.text = $"<color={COL_DENIED}><b>✘ {callsign} — LANDING DENIED</b></color>";

        if (TVTutorialManager.Instance != null)
        {
            TVTutorialManager.Instance.NotifyFlightDenied(callsign);
        }

        selectedIndex = -1;
        RefreshButtons();
        DisplayFlights();

        float randomDelay = Random.Range(5f, 10f);
        StartCoroutine(HideFlightAfterDelay(callsign, randomDelay));
    }

    private IEnumerator HideFlightAfterDelay(string callsign, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!hiddenFlights.Contains(callsign))
        {
            hiddenFlights.Add(callsign);
        }
        DisplayFlights();
    }
}