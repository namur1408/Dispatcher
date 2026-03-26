using UnityEngine;
using TMPro;

public class BigRadarTerminal : MonoBehaviour
{
    public static BigRadarTerminal Instance;

    public TextMeshProUGUI topInfoText;
    public TextMeshProUGUI selectedPlaneText;

    private int currentPlaneCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ClearSelection();
        UpdateTopPanel();
    }

    public void SetPlaneCount(int count)
    {
        currentPlaneCount = count;
        UpdateTopPanel();
    }

    private void UpdateTopPanel()
    {
        if (topInfoText != null)
        {
            topInfoText.text = $">DEFCON: 5\n>TARGETS: {currentPlaneCount}";
        }
    }

    public void SelectPlane(UIAirplane plane)
    {
        if (selectedPlaneText == null || plane == null) return;

        string statusString = plane.dispatchStatus.ToString().ToUpper();
        string colorHex = "#FFFFFF";

        if (plane.dispatchStatus == UIAirplane.DispatchStatus.Approved) colorHex = "#00FF00"; // Зеленый
        if (plane.dispatchStatus == UIAirplane.DispatchStatus.Denied) colorHex = "#FF0000";   // Красный

        selectedPlaneText.text =
            $"--- SELECTED TARGET ---\n\n" +
            $">CALLSIGN: {plane.callsignText.text}\n" +
            $">SPEED:    {plane.speed * 10f} KTS\n" +
            $">STATUS:   <color={colorHex}>{statusString}</color>";
    }

    public void ClearSelection()
    {
        if (selectedPlaneText != null)
        {
            selectedPlaneText.text = ">NO TARGET SELECTED\n\n>AWAITING INPUT...";
        }
    }
}