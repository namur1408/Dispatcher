using UnityEngine;
using TMPro;

public class DailyRulesDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text rulesTextDisplay;
    [SerializeField] private DailyShiftData currentDayData;

    private void Start() => UpdateRulesDisplay();

    public void UpdateRulesDisplay()
    {
        if (currentDayData == null || rulesTextDisplay == null) return;

        string finalMessage = $"DAY {currentDayData.dayNumber}\n\n";
        finalMessage += $"{currentDayData.shiftBriefing}\n\n";

        // Display Allowed Tags
        finalMessage += "<color=#2E7D32><b>CLEARED FOR LANDING:</b></color>\n";
        if (currentDayData.allowedTags.Count > 0)
        {
            foreach (string tag in currentDayData.allowedTags)
            {
                finalMessage += $"- Flights with code {tag}\n";
            }
        }
        else { finalMessage += "- None\n"; }

        finalMessage += "\n";

        // Display Forbidden Tags
        finalMessage += "<color=#C62828><b>LANDING DENIED:</b></color>\n";
        if (currentDayData.forbiddenTags.Count > 0)
        {
            foreach (string tag in currentDayData.forbiddenTags)
            {
                finalMessage += $"- Flights with code {tag}\n";
            }
        }
        else { finalMessage += "- No restrictions\n"; }

        rulesTextDisplay.text = finalMessage;
    }
}