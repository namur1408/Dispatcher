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

    [Header("OS Interactions")]
    public Button desktopMailIcon;
    public Button desktopRadarIcon;

    [Header("AirTraffic App References")]
    public Button returnButton;
    public Button allowButton;
    public Button denyButton;
    public Button resourcesTabButton;
    public Button backToFlightsButton;

    [Header("Mentor Settings")]
    public Color mentorNormalColor = Color.green;
    public Color mentorAngryColor = Color.red;
    public float typeSpeed = 0.04f;
    public float shakeMagnitude = 5f;

    private bool skipRequested = false;
    public static bool isTvTutorialCompleted = false;

    private bool hasOpenedMail = false;
    private bool hasOpenedRadarApp = false;
    private bool hasOpenedResources = false;
    private bool hasReturnedToFlights = false;

    private bool isTargetDenied = false;
    private bool isTargetAllowedByMistake = false;
    private string targetCallsign = "KO-677";

    private string msgOsIntro = "Welcome to Aegis OS. This terminal is the brain of our operations.";
    private string msgOpenMail = "You have a new unread message. Click the 'Inbox' icon to read today's directives.";
    private string msgReadMail = "Good. Directives dictate who we can accept and who we must turn away. Ignoring them leads to... severe consequences.";
    private string msgOpenApp = "Now close the Inbox and launch 'ATC_Sys.exe' to manage the incoming flights.";

    private string msgLock = "Listen carefully: once you press [ALLOW] or [DENY], the plane's route is LOCKED. You can NO LONGER change its path on the radar.";
    private string msgDetails = "On the right, you can see the flight's CARGO. At the bottom is your CAPACITY. You can only land 5 planes at a time.";
    private string msgGoToRes = "Let's look at ground ops. Click the 'Resources' tab at the bottom to continue.";

    private string msgRes1 = "It's empty right now but when a plane lands, it replaces the '[ NO PLANES ]' tag.";
    private string msgRes2 = "Then you need to click the plane's name to open details, then [UNLOAD] its cargo into our warehouse and use our stocks to [REFUEL] or [REPAIR] it.";
    private string msgRes3 = "If you don't fully service a plane before the day ends, it will block these runways until you finish the work on that aircraft!";
    private string msgGoBack = "Understood? Now click 'Back' to go back to the main flight list.";

    private string msgTask = "Time for your first test. Find the flight with the 'KO' prefix. Select it and press DENY. We don't accept them.";
    private string msgSuccess = "Excellent work. Now hit 'Shut Down' at the bottom left and go back to the physical Radar.";
    private string msgFail = "Incorrect. I specifically told you to DENY the 'KO' prefix. Pay attention! Now go back to the Radar.";

    private Vector2 originalTextPos;

    void Awake() { Instance = this; }

    void Start()
    {
        if (subtitleText != null)
            originalTextPos = subtitleText.rectTransform.anchoredPosition;

        if (desktopMailIcon != null)
            desktopMailIcon.onClick.AddListener(() => { hasOpenedMail = true; });

        if (desktopRadarIcon != null)
            desktopRadarIcon.onClick.AddListener(() => { hasOpenedRadarApp = true; });

        if (resourcesTabButton != null)
            resourcesTabButton.onClick.AddListener(() => { hasOpenedResources = true; });

        if (backToFlightsButton != null)
            backToFlightsButton.onClick.AddListener(() => { hasReturnedToFlights = true; });

        if (isTvTutorialCompleted || DeskTutorialManager.tutorialStep < 3)
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            return;
        }

        if (desktopRadarIcon != null) desktopRadarIcon.interactable = false;
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

        yield return StartCoroutine(TypeText(msgOsIntro, false));
        yield return new WaitUntil(() => skipRequested);

        yield return StartCoroutine(TypeText(msgOpenMail, false));
        skipRequested = false;
        yield return new WaitUntil(() => hasOpenedMail);
        subtitlePanel.SetActive(false);

        yield return new WaitForSecondsRealtime(0.5f);

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msgReadMail, false));
        yield return new WaitUntil(() => skipRequested);

        if (desktopRadarIcon != null) desktopRadarIcon.interactable = true;

        yield return StartCoroutine(TypeText(msgOpenApp, false));
        skipRequested = false;
        yield return new WaitUntil(() => hasOpenedRadarApp);
        subtitlePanel.SetActive(false);

        yield return new WaitForSecondsRealtime(0.5f);

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msgLock, false));
        yield return new WaitUntil(() => skipRequested);
        yield return StartCoroutine(TypeText(msgGoToRes, false));
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        skipRequested = false;

        yield return new WaitUntil(() => hasOpenedResources);

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msgRes1, false));
        yield return new WaitUntil(() => skipRequested);
        yield return StartCoroutine(TypeText(msgRes2, false));
        yield return new WaitUntil(() => skipRequested);
        yield return StartCoroutine(TypeText(msgRes3, false));
        yield return new WaitUntil(() => skipRequested);

        yield return StartCoroutine(TypeText(msgGoBack, false));
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        skipRequested = false;

        yield return new WaitUntil(() => hasReturnedToFlights);

        subtitlePanel.SetActive(true);
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

        DeskTutorialManager.tutorialStep = 5;
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