using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

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

    [Tooltip("Текст для левой части (Самолеты и полоса)")]
    public TextMeshProUGUI detailedResourcesText;

    [Tooltip("Текст для правой части (Склад с ресурсами)")]
    public TextMeshProUGUI warehouseResourcesText;

    [Header("List Layout Settings")]
    public int leftPadding = 100;
    public float entryWidth = 750f;

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

    private class FlightEntryUI
    {
        public Image backgroundImage;
        public GameObject selectionFrame;
        public int flightIndex;
        public int listIndex;
    }
    private List<FlightEntryUI> activeFlightUIs = new List<FlightEntryUI>();

    private string selectedResourceCallsign = "";

    void Start()
    {
        if (approveButton != null) approveButton.onClick.AddListener(OnApproveClicked);
        if (denyButton != null) denyButton.onClick.AddListener(OnDenyClicked);

        if (resourcesButton != null) resourcesButton.onClick.AddListener(ShowResourcesView);
        if (backToFlightsButton != null) backToFlightsButton.onClick.AddListener(ShowFlightsView);

        if (tvListContainer != null)
        {
            VerticalLayoutGroup vlg = tvListContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childForceExpandWidth = false;
                vlg.childControlWidth = true;
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.padding = new RectOffset(leftPadding, 0, vlg.padding.top, vlg.padding.bottom);
            }
        }

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
            HandleTextClicks();
            // Таймеры теперь обновляются глобально в FlightDataManager
            UpdateResourcesText();
        }
    }

    public void ShowResourcesView()
    {
        if (flightsPanel != null) flightsPanel.SetActive(false);
        if (resourcesPanel != null) resourcesPanel.SetActive(true);

        selectedResourceCallsign = "";
        UpdateResourcesText();
    }

    public void ShowFlightsView()
    {
        if (resourcesPanel != null) resourcesPanel.SetActive(false);
        if (flightsPanel != null) flightsPanel.SetActive(true);

        DisplayFlights();
    }

    private void HandleTextClicks()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            Camera cam = null;
            if (detailedResourcesText.canvas.renderMode == RenderMode.ScreenSpaceCamera ||
                detailedResourcesText.canvas.renderMode == RenderMode.WorldSpace)
            {
                cam = detailedResourcesText.canvas.worldCamera;
            }

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(detailedResourcesText, mousePos, cam);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = detailedResourcesText.textInfo.linkInfo[linkIndex];
                string linkID = linkInfo.GetLinkID();

                if (linkID.StartsWith("UNLOAD_"))
                {
                    if (FlightDataManager.Instance != null)
                        FlightDataManager.Instance.StartUnloading(linkID.Replace("UNLOAD_", ""));
                }
                else if (linkID.StartsWith("REFUEL_"))
                {
                    if (FlightDataManager.Instance != null)
                        FlightDataManager.Instance.StartRefueling(linkID.Replace("REFUEL_", ""));
                }
                else
                {
                    selectedResourceCallsign = (selectedResourceCallsign == linkID) ? "" : linkID;
                }
            }
        }
    }

    private string CreateProgressBar(float percent)
    {
        int barLength = 16;
        int filled = Mathf.Clamp(Mathf.RoundToInt(percent * barLength), 0, barLength);

        string filledBar = new string('|', filled);
        string emptyBar = new string('|', barLength - filled);

        int pct = Mathf.Clamp(Mathf.RoundToInt(percent * 100), 0, 100);
        string pctString = pct.ToString();

        if (pct < 10) pctString = "<color=#00000000>88</color>" + pctString;
        else if (pct < 100) pctString = "<color=#00000000>8</color>" + pctString;

        return $"<color=#888888>[</color><color=#00FF41>{filledBar}</color><color=#002200>{emptyBar}</color><color=#888888>]</color> <color=#FFD700>{pctString}%</color>";
    }

    private void UpdateResourcesText()
    {
        if (FlightDataManager.Instance == null) return;
        var fdm = FlightDataManager.Instance;

        if (detailedResourcesText != null)
        {
            string leftInfo = $"<color={COL_HEADER}><b>RUNWAY STATUS:</b></color>\n";
            string capCol = fdm.landedPlanes >= fdm.maxPlanes ? COL_DENIED : COL_APPROVED;
            leftInfo += $"CAPACITY: <color={capCol}><b>{fdm.landedPlanes} / {fdm.maxPlanes}</b></color>\n\n";

            leftInfo += $"<color=white>LANDED PLANES:</color>\n";
            bool hasApprovedPlanes = false;

            foreach (var flight in fdm.savedFlights)
            {
                if (flight.decisionMade && flight.approved && !flight.isRefueled)
                {
                    hasApprovedPlanes = true;

                    if (selectedResourceCallsign == flight.callsign)
                    {
                        string cargoUnit = "";
                        if (flight.cargo == "Medicines") cargoUnit = " BOX";
                        else if (flight.cargo == "Food") cargoUnit = " KG";
                        else if (flight.cargo == "Fuel") cargoUnit = " L";

                        string displayCargo = flight.isUnloaded ? "EMPTY" : $"{flight.cargo} ({flight.cargoAmount}{cargoUnit})";

                        leftInfo += $" ▼ <link=\"{flight.callsign}\"><color=#FFFFFF><b>{flight.callsign}</b></color></link>\n";
                        leftInfo += $"    CARGO: <color=#FFD700>{displayCargo}</color>\n";
                        leftInfo += $"    TANK FUEL: <color=#FFD700>{flight.currentFuel} / {flight.planeMaxFuel} L</color>\n";

                        if (!flight.isUnloaded) // ФАЗА 1: ВЫГРУЗКА
                        {
                            if (flight.isUnloading)
                            {
                                float progress = 1f - (flight.unloadTimer / FlightDataManager.UNLOAD_TIME);
                                leftInfo += $"    {CreateProgressBar(progress)}\n";
                            }
                            else
                            {
                                leftInfo += $"    <link=\"UNLOAD_{flight.callsign}\"><color=#00FF41><b>[ START UNLOADING ]</b></color></link>\n";
                            }
                        }
                        else // ФАЗА 2: ЗАПРАВКА
                        {
                            if (flight.isRefueling)
                            {
                                float progress = 1f - (flight.refuelTimer / FlightDataManager.REFUEL_TIME);
                                leftInfo += $"    {CreateProgressBar(progress)}\n";
                            }
                            else
                            {
                                int neededFuel = flight.planeMaxFuel - flight.currentFuel;
                                if (fdm.totalFuel > 0)
                                {
                                    leftInfo += $"    <link=\"REFUEL_{flight.callsign}\"><color=#00BFFF><b>[ START REFUELING ({neededFuel}L) ]</b></color></link>\n";
                                }
                                else
                                {
                                    leftInfo += $"    <color=#FF3030>[ NOT ENOUGH FUEL ON WAREHOUSE ]</color>\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        leftInfo += $" ► <link=\"{flight.callsign}\"><color={COL_CALLSIGN}><b>{flight.callsign}</b></color></link>\n";
                    }
                }
            }

            if (!hasApprovedPlanes) leftInfo += $"  <color=#E1D9D1>[ NO PLANES ]</color>\n";
            if (detailedResourcesText.text != leftInfo) detailedResourcesText.text = leftInfo;
        }

        if (warehouseResourcesText != null)
        {
            string rightInfo = $"<color={COL_HEADER}><b>WAREHOUSE STATUS:</b></color>\n\n";

            rightInfo += $"PEOPLE:    <color=#FFD700><b>{fdm.totalPeople} / {fdm.maxPeople}</b></color>\n";
            rightInfo += $"FUEL:      <color=#FFD700><b>{fdm.totalFuel} / {fdm.maxFuel} L</b></color>\n";
            rightInfo += $"MEDICINES: <color=#FFD700><b>{fdm.totalMedicines} / {fdm.maxMedicines} BOX</b></color>\n";
            rightInfo += $"FOOD:      <color=#FFD700><b>{fdm.totalFood} / {fdm.maxFood} KG</b></color>\n";

            if (warehouseResourcesText.text != rightInfo) warehouseResourcesText.text = rightInfo;
        }
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

        foreach (Transform child in tvListContainer) Destroy(child.gameObject);

        activeFlightUIs.Clear();

        CreateStyledLine($"<color={COL_HEADER}><b>╔══════════════════════════════╗</b></color>", 24);
        CreateStyledLine($"<color={COL_HEADER}><b>║     AIR TRAFFIC CONTROL      ║</b></color>", 24);
        CreateStyledLine($"<color={COL_HEADER}><b>╚══════════════════════════════╝</b></color>", 24);
        CreateStyledLine($"<color={COL_SEPARATOR}>──────────────────────────────────</color>", 20);

        var flights = FlightDataManager.Instance.savedFlights;

        if (flights.Count == 0)
        {
            CreateStyledLine($"<color={COL_SEPARATOR}>  [ NO ACTIVE FLIGHTS ]</color>", 24);
            return;
        }

        for (int i = 0; i < flights.Count; i++)
        {
            var data = flights[i];
            int index = i;

            if (hiddenFlights.Contains(data.callsign)) continue;

            GameObject entry = Instantiate(tvEntryPrefab, tvListContainer);

            LayoutElement le = entry.GetComponent<LayoutElement>();
            if (le == null) le = entry.AddComponent<LayoutElement>();
            le.minHeight = 100f;
            le.preferredHeight = 100f;
            le.preferredWidth = entryWidth;

            TextMeshProUGUI txt = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.fontSize = 70;
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
                GameObject frame = AddFrame(entry);
                activeFlightUIs.Add(new FlightEntryUI
                {
                    backgroundImage = img,
                    selectionFrame = frame,
                    flightIndex = index,
                    listIndex = i
                });
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

        CreateStyledLine($"<color={COL_SEPARATOR}>──────────────────────────────────</color>", 20);
        CreateStyledLine($"<color={COL_SPEED}>  TOTAL: {flights.Count} FLIGHT(S)</color>", 20);

        UpdateSelectionVisuals();
    }

    private GameObject AddFrame(GameObject parent)
    {
        GameObject frameObj = new GameObject("SelectionFrame");
        RectTransform rt = frameObj.AddComponent<RectTransform>();
        rt.SetParent(parent.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        rt.offsetMin = new Vector2(-20f, 0f);
        rt.offsetMax = Vector2.zero;

        float thickness = 4f;
        Color frameColor = new Color(0.0f, 0.5f, 0.1f, 1f);

        CreateBorderRect(rt, "Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, thickness), frameColor);
        CreateBorderRect(rt, "Bottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, thickness), frameColor);
        CreateBorderRect(rt, "Left", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(thickness, 0), frameColor);
        CreateBorderRect(rt, "Right", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(thickness, 0), frameColor);

        frameObj.SetActive(false);
        return frameObj;
    }

    private void CreateBorderRect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 sizeDelta, Color color)
    {
        GameObject borderObj = new GameObject(name);
        RectTransform rt = borderObj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;

        Image img = borderObj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private void UpdateSelectionVisuals()
    {
        foreach (var ui in activeFlightUIs)
        {
            if (ui.flightIndex == selectedIndex)
            {
                if (ui.backgroundImage != null) ui.backgroundImage.color = Color.clear;
                if (ui.selectionFrame != null) ui.selectionFrame.SetActive(true);
            }
            else
            {
                if (ui.backgroundImage != null)
                {
                    ui.backgroundImage.color = (ui.listIndex % 2 == 0)
                        ? new Color(0f, 0.12f, 0f, 0.6f)
                        : new Color(0f, 0.08f, 0f, 0.4f);
                }
                if (ui.selectionFrame != null) ui.selectionFrame.SetActive(false);
            }
        }
    }

    void CreateStyledLine(string content, int fontSize = 14)
    {
        GameObject line = Instantiate(tvEntryPrefab, tvListContainer);

        LayoutElement le = line.GetComponent<LayoutElement>();
        if (le == null) le = line.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 40f;
        le.preferredWidth = entryWidth;

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
            string cargoUnit = "";
            if (data.cargo == "Medicines") cargoUnit = " BOX";
            else if (data.cargo == "Food") cargoUnit = " KG";
            else if (data.cargo == "Fuel") cargoUnit = " L";

            string infoString = $"<color=white><b>FLIGHT DETAILS:</b>\n\n</color>";
            infoString += $"Callsign: <b>{callsign}</b>\n";
            infoString += $"Status: {data.status}\n";
            infoString += $"Speed: {data.speed:F0}\n";
            infoString += $"Cargo: <color=#FFD700><b>{data.cargo} ({data.cargoAmount}{cargoUnit})</b></color>\n";

            detailedInfoText.text = infoString;
        }

        UpdateSelectionVisuals();
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

        if (selectionPanelContainer != null) selectionPanelContainer.SetActive(canDecide);

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

        fdm.AddDecision(callsign, true);
        if (selectedLabel != null)
            selectedLabel.text = $"<color={COL_APPROVED}><b>✔ {callsign} — LANDING APPROVED</b></color>";

        if (TVTutorialManager.Instance != null) TVTutorialManager.Instance.NotifyFlightAllowed(callsign);

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

        if (TVTutorialManager.Instance != null) TVTutorialManager.Instance.NotifyFlightDenied(callsign);

        selectedIndex = -1;
        RefreshButtons();
        DisplayFlights();

        float randomDelay = Random.Range(5f, 10f);
        StartCoroutine(HideFlightAfterDelay(callsign, randomDelay));
    }

    private IEnumerator HideFlightAfterDelay(string callsign, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!hiddenFlights.Contains(callsign)) hiddenFlights.Add(callsign);
        DisplayFlights();
    }
}