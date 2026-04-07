using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeskTutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    [Header("Highlights / Objects")]
    public GameObject radioHighlight;
    public GameObject bookHighlight;
    public GameObject radarHighlight;

    [Header("Interactions (Transitions & Buttons)")]
    public Button radioButton;
    public ZoomTransition bookTransition;
    public ZoomTransition radarTransition;
    public ZoomTransition tvTransition;

    [Header("Timing Settings")]
    public float typeSpeed   = 0.04f;
    public float msgWaitTime = 4.5f;

    private bool isRadioClicked  = false;
    private bool isBookClicked   = false;
    private bool isRadarClicked  = false;
    private bool skipRequested   = false;

    public static int tutorialStep = 0;

    private string msg1 = "Click on the radio to listen to the incoming message";
    private string msg2 = "Welcome to your first shift, Dispatcher! Let me show you around your new workplace!";
    private string msg3 = "The manual and mandatory requirements for today's shift are in the book located to the left of the radio.\nOpen it to review today's tasks.";
    private string msg4 = "Excellent. Now it's time to manage the airspace.\nClick on the Radar monitor to open it.";

    void Awake()
    {
        SetAllInteractions(false);
    }

    void Start()
    {
        // FIX 1: Guard against unassigned references that would throw NullReferenceException
        //        if the designer forgot to wire them up in the Inspector.
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (radioHighlight != null) radioHighlight.SetActive(false);
        if (bookHighlight  != null) bookHighlight.SetActive(false);
        if (radarHighlight != null) radarHighlight.SetActive(false);
        if (subtitleText   != null) subtitleText.text = "";

        if (tutorialStep == 0)
            StartCoroutine(Part1_RadioAndBook());
        else if (tutorialStep == 1)
            StartCoroutine(Part2_Radar());
        else
            SetAllInteractions(true);
    }

    void SetAllInteractions(bool state)
    {
        if (radioButton    != null) radioButton.interactable    = state;
        if (bookTransition != null) bookTransition.canClick     = state;
        if (radarTransition!= null) radarTransition.canClick    = state;
        if (tvTransition   != null) tvTransition.canClick       = state;
    }

    IEnumerator Part1_RadioAndBook()
    {
        // Pause the game while we display the tutorial dialogue.
        // All WaitForSecondsRealtime / WaitUntil below use unscaled time, so they
        // work correctly even when timeScale = 0.
        Time.timeScale = 0f;

        // Step 1: Prompt player to click the radio
        if (radioButton    != null) radioButton.interactable = true;
        if (radioHighlight != null) radioHighlight.SetActive(true);
        if (subtitlePanel  != null) subtitlePanel.SetActive(true);

        yield return StartCoroutine(TypeText(msg1));
        yield return new WaitUntil(() => isRadioClicked);

        if (radioHighlight != null) radioHighlight.SetActive(false);
        if (radioButton    != null) radioButton.interactable = false;
        if (subtitleText   != null) subtitleText.text = "";

        yield return StartCoroutine(TypeText(msg2));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));
        if (subtitlePanel != null) subtitlePanel.SetActive(false);

        yield return new WaitForSecondsRealtime(1f);

        // Step 2: Prompt player to open the book
        if (bookTransition != null) bookTransition.canClick = true;
        if (bookHighlight  != null) bookHighlight.SetActive(true);
        if (subtitlePanel  != null) subtitlePanel.SetActive(true);

        yield return StartCoroutine(TypeText(msg3));
        yield return new WaitUntil(() => isBookClicked);

        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (bookHighlight != null) bookHighlight.SetActive(false);

        tutorialStep = 1;
        Time.timeScale = 1f;
    }

    IEnumerator Part2_Radar()
    {
        SetAllInteractions(false);

        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;

        // Step 3: Prompt player to open the radar
        if (radarTransition != null) radarTransition.canClick = true;
        if (radarHighlight  != null) radarHighlight.SetActive(true);
        if (subtitlePanel   != null) subtitlePanel.SetActive(true);

        yield return StartCoroutine(TypeText(msg4));
        yield return new WaitUntil(() => isRadarClicked);

        if (subtitlePanel  != null) subtitlePanel.SetActive(false);
        if (radarHighlight != null) radarHighlight.SetActive(false);
        if (subtitleText   != null) subtitleText.text = "";

        tutorialStep = 2;
        Time.timeScale = 1f;
        SetAllInteractions(true);
    }

    // --- Public callbacks wired to UI buttons/events ---
    public void PlayerClickedRadio() { isRadioClicked = true; }
    public void PlayerClickedBook()  { isBookClicked  = true; }
    public void PlayerClickedRadar() { isRadarClicked = true; }
    public void OnDialogueClicked()  { skipRequested  = true; }

    // --- Helpers ---
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

        skipRequested      = false;
        subtitleText.text  = "";

        foreach (char c in textToType.ToCharArray())
        {
            if (skipRequested)
            {
                subtitleText.text = textToType;
                break;
            }
            subtitleText.text += c;
            // WaitForSecondsRealtime works correctly when timeScale = 0
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        skipRequested = false;
    }
}
