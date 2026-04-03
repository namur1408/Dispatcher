using UnityEngine;

public class ShiftRulesManager : MonoBehaviour
{
    [SerializeField] private DailyShiftData todayRules; // Reference to the current day's rules

    // Method called when the player clicks the "Allow Landing" button
    public void CheckLandingPermission(string incomingPlaneTag) 
    {
        // 1. Check if the tag is in the FORBIDDEN list
        if (todayRules.forbiddenTags.Contains(incomingPlaneTag))
        {
            Debug.Log($"ERROR: Landing denied for flight code {incomingPlaneTag}!");
            // Trigger penalty logic here
            return; 
        }

        // 2. Check if the tag is in the ALLOWED list
        if (todayRules.allowedTags.Contains(incomingPlaneTag))
        {
            Debug.Log($"SUCCESS: Flight {incomingPlaneTag} landed safely.");
            // Trigger score logic here
            return;
        }

        // 3. Fallback if the tag is not in either list
        Debug.Log($"WARNING: Flight {incomingPlaneTag} is not on any list. Dispatcher's discretion.");
    }
}