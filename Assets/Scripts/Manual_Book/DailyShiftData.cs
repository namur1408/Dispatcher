using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Day_01_Rules", menuName = "Game Data/Daily Shift Rules")]
public class DailyShiftData : ScriptableObject
{
    [Header("Shift Info")]
    public int dayNumber = 1;
    [TextArea(3, 5)]
    public string shiftBriefing = "Enter the daily briefing here...";

    [Header("Flight Rules (String Tags)")]
    [Tooltip("Enter flight prefixes ALLOWED to land (e.g., QY, GE)")]
    public List<string> allowedTags = new List<string>();

    [Tooltip("Enter flight prefixes STRICTLY FORBIDDEN to land")]
    public List<string> forbiddenTags = new List<string>();
}