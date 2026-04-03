using UnityEngine;
using UnityEngine.UI;

public class BookTabManager : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button instructionsTabButton;
    [SerializeField] private Button dailyRulesTabButton;

    [Header("Content Panels")]
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject dailyRulesPanel;

    private void Start()
    {
        // Programmatically bind the buttons to the switch methods
        if (instructionsTabButton != null) 
            instructionsTabButton.onClick.AddListener(ShowInstructions);
            
        if (dailyRulesTabButton != null) 
            dailyRulesTabButton.onClick.AddListener(ShowDailyRules);

        // Set default state on load
        ShowDailyRules();
    }

    private void ShowInstructions()
    {
        instructionsPanel.SetActive(true);
        dailyRulesPanel.SetActive(false);
        UpdateButtonVisuals(instructionsTabButton, dailyRulesTabButton);
    }

    private void ShowDailyRules()
    {
        instructionsPanel.SetActive(false);
        dailyRulesPanel.SetActive(true);
        UpdateButtonVisuals(dailyRulesTabButton, instructionsTabButton);
    }

    // Optional: Make the active tab look pressed or different color
    private void UpdateButtonVisuals(Button activeBtn, Button inactiveBtn)
    {
        if (activeBtn != null) activeBtn.interactable = false; // Disable clicking the already active tab
        if (inactiveBtn != null) inactiveBtn.interactable = true; // Enable clicking the other tab
    }
}