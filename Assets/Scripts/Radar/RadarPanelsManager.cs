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
    public GameObject departureMarkerPrefab; // Optional prefab for the departure destination marker
    
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
    private GameObject departureMarkerInstance;
    private Sprite departureCircleSprite;

    private float refreshTimer = 1f;
    // Cache TMP references per button to avoid GetComponentInChildren every frame
    private System.Collections.Generic.Dictionary<Button, TextMeshProUGUI> buttonTextCache
        = new System.Collections.Generic.Dictionary<Button, TextMeshProUGUI>();

    private void FixHeaderAnchors(GameObject window)
    {
        if (window == null) return;
        
        Transform headerTransform = null;
        for (int i = 0; i < window.transform.childCount; i++)
        {
            Transform child = window.transform.GetChild(i);
            if (child.name.ToLower().Contains("header"))
            {
                headerTransform = child;
                break;
            }
        }

        if (headerTransform != null)
        {
            RectTransform rect = headerTransform.GetComponent<RectTransform>();
            if (rect != null)
            {
                float currentHeight = rect.rect.height;
                if (currentHeight <= 0) currentHeight = 45f;
                
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                
                rect.offsetMin = new Vector2(0f, -currentHeight);
                rect.offsetMax = new Vector2(0f, 0f);
                
                Debug.Log($"[UI] Programmatically set {window.name}'s Header to Top-Stretch with height {currentHeight}");
            }
        }
    }

    private void Awake()
    {
        Instance = this;
        if (runwaySelectionPanel != null) runwaySelectionPanel.SetActive(false);
        
        SetupRunwayButtons();
        
        FixHeaderAnchors(arrivalsWindow);
        FixHeaderAnchors(transitsWindow);
        FixHeaderAnchors(departuresWindow);
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

        // Update runway buttons interactability
        if (runwaySelectionPanel != null && runwaySelectionPanel.activeInHierarchy)
        {
            foreach (var cfg in runwayButtons)
            {
                if (cfg.button != null)
                {
                    bool isOccupied = RunwayManager.Instance != null && RunwayManager.Instance.IsRunwayOccupied(cfg.runwayId);
                    
                    bool isDeparture = selectedFlightForRunway != null && (selectedFlightForRunway.isReadyToDepart || selectedFlightForRunway.isDeparting);
                    if (isDeparture) isOccupied = false;

                    if (cfg.button.interactable == isOccupied)
                    {
                        cfg.button.interactable = !isOccupied;
                        
                        // Use cached TMP reference
                        if (!buttonTextCache.TryGetValue(cfg.button, out TextMeshProUGUI txt))
                        {
                            txt = cfg.button.GetComponentInChildren<TextMeshProUGUI>();
                            buttonTextCache[cfg.button] = txt;
                        }
                        if (txt != null)
                        {
                            Color c = txt.color;
                            c.a = isOccupied ? 0.3f : 1.0f;
                            txt.color = c;
                        }
                    }
                }
            }
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

            if (flight.isReadyToDepart)
            {
                // Обслуженный самолёт, ожидающий назначения полосы вылета
                newEntry = CreateEntry(flight, departuresContent);
            }
            else if (flight.isDeparting && !flight.hasTakenOff)
            {
                // Самолёт на полосе, но ещё не оторвался от земли (показываем белым)
                newEntry = CreateEntry(flight, departuresContent);
            }
            else if (flight.targetPosition != Vector2.zero && string.IsNullOrEmpty(flight.assignedRunway))
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
            HideDepartureDestination();
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
            string dest;
            string status = data.assignedRunway != "" ? $"[RWY {data.assignedRunway}]" : "";

            if (data.isReadyToDepart)
            {
                // Ожидает назначения полосы в Departures
                dest = string.IsNullOrEmpty(data.departureDestination) ? "DEPART" : data.departureDestination;
                text.text = $"{data.callsign} | {dest} | {status}";
                // Жёлтый — ждёт полосы
                text.color = string.IsNullOrEmpty(data.assignedRunway) ? Color.yellow : Color.green;
            }
            else if (data.isDeparting)
            {
                dest = string.IsNullOrEmpty(data.departureDestination) ? "DEPART" : data.departureDestination;
                text.text = $"{data.callsign} | {dest} | {status}";
                text.color = Color.white;
            }
            else
            {
                if (!string.IsNullOrEmpty(data.assignedRunway))
                {
                    dest = "LANDING";
                }
                else
                {
                    dest = data.targetPosition == Vector2.zero ? "BASE" : "TRANSIT";
                }
                text.text = $"{data.callsign} | {dest} | {status}";
                // Highlight if approved or assigned a runway
                if (data.approved && string.IsNullOrEmpty(data.assignedRunway))
                {
                    text.color = Color.green;
                }
                else if (!string.IsNullOrEmpty(data.assignedRunway))
                {
                    text.color = Color.green;
                }
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
        // Allow runway assignment if:
        // - прилёт, одобрен, нет полосы
        // - вылетает (isDeparting), нет полосы
        // - готов к вылету (isReadyToDepart), нет полосы (ждёт назначения)
        bool canAssign = (!data.isDeparting && !data.isReadyToDepart && data.approved && string.IsNullOrEmpty(data.assignedRunway))
                      || (data.isDeparting && string.IsNullOrEmpty(data.assignedRunway))
                      || (data.isReadyToDepart && string.IsNullOrEmpty(data.assignedRunway));

        if (canAssign)
        {
            // Toggle off if clicking the same flight
            if (selectedFlightForRunway == data && runwaySelectionPanel != null && runwaySelectionPanel.activeSelf)
            {
                runwaySelectionPanel.SetActive(false);
                selectedFlightForRunway = null;
                runwaySelectionPanel.transform.SetParent(this.transform, false);
                HideDepartureDestination();
                return;
            }

            selectedFlightForRunway = data;
            if (runwaySelectionPanel != null)
            {
                Debug.Log($"[Runway] Showing panel for: {data.callsign}");
                runwaySelectionPanel.transform.SetParent(clickedEntry.transform.parent, false);
                runwaySelectionPanel.transform.SetSiblingIndex(clickedEntry.transform.GetSiblingIndex() + 1);
                runwaySelectionPanel.SetActive(true);

                if (data.isReadyToDepart || data.isDeparting)
                {
                    ShowDepartureDestination(data.departureDestination);
                }
                else
                {
                    HideDepartureDestination();
                }

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
        bool isDeparture = selectedFlightForRunway.isReadyToDepart || selectedFlightForRunway.isDeparting;
        if (!isDeparture && RunwayManager.Instance != null && RunwayManager.Instance.IsRunwayOccupied(runwayId))
        {
            Debug.LogWarning($"[Runway] Runway {runwayId} is occupied!");
            return;
        }

        // 1. Обновляем данные
        selectedFlightForRunway.assignedRunway = runwayId;
        selectedFlightForRunway.isAligningToLand = !selectedFlightForRunway.isDeparting && !selectedFlightForRunway.isReadyToDepart;

        if (selectedFlightForRunway.isReadyToDepart)
        {
            if (RadarManager.Instance != null)
            {
                selectedFlightForRunway.hasBeenPinged = true; // Force visibility
                RadarManager.Instance.SpawnDepartingNow(selectedFlightForRunway);
            }
        }
        else
        {
            // 2b. Обычный самолёт уже в сцене — находим и назначаем полосу
            UIAirplane[] allPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
            foreach (var plane in allPlanes)
            {
                if (plane != null && (plane.originalCallsign == selectedFlightForRunway.callsign ||
                                     (plane.callsignText != null && plane.callsignText.text == selectedFlightForRunway.callsign)))
                {
                    plane.SetAssignedRunway(runwayId, !selectedFlightForRunway.isDeparting);
                }
            }
        }

        // 3. UI Cleanup
        if (runwaySelectionPanel != null)
        {
            runwaySelectionPanel.SetActive(false);
            runwaySelectionPanel.transform.SetParent(this.transform, false);
        }
        HideDepartureDestination();
        selectedFlightForRunway = null;
        RefreshLists();
    }

    private void ShowDepartureDestination(string destinationName)
    {
        if (string.IsNullOrEmpty(destinationName)) return;

        BigRadarLoader loader = Object.FindFirstObjectByType<BigRadarLoader>();
        if (loader == null || loader.radarContent == null) return;

        if (departureMarkerInstance != null)
        {
            Destroy(departureMarkerInstance);
        }

        Vector2 destPos = GetDestinationCoordinate(destinationName);
        if (destPos != Vector2.zero)
        {
            destPos = destPos.normalized * 380f;
        }

        if (departureMarkerPrefab != null)
        {
            departureMarkerInstance = Instantiate(departureMarkerPrefab, loader.radarContent, false);
            RectTransform rect = departureMarkerInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = destPos;
            }

            TextMeshProUGUI labelText = departureMarkerInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.text = $"➔ {destinationName.ToUpper()}";
            }

            Debug.Log($"[DepartureMarker] Created prefab marker for {destinationName} at {destPos}");
        }
        else
        {
            // Create marker gameobject
            departureMarkerInstance = new GameObject("DepartureDestinationMarker", typeof(RectTransform));
            departureMarkerInstance.transform.SetParent(loader.radarContent, false);
            
            RectTransform rect = departureMarkerInstance.GetComponent<RectTransform>();
            rect.anchoredPosition = destPos;
            rect.sizeDelta = new Vector2(40f, 40f);

            if (departureCircleSprite == null)
            {
                departureCircleSprite = CreateCircleSprite();
            }

            // Pulsing radar-green outer ring
            GameObject pulseCircle = new GameObject("PulseCircle", typeof(RectTransform), typeof(Image));
            pulseCircle.transform.SetParent(departureMarkerInstance.transform, false);
            RectTransform pulseRect = pulseCircle.GetComponent<RectTransform>();
            pulseRect.anchoredPosition = Vector2.zero;
            pulseRect.sizeDelta = new Vector2(30f, 30f);
            Image pulseImg = pulseCircle.GetComponent<Image>();
            pulseImg.color = new Color(0f, 1f, 0f, 0.4f);
            if (departureCircleSprite != null) pulseImg.sprite = departureCircleSprite;
            pulseCircle.AddComponent<DestinationPulseEffect>();

            // Center solid green dot
            GameObject centerDot = new GameObject("CenterDot", typeof(RectTransform), typeof(Image));
            centerDot.transform.SetParent(departureMarkerInstance.transform, false);
            RectTransform dotRect = centerDot.GetComponent<RectTransform>();
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.sizeDelta = new Vector2(10f, 10f);
            Image dotImg = centerDot.GetComponent<Image>();
            dotImg.color = new Color(0f, 1f, 0f, 0.9f);
            if (departureCircleSprite != null) dotImg.sprite = departureCircleSprite;

            // Create text label
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(departureMarkerInstance.transform, false);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0f, -25f);
            labelRect.sizeDelta = new Vector2(150f, 30f);

            TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
            labelText.text = $"➔ {destinationName.ToUpper()}";
            labelText.fontSize = 13f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0f, 1f, 0f, 0.9f);
            
            labelText.outlineColor = Color.black;
            labelText.outlineWidth = 0.2f;

            Debug.Log($"[DepartureMarker] Created pulsing marker for {destinationName} at {destPos}");
        }
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        float radius = size / 2f;
        float centerX = size / 2f;
        float centerY = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distSq = dx * dx + dy * dy;

                if (distSq <= radius * radius)
                {
                    float dist = Mathf.Sqrt(distSq);
                    float edgeDist = radius - dist;
                    float alpha = Mathf.Clamp01(edgeDist / 1.5f); // Smooth anti-aliased edge
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void HideDepartureDestination()
    {
        if (departureMarkerInstance != null)
        {
            Destroy(departureMarkerInstance);
            departureMarkerInstance = null;
        }
    }

    private Vector2 GetDestinationCoordinate(string destination)
    {
        if (string.IsNullOrEmpty(destination)) return Vector2.zero;

        switch (destination)
        {
            case "Bastion-1": return new Vector2(-416f, 476f);
            case "Bastion-2": return new Vector2(400f, 400f);
            case "Bastion-3": return new Vector2(-535f, 119f);
            case "Bastion-4": return new Vector2(0f, 535f);
            case "Bastion-5": return new Vector2(437f, -357f);
            case "Bastion-6": return new Vector2(-450f, -400f);
            case "Bastion-7": return new Vector2(500f, 100f);
            case "Bastion-8": return new Vector2(150f, -500f);
            case "Bastion-9": return new Vector2(-200f, 500f);
            case "Sector-Z":  return new Vector2(0f, 535f);
            default:
                // Fallback: calculate a stable position based on the name's hash code
                int hash = destination.GetHashCode();
                float angle = Mathf.Abs(hash % 360) * Mathf.Deg2Rad;
                float radius = 480f + Mathf.Abs(hash % 50);
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }
}

public class DestinationPulseEffect : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image img;
    private float timer = 0f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        img = GetComponent<Image>();
    }

    void Update()
    {
        timer += Time.deltaTime * 3.5f;
        
        // Pulse size (scale from 0.8 to 2.2)
        float scale = 0.8f + Mathf.PingPong(timer, 1.4f);
        if (rectTransform != null)
        {
            rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        // Fade alpha with pulse
        if (img != null)
        {
            float alpha = Mathf.Max(0.05f, 0.6f - (scale - 0.8f) * 0.4f);
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}
