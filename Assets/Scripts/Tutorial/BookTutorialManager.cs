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
    public float typeSpeed = 0.04f;
    public float msgWaitTime = 4f;

    private bool isManualTabClicked = false;
    private bool isReturnClicked = false;
    private bool skipRequested = false;

    public static bool isBookTutorialCompleted = false;

    private string msg1 = "This is your Shift Tasks page. Here you will find your daily goals and restrictions.";
    private string msg2 = "Always check this before starting! Now, click on the 'Manual' tab to read the instructions.";
    private string msg3 = "The Manual explains how to use the Radar, Terminal, and Radio. Read it carefully.";
    private string msg4 = "When you are ready to begin your shift, click 'Return' to close the book.";

    void Start()
    {
        if (isBookTutorialCompleted)
        {
            subtitlePanel.SetActive(false);
            if (returnButton != null) returnButton.interactable = true;
            return;
        }

        subtitlePanel.SetActive(false);
        if (returnButton != null) returnButton.interactable = false; 
        StartCoroutine(BookTutorialSequence());
    }

    IEnumerator BookTutorialSequence()
    {
        yield return new WaitForSecondsRealtime(initialDelay);

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg1));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));
        yield return StartCoroutine(TypeText(msg2));

        yield return new WaitUntil(() => isManualTabClicked);

        yield return StartCoroutine(TypeText(msg3));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));

        if (returnButton != null) returnButton.interactable = true;

        yield return StartCoroutine(TypeText(msg4));

        yield return new WaitUntil(() => skipRequested || isReturnClicked);

        subtitlePanel.SetActive(false);

        if (!isReturnClicked)
        {
            yield return new WaitUntil(() => isReturnClicked);
        }

        isBookTutorialCompleted = true;
    }

    public void PlayerClickedManualTab() { isManualTabClicked = true; }
    public void PlayerClickedReturn() { isReturnClicked = true; }
    public void OnDialogueClicked() { skipRequested = true; }

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
        skipRequested = false;
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