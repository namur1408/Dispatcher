using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BookTutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;
    public Button returnButton;

    [Header("Timing Settings")]
    public float initialDelay = 1f;
    public float typeSpeed    = 0.04f;
    public float msgWaitTime  = 4f;

    private bool isManualTabClicked = false;
    private bool isReturnClicked    = false;
    private bool skipRequested      = false;

    public static bool isBookTutorialCompleted = false;

    private string msg1 = "This is your Shift Tasks page. Here you will find your daily goals and restrictions.";
    private string msg2 = "Always check this before starting! Now, click on the 'Manual' tab to read the instructions.";
    private string msg3 = "The Manual explains how to use the Radar, Terminal, and Radio. Read it carefully.";
    private string msg4 = "When you are ready to begin your shift, click 'Return' to close the book.";

    void Start()
    {
        if (isBookTutorialCompleted)
        {
            if (subtitlePanel  != null) subtitlePanel.SetActive(false);
            if (returnButton   != null) returnButton.interactable = true;
            return;
        }

        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (returnButton  != null) returnButton.interactable = false;

        StartCoroutine(BookTutorialSequence());
    }

    IEnumerator BookTutorialSequence()
    {
        // FIX: Use WaitForSecondsRealtime for the initial delay so it works
        //      even if timeScale happens to be 0 when the scene loads.
        yield return new WaitForSecondsRealtime(initialDelay);

        if (subtitlePanel != null) subtitlePanel.SetActive(true);

        yield return StartCoroutine(TypeText(msg1));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));

        yield return StartCoroutine(TypeText(msg2));
        yield return new WaitUntil(() => isManualTabClicked);

        yield return StartCoroutine(TypeText(msg3));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));

        if (returnButton != null) returnButton.interactable = true;

        yield return StartCoroutine(TypeText(msg4));

        // Wait for either a click on the dialogue panel OR the return button
        yield return new WaitUntil(() => skipRequested || isReturnClicked);

        if (subtitlePanel != null) subtitlePanel.SetActive(false);

        // If they skipped the text but haven't pressed Return yet, wait for it
        if (!isReturnClicked)
            yield return new WaitUntil(() => isReturnClicked);

        isBookTutorialCompleted = true;
    }

    public void PlayerClickedManualTab() { isManualTabClicked = true; }
    public void PlayerClickedReturn()    { isReturnClicked    = true; }
    public void OnDialogueClicked()      { skipRequested      = true; }

    IEnumerator WaitWithSkip(float time)
    {
        float timer = time;
        skipRequested = false;
        while (timer > 0f && !skipRequested)
        {
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }
        skipRequested = false;
    }

    IEnumerator TypeText(string textToType)
    {
        if (subtitleText == null) yield break;

        skipRequested     = false;
        subtitleText.text = "";

        foreach (char c in textToType.ToCharArray())
        {
            if (skipRequested)
            {
                subtitleText.text = textToType;
                break;
            }
            subtitleText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        skipRequested = false;
    }
}
