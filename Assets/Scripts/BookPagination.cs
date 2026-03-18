using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BookPagination : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image bookDisplay;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private LightFlicker lightTransition;

    [Header("Book Data")]
    [SerializeField] private List<Sprite> bookPages = new List<Sprite>();

    [Header("Timing Settings")]
    [Tooltip("How long the screen stays black before the page changes")]
    [SerializeField] private float darkDelay = 0.3f; // You can adjust 0.2 - 0.4 here

    private int _currentPageIndex = 0;
    private bool _isChangingPage = false; // Prevents spam clicking

    private void Start()
    {
        // Bind buttons to the Coroutine methods
        if (nextButton != null) nextButton.onClick.AddListener(() => StartCoroutine(TurnPageRoutine(1)));
        if (prevButton != null) prevButton.onClick.AddListener(() => StartCoroutine(TurnPageRoutine(-1)));
        
        UpdatePageVisualsOnly();
    }

    private IEnumerator TurnPageRoutine(int direction)
    {
        // Exit if a transition is already happening
        if (_isChangingPage) yield break;

        int nextPageIndex = _currentPageIndex + direction;

        // Exit if we are at the beginning or end of the book
        if (nextPageIndex < 0 || nextPageIndex >= bookPages.Count) yield break;

        // Lock the pagination system
        _isChangingPage = true;
        ToggleButtons(false);

        // 1. Fade the screen to black
        if (lightTransition != null)
        {
            yield return StartCoroutine(lightTransition.FadeOut());
        }

        // 2. Wait in the dark for the specified duration (0.2s - 0.4s)
        yield return new WaitForSeconds(darkDelay);

        // 3. Change the actual book image
        _currentPageIndex = nextPageIndex;
        bookDisplay.sprite = bookPages[_currentPageIndex];

        // 4. Fade the screen back to normal
        if (lightTransition != null)
        {
            yield return StartCoroutine(lightTransition.FadeIn());
        }

        // Unlock the pagination system and update button states
        ToggleButtons(true);
        _isChangingPage = false;
    }

    // Updates the image immediately (used only on Start)
    private void UpdatePageVisualsOnly()
    {
        if (bookPages.Count > 0 && bookDisplay != null)
        {
            bookDisplay.sprite = bookPages[_currentPageIndex];
            ToggleButtons(true);
        }
    }

    // Turns buttons on/off depending on page limits
    private void ToggleButtons(bool isInteractable)
    {
        if (prevButton != null) prevButton.interactable = isInteractable && (_currentPageIndex > 0);
        if (nextButton != null) nextButton.interactable = isInteractable && (_currentPageIndex < bookPages.Count - 1);
    }
}