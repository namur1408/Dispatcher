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
    public float typeSpeed = 0.04f;
    public float msgWaitTime = 4.5f;

    private bool isRadioClicked = false;
    private bool isBookClicked = false;
    private bool isRadarClicked = false;
    private bool skipRequested = false;
    public static int tutorialStep = 0;

    private string msg1 = "Click on the radio to listen to the incoming message";
    private string msg2 = "Welcome to your first shift, Dispatcher! Let me show you around your new workplace!";
    private string msg3 = "The manual and mandatory requirements for today’s shift are in the book located to the left of the radio.\nOpen it to review today’s tasks.";
    private string msg4 = "Excellent. Now it's time to manage the airspace.\nClick on the Radar monitor to open it.";

    void Awake()
    {
        SetAllInteractions(false);
    }

    void Start()
    {
        subtitlePanel.SetActive(false);
        if (radioHighlight) radioHighlight.SetActive(false);
        if (bookHighlight) bookHighlight.SetActive(false);
        if (radarHighlight) radarHighlight.SetActive(false);
        subtitleText.text = "";
        if (tutorialStep == 0)
        {
            StartCoroutine(Part1_RadioAndBook());
        }
        else if (tutorialStep == 1)
        {
            StartCoroutine(Part2_Radar());
        }
        else
        {
            SetAllInteractions(true);
        }
    }
    void SetAllInteractions(bool state)
    {
        if (radioButton) radioButton.interactable = state;
        if (bookTransition) bookTransition.canClick = state;
        if (radarTransition) radarTransition.canClick = state;
        if (tvTransition) tvTransition.canClick = state;
    }

    IEnumerator Part1_RadioAndBook()
    {
        Time.timeScale = 0f;
        if (radioButton) radioButton.interactable = true;
        if (radioHighlight) radioHighlight.SetActive(true);
        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg1));

        yield return new WaitUntil(() => isRadioClicked);

        if (radioHighlight) radioHighlight.SetActive(false);
        if (radioButton) radioButton.interactable = false;
        subtitleText.text = "";

        yield return StartCoroutine(TypeText(msg2));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));

        subtitlePanel.SetActive(false);
        yield return new WaitForSecondsRealtime(1f);
        if (bookTransition) bookTransition.canClick = true;
        if (bookHighlight) bookHighlight.SetActive(true);
        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg3));

        yield return new WaitUntil(() => isBookClicked);
        subtitlePanel.SetActive(false);
        if (bookHighlight) bookHighlight.SetActive(false);

        tutorialStep = 1;
        Time.timeScale = 1f;
    }

    IEnumerator Part2_Radar()
    {
        SetAllInteractions(false);
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;
        if (radarTransition) radarTransition.canClick = true;
        if (radarHighlight) radarHighlight.SetActive(true);
        subtitlePanel.SetActive(true);

        yield return StartCoroutine(TypeText(msg4));

        yield return new WaitUntil(() => isRadarClicked);
        subtitlePanel.SetActive(false);
        if (radarHighlight) radarHighlight.SetActive(false);
        subtitleText.text = "";

        tutorialStep = 2;
        Time.timeScale = 1f;
        SetAllInteractions(true);
    }

    public void PlayerClickedRadio()
    {
        isRadioClicked = true;
    }

    public void PlayerClickedBook()
    {
        isBookClicked = true;
        tutorialStep = 1; 
    }

    public void PlayerClickedRadar()
    {
        isRadarClicked = true;
        tutorialStep = 2; 
    }
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