using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TVTutorialManager : MonoBehaviour
{
    public static TVTutorialManager Instance;

    [Header("UI References")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;
    public Button returnButton;
    public Button allowButton;
    public Button denyButton;

    [Header("Mentor Settings")]
    public Color mentorNormalColor = Color.green;
    public Color mentorAngryColor = Color.red;
    public float typeSpeed = 0.04f;
    public float shakeMagnitude = 5f;

    private bool skipRequested = false;
    public static bool isTvTutorialCompleted = false;

    private bool isTargetDenied = false;
    private bool isTargetAllowedByMistake = false;
    private string targetCallsign = "KO-677";

    private string msgIntro = "Welcome to the Dispatch Terminal. Here you make the final call on every flight.";
    private string msgDetails = "On the right, you can see the flight's manifest and CARGO. At the bottom is your airport's CAPACITY. You can only land 5 planes per shift.";
    private string msgTask = "Find the flight with the 'KO' prefix in the list. Select it and press DENY to reject its landing request.";
    private string msgSuccess = "Excellent work. Now hit 'Return' and go back to the Radar. I need to show you how holding patterns work.";
    private string msgFail = "<size=120%>WHAT ARE YOU DOING?!</size>\n'KO' flights are prohibited! You put us at risk! Hit 'Return' and go back to the Radar immediately!";

    private Vector2 originalTextPos;

    void Awake() { Instance = this; }

    void Start()
    {
        if (subtitleText != null)
            originalTextPos = subtitleText.rectTransform.anchoredPosition;

        if (isTvTutorialCompleted || DeskTutorialManager.tutorialStep < 3)
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            return;
        }

        if (returnButton != null) returnButton.interactable = false;
        if (allowButton != null) allowButton.interactable = false;
        if (denyButton != null) denyButton.interactable = false;

        subtitlePanel.SetActive(false);
        StartCoroutine(TVTutorialSequence());
    }

    IEnumerator TVTutorialSequence()
    {
        yield return new WaitForSeconds(0.5f);

        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;

        yield return StartCoroutine(TypeText(msgIntro, false));
        yield return new WaitUntil(() => skipRequested);
        yield return StartCoroutine(TypeText(msgDetails, false));
        yield return new WaitUntil(() => skipRequested);
        yield return StartCoroutine(TypeText(msgTask, false));
        yield return new WaitUntil(() => skipRequested);
        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        if (allowButton != null) allowButton.interactable = true;
        if (denyButton != null) denyButton.interactable = true;
        while (!isTargetDenied && !isTargetAllowedByMistake)
        {
            yield return null;
        }
        if (allowButton != null) allowButton.interactable = false;
        if (denyButton != null) denyButton.interactable = false;

        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);

        if (isTargetAllowedByMistake)
        {
            subtitleText.color = mentorAngryColor;
            yield return StartCoroutine(TypeText(msgFail, true));
            yield return new WaitUntil(() => skipRequested);
        }
        else if (isTargetDenied)
        {
            subtitleText.color = mentorNormalColor;
            yield return StartCoroutine(TypeText(msgSuccess, false));
            yield return new WaitUntil(() => skipRequested);
        }
        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        if (returnButton != null) returnButton.interactable = true;
        if (allowButton != null) allowButton.interactable = true;
        if (denyButton != null) denyButton.interactable = true;

        isTvTutorialCompleted = true;
        DeskTutorialManager.tutorialStep = 4;
    }

    public void NotifyFlightAllowed(string callsign) { if (callsign == targetCallsign) isTargetAllowedByMistake = true; }
    public void NotifyFlightDenied(string callsign) { if (callsign == targetCallsign) isTargetDenied = true; }
    public void OnDialogueClicked() { skipRequested = true; }

    IEnumerator TypeText(string textToType, bool shake)
    {
        skipRequested = false;
        subtitleText.text = textToType;
        subtitleText.maxVisibleCharacters = 0;
        subtitleText.ForceMeshUpdate();
        int totalVisibleCharacters = subtitleText.textInfo.characterCount;
        RectTransform rt = subtitleText.rectTransform;

        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            if (skipRequested) { subtitleText.maxVisibleCharacters = totalVisibleCharacters; break; }
            subtitleText.maxVisibleCharacters = i;
            if (shake) rt.anchoredPosition = originalTextPos + new Vector2(Random.Range(-shakeMagnitude, shakeMagnitude), Random.Range(-shakeMagnitude, shakeMagnitude));
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        rt.anchoredPosition = originalTextPos;
        skipRequested = false;
    }
}