using System.Collections;
using UnityEngine;

public class BigRadarTerminal : MonoBehaviour
{
    public static BigRadarTerminal Instance;

    public TerminalTypewriter topInfoText;
    public TerminalTypewriter selectedPlaneText;

    private int currentPlaneCount = -1;
    private UIAirplane currentSelectedPlane = null;

    private bool isStartupSequenceDone = false;

    private float refreshTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        isStartupSequenceDone = false;
        currentPlaneCount = -1;
        currentSelectedPlane = null;
        StartCoroutine(StartupSequence());
    }

    IEnumerator StartupSequence()
    {
        SetPlaneCount(0);
        yield return new WaitUntil(() => topInfoText == null || !topInfoText.IsTyping);
        isStartupSequenceDone = true;

        if (currentSelectedPlane == null)
        {
            ClearSelection();
        }
    }

    void Update()
    {
        if (currentSelectedPlane != null && isStartupSequenceDone)
        {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0)
            {
                refreshTimer = 0.5f;
                UpdateSelectedPlaneUI(true);
            }
        }
    }

    public void SetPlaneCount(int count)
    {
        if (currentPlaneCount != count)
        {
            currentPlaneCount = count;
            UpdateTopPanel();
            if (isStartupSequenceDone && currentSelectedPlane == null)
            {
                ClearSelection();
            }
        }
    }

    private void UpdateTopPanel()
    {
        if (topInfoText != null)
        {
            topInfoText.SetText($">DEFCON: 5\n>TARGETS: {currentPlaneCount}");
        }
    }

    public void SelectPlane(UIAirplane plane)
    {
        // Removed selection blocking during loading (!isStartupSequenceDone)
        if (plane == null) return;
        if (currentSelectedPlane == plane) return;

        currentSelectedPlane = plane;
        UpdateSelectedPlaneUI(false);
    }

    private void UpdateSelectedPlaneUI(bool isLiveUpdate)
    {
        if (currentSelectedPlane == null) return;


        bool isTransit = currentSelectedPlane.targetPosition != Vector2.zero && string.IsNullOrEmpty(currentSelectedPlane.assignedRunway);

        // Blocking the radio for transit aircraft
        if (isTransit)
        {
            if (RadioManager.activeCallsign == currentSelectedPlane.callsignText.text)
            {
                RadioManager.activeCallsign = "";
            }
        }
        else
        {
            if (RadioManager.activeCallsign != currentSelectedPlane.callsignText.text)
            {
                RadioManager.activeCallsign = currentSelectedPlane.callsignText.text;

                if (FlightDataManager.Instance != null)
                {
                    var fData = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == currentSelectedPlane.callsignText.text);
                    if (fData != null)
                    {
                        var state = FlightDataManager.Instance.GetOrCreateInterrogationState(fData.callsign);
                        if (!state.isCargoKnown)
                        {
                            RadioManager.isNewCall = true;
                        }
                    }
                }
            }
        }

        string fullText = "";

        if (isTransit)
        {
            fullText = $"  SELECTED TARGET\n\n" +
                       $">CALLSIGN: {currentSelectedPlane.callsignText.text}\n" +
                       $">SPEED:    {currentSelectedPlane.speed * 5f} KTS\n" +
                       $">TYPE:     <color=#00BFFF>TRANSIT (XSIT)</color>";
        }
        else
        {
            string statusString = currentSelectedPlane.dispatchStatus.ToString().ToUpper();
            string colorHex = "#FFFFFF";

            if (currentSelectedPlane.dispatchStatus == UIAirplane.DispatchStatus.Approved) colorHex = "#00FF00";
            if (currentSelectedPlane.dispatchStatus == UIAirplane.DispatchStatus.Denied) colorHex = "#FF0000";

            int liveFuel = Mathf.RoundToInt(currentSelectedPlane.currentFuel);
            string fuelDisplay = liveFuel > 0 ? $"{liveFuel} L" : "<color=#FF0000>CRITICAL (0 L)</color>";

            string cargoInfo = "NONE";

            if (FlightDataManager.Instance != null)
            {
                var flightData = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == currentSelectedPlane.callsignText.text);
                if (flightData != null)
                {
                    var state = FlightDataManager.Instance.GetOrCreateInterrogationState(flightData.callsign);
                    if (!state.isCargoKnown)
                    {
                        cargoInfo = "<color=#FF0000>UNKNOWN</color>";
                    }
                    else
                    {
                        string cUnit = "";
                        string cargoName = flightData.cargo.ToUpper();
                        if (cargoName == "MEDICINES") cUnit = " BOX";
                        else if (cargoName == "FOOD") cUnit = " KG";
                        else if (cargoName == "FUEL") cUnit = " L";
                        else if (cargoName == "PEOPLE") cUnit = " PPL";

                        if (cargoName != "NONE")
                            cargoInfo = $"{cargoName} ({flightData.cargoAmount}{cUnit})";
                    }
                }
            }

            fullText = $"  SELECTED TARGET\n\n" +
                       $">CALLSIGN: {currentSelectedPlane.callsignText.text}\n" +
                       $">SPEED:    {currentSelectedPlane.speed * 5f} KTS\n" +
                       $">CARGO:    <color=#FFD700>{cargoInfo}</color>\n" +
                       $">FUEL:     {fuelDisplay}\n" +
                       $">STATUS:   <color={colorHex}>{statusString}</color>";
        }

        if (isLiveUpdate)
        {
            selectedPlaneText.UpdateTextInstant(fullText);
        }
        else
        {
            selectedPlaneText.SetText(fullText);
        }
    }

    public void ClearSelection()
    {
        currentSelectedPlane = null;

        RadioManager.activeCallsign = "";


        if (selectedPlaneText != null && isStartupSequenceDone)
        {
            string clearText = currentPlaneCount == 0 ? ">AWAITING INPUT..." : ">NO TARGET SELECTED";
            selectedPlaneText.UpdateTextInstant(clearText);
        }
    }
}
