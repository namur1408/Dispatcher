using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TVDisplayInfo : MonoBehaviour
{
    // ── Flight List ──────────────────────────────────────────────────────
    [Header("Flight List")]
    public Transform tvListContainer;
    public GameObject tvEntryPrefab;

    [Header("Selection Label")]
    public TextMeshProUGUI selectedLabel;

    [Header("Action Buttons")]
    public Button approveButton;
    public Button denyButton;

    // ── Aircraft Info Panel ──────────────────────────────────────────────
    [Header("Aircraft Info Panel")]
    [SerializeField] private GameObject aircraftInfoPanel;
    [SerializeField] private TextMeshProUGUI infoPanelTitle;
    [SerializeField] private TextMeshProUGUI infoIndex;
    [SerializeField] private TextMeshProUGUI infoCountry;
    [SerializeField] private TextMeshProUGUI infoCargo;
    [SerializeField] private TextMeshProUGUI infoHeight;
    [SerializeField] private TextMeshProUGUI infoSpeed;
    [SerializeField] private TextMeshProUGUI manageLandingLabel;

    // ── Tutorial Highlights ───────────────────────────────────────────────
    [Header("Tutorial Highlight Overlays")]
    public GameObject listHighlight;
    public GameObject buttonsHighlight;

    // ── Colours ───────────────────────────────────────────────────────────
    [HideInInspector] public Color approveNormalColor  = new Color(0.04f, 0.40f, 0.04f, 1f);
    [HideInInspector] public Color approvePressedColor = new Color(0.10f, 0.85f, 0.10f, 1f);
    [HideInInspector] public Color denyNormalColor     = new Color(0.42f, 0.04f, 0.04f, 1f);
    [HideInInspector] public Color denyPressedColor    = new Color(0.90f, 0.10f, 0.10f, 1f);
    [HideInInspector] public Color disabledColor       = new Color(0.12f, 0.12f, 0.12f, 0.55f);

    // ── TMP color tags ────────────────────────────────────────────────────
    private const string COL_HEADER    = "#00FF41";
    private const string COL_SEPARATOR = "#1A6A1A";
    private const string COL_CALLSIGN  = "#00FF41";
    private const string COL_APPROACH  = "#FFD700";
    private const string COL_TRANSIT   = "#00BFFF";
    private const string COL_APPROVED  = "#00FF41";
    private const string COL_DENIED    = "#FF3030";
    private const string COL_SPEED_TXT = "#AAAAAA";
    private const string COL_SELECTED  = "#FFFFFF";
    private const string COL_DIM       = "#2A6A2A";
    private const string COL_LABEL     = "#4DFFB4";

    // ── State ─────────────────────────────────────────────────────────────
    private int selectedIndex = -1;

    public int SelectedIndex => selectedIndex;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Start()
    {
        if (approveButton != null) approveButton.onClick.AddListener(OnApproveClicked);
        if (denyButton    != null) denyButton.onClick.AddListener(OnDenyClicked);

        if (listHighlight     != null) listHighlight.SetActive(false);
        if (buttonsHighlight  != null) buttonsHighlight.SetActive(false);
        if (aircraftInfoPanel != null) aircraftInfoPanel.SetActive(false);

        StyleButtons();
        DisplayFlights();
        RefreshButtons();
    }

    // ── Button Styling ────────────────────────────────────────────────────
    void StyleButtons()
    {
        StyleOneButton(approveButton, approveNormalColor, approvePressedColor, "ALLOW", "v");
        StyleOneButton(denyButton,    denyNormalColor,    denyPressedColor,    "DENY",  "x");
    }

    void StyleOneButton(Button btn, Color normal, Color pressed, string label, string icon)
    {
        if (btn == null) return;
        var cb = btn.colors;
        cb.normalColor      = normal;
        cb.highlightedColor = Color.Lerp(normal, Color.white, 0.18f);
        cb.pressedColor     = pressed;
        cb.selectedColor    = normal;
        cb.disabledColor    = disabledColor;
        cb.colorMultiplier  = 1f;
        btn.colors = cb;

        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = normal;

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;
        tmp.text               = string.Format("<size=115%><b>{0}</b></size>\n<size=78%>[ {1} ]</size>", icon, label);
        tmp.color              = new Color(0f, 1f, 0.28f);
        tmp.fontSize           = 22;
        tmp.fontStyle          = FontStyles.Bold;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
    }

    // ── Flight List ───────────────────────────────────────────────────────
    public void DisplayFlights()
    {
        if (FlightDataManager.Instance == null) return;

        foreach (Transform child in tvListContainer)
            Destroy(child.gameObject);

        CreateStyledLine(string.Format("<color={0}><b>====================</b></color>", COL_HEADER), 15);
        CreateStyledLine(string.Format("<color={0}><b> AIR TRAFFIC CONTROL </b></color>", COL_HEADER), 20);
        CreateStyledLine(string.Format("<color={0}><b>====================</b></color>", COL_HEADER), 15);

        var flights = FlightDataManager.Instance.savedFlights;

        if (flights.Count == 0)
        {
            CreateStyledLine(string.Format("<color={0}>[ NO ACTIVE FLIGHTS ]</color>", COL_DIM), 16);
            return;
        }

        CreateStyledLine(string.Format("<color={0}>  CALLSIGN    STATUS   SPD </color>", COL_DIM), 13);
        CreateStyledLine(string.Format("<color={0}>--------------------</color>", COL_SEPARATOR), 13);

        for (int i = 0; i < flights.Count; i++)
        {
            var data  = flights[i];
            int index = i;

            GameObject entry = Instantiate(tvEntryPrefab, tvListContainer);

            RectTransform rt = entry.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 54f);

            TextMeshProUGUI txt = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.fontSize           = 19;
                txt.alignment          = TextAlignmentOptions.Left;
                txt.enableWordWrapping = false;

                if (data.decisionMade)
                {
                    string decCol  = data.approved ? COL_APPROVED : COL_DENIED;
                    string decIcon = data.approved ? "ALLOW" : "DENY ";
                    txt.text = string.Format(" <color={0}><b>{1,-9}</b></color><color={2}> {3}</color>",
                        COL_CALLSIGN, data.callsign, decCol, decIcon);
                }
                else
                {
                    string stCol  = data.status == "APPROACHING" ? COL_APPROACH : COL_TRANSIT;
                    string stIcon = data.status == "APPROACHING" ? "LAND" : "XSIT";
                    txt.text = string.Format(" <color={0}><b>{1,-9}</b></color><color={2}>{3,-7}</color><color={4}>{5:F0}</color>",
                        COL_CALLSIGN, data.callsign, stCol, stIcon, COL_SPEED_TXT, data.speed);
                }

                Image rowImg = entry.GetComponent<Image>();
                if (rowImg != null)
                    rowImg.color = (i % 2 == 0)
                        ? new Color(0f, 0.16f, 0f, 0.65f)
                        : new Color(0f, 0.09f, 0f, 0.45f);
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
                    var cb = btn.colors;
                    cb.normalColor      = Color.clear;
                    cb.highlightedColor = new Color(0f, 0.45f, 0f, 0.40f);
                    cb.pressedColor     = new Color(0f, 0.65f, 0f, 0.55f);
                    cb.selectedColor    = new Color(0f, 0.30f, 0f, 0.50f);
                    btn.colors = cb;
                }
                else
                {
                    btn.interactable = false;
                }
            }
        }

        CreateStyledLine(string.Format("<color={0}>--------------------</color>", COL_SEPARATOR), 13);
        CreateStyledLine(string.Format("<color={0}>  TOTAL: {1} FLIGHT(S)</color>", COL_SPEED_TXT, flights.Count), 14);
    }

    void CreateStyledLine(string content, int fontSize = 14)
    {
        GameObject line = Instantiate(tvEntryPrefab, tvListContainer);

        RectTransform rt = line.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 30f);

        TextMeshProUGUI t = line.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null)
        {
            t.text      = content;
            t.fontSize  = fontSize;
            t.alignment = TextAlignmentOptions.Center;
        }

        Image img = line.GetComponent<Image>();
        if (img != null) img.color = Color.clear;

        Button btn = line.GetComponent<Button>();
        if (btn != null) btn.interactable = false;
    }

    // ── Selection ─────────────────────────────────────────────────────────
    void SelectFlight(int index)
    {
        var flights = FlightDataManager.Instance.savedFlights;
        if (index < 0 || index >= flights.Count) return;

        selectedIndex = index;
        var data = flights[index];

        if (selectedLabel != null)
        {
            string stCol = data.status == "APPROACHING" ? COL_APPROACH : COL_TRANSIT;
            selectedLabel.text = string.Format("<color={0}>SELECTED: </color><color={1}><b>{2}</b></color>  <color={3}>{4}</color>",
                COL_HEADER, COL_SELECTED, data.callsign, stCol, data.status);
        }

        ShowAircraftInfo(data);
        RefreshButtons();
    }

    void ShowAircraftInfo(FlightData data)
    {
        if (aircraftInfoPanel != null)
            aircraftInfoPanel.SetActive(true);

        if (infoPanelTitle != null)
        {
            infoPanelTitle.text  = "Aircraft info";
            infoPanelTitle.color = new Color(0.3f, 0.85f, 1f);
        }

        if (manageLandingLabel != null)
        {
            manageLandingLabel.text  = "Manage landing";
            manageLandingLabel.color = new Color(0.3f, 0.85f, 1f);
        }

        if (infoIndex != null)
            infoIndex.text = FormatInfoRow("Index", data.callsign, COL_CALLSIGN);

        string countryCode = data.callsign.Length >= 2 ? data.callsign.Substring(0, 2).ToUpper() : "??";
        string countryName = GetCountryFromCode(countryCode);
        if (infoCountry != null)
            infoCountry.text = FormatInfoRow("Country", countryName, COL_SELECTED);

        string cargo = GetFakeCargo(data.callsign);
        if (infoCargo != null)
            infoCargo.text = FormatInfoRow("Cargo", cargo, COL_SELECTED);

        float height = 3000f + (Mathf.Abs(data.callsign.GetHashCode()) % 7000);
        if (infoHeight != null)
            infoHeight.text = FormatInfoRow("Height", string.Format("{0:F0} m", height), COL_SPEED_TXT);

        if (infoSpeed != null)
            infoSpeed.text = FormatInfoRow("Speed", string.Format("{0:F0} km/h", data.speed), COL_SPEED_TXT);
    }

    string FormatInfoRow(string label, string value, string valueColorHex)
    {
        return string.Format("<color={0}>{1}:</color>  <color={2}><b>{3}</b></color>", COL_LABEL, label, valueColorHex, value);
    }

    string GetCountryFromCode(string code)
    {
        switch (code)
        {
            case "GH": return "Ghana";
            case "LT": return "Lithuania";
            case "UA": return "Ukraine";
            case "PL": return "Poland";
            case "DE": return "Germany";
            case "FR": return "France";
            case "US": return "United States";
            case "RU": return "Russia";
            case "TR": return "Turkey";
            case "IT": return "Italy";
            default:   return code + " (Unknown)";
        }
    }

    string GetFakeCargo(string callsign)
    {
        string[] cargos = { "Food", "Medical", "Passengers", "Mail", "Electronics", "Fuel", "Machinery" };
        int idx = Mathf.Abs(callsign.GetHashCode()) % cargos.Length;
        return cargos[idx];
    }

    void HideAircraftInfo()
    {
        if (aircraftInfoPanel != null)
            aircraftInfoPanel.SetActive(false);
    }

    void RefreshButtons()
    {
        bool canDecide = selectedIndex >= 0;

        if (approveButton != null)
        {
            approveButton.interactable = canDecide;
            Image img = approveButton.GetComponent<Image>();
            if (img != null) img.color = canDecide ? approveNormalColor : disabledColor;
        }

        if (denyButton != null)
        {
            denyButton.interactable = canDecide;
            Image img = denyButton.GetComponent<Image>();
            if (img != null) img.color = canDecide ? denyNormalColor : disabledColor;
        }

        if (!canDecide && selectedLabel != null)
            selectedLabel.text = string.Format("<color={0}>SELECT A FLIGHT FROM THE LIST</color>", COL_DIM);
    }

    // ── Decisions ─────────────────────────────────────────────────────────
    void OnApproveClicked()
    {
        if (selectedIndex < 0) return;
        var flights = FlightDataManager.Instance.savedFlights;
        if (selectedIndex >= flights.Count) return;

        string callsign = flights[selectedIndex].callsign;
        FlightDataManager.Instance.AddDecision(callsign, true);

        if (selectedLabel != null)
            selectedLabel.text = string.Format("<color={0}><b>{1}  -  LANDING APPROVED</b></color>", COL_APPROVED, callsign);

        selectedIndex = -1;
        HideAircraftInfo();
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
            selectedLabel.text = string.Format("<color={0}><b>{1}  -  LANDING DENIED</b></color>", COL_DENIED, callsign);

        selectedIndex = -1;
        HideAircraftInfo();
        RefreshButtons();
        DisplayFlights();
    }
}
