using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BookPagination : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image bookDisplay;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    [Header("Book Data")]
    [SerializeField] private List<Sprite> bookPages = new List<Sprite>();

    private int _currentPageIndex = 0;

    private void Start()
    {
        // Bind buttons to the instant page turn method
        if (nextButton != null) nextButton.onClick.AddListener(() => TurnPage(1));
        if (prevButton != null) prevButton.onClick.AddListener(() => TurnPage(-1));
        
        UpdatePageVisualsOnly();
    }

    private void TurnPage(int direction)
    {
        int nextPageIndex = _currentPageIndex + direction;

        // Exit if we are at the beginning or end of the book
        if (nextPageIndex < 0 || nextPageIndex >= bookPages.Count) return;

        // Change the actual book image instantly
        _currentPageIndex = nextPageIndex;
        bookDisplay.sprite = bookPages[_currentPageIndex];

        // Update button states
        ToggleButtons();
    }

    // Updates the image immediately (used only on Start)
    private void UpdatePageVisualsOnly()
    {
        if (bookPages.Count > 0 && bookDisplay != null)
        {
            bookDisplay.sprite = bookPages[_currentPageIndex];
            ToggleButtons();
        }
    }

    // Turns buttons on/off depending on page limits
    private void ToggleButtons()
    {
        if (prevButton != null) prevButton.interactable = (_currentPageIndex > 0);
        if (nextButton != null) nextButton.interactable = (_currentPageIndex < bookPages.Count - 1);
    }
}