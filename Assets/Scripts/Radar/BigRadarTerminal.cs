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

    void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        SetPlaneCount(0);
        yield return new WaitUntil(() => !topInfoText.IsTyping);
        isStartupSequenceDone = true;
        ClearSelection();
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
        if (!isStartupSequenceDone) return;

        if (selectedPlaneText == null || plane == null) return;

        currentSelectedPlane = plane;

        string statusString = plane.dispatchStatus.ToString().ToUpper();
        string colorHex = "#FFFFFF";

        if (plane.dispatchStatus == UIAirplane.DispatchStatus.Approved) colorHex = "#00FF00";
        if (plane.dispatchStatus == UIAirplane.DispatchStatus.Denied) colorHex = "#FF0000";

        selectedPlaneText.SetText(
            $"  SELECTED TARGET\n\n" +
            $">CALLSIGN: {plane.callsignText.text}\n" +
            $">SPEED:    {plane.speed * 10f} KTS\n" +
            $">STATUS:   <color={colorHex}>{statusString}</color>");
    }

    public void ClearSelection()
    {
        currentSelectedPlane = null;

        if (selectedPlaneText != null && isStartupSequenceDone)
        {
            if (currentPlaneCount == 0)
            {
                selectedPlaneText.SetText(">AWAITING INPUT...");
            }
            else
            {
                selectedPlaneText.SetText(">NO TARGET SELECTED");
            }
        }
    }
}