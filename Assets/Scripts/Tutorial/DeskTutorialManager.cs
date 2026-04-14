using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeskTutorialManager : MonoBehaviour
{
    public static DeskTutorialManager Instance;

    [Header("UI References")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    [Header("Highlights / Objects")]
    public GameObject radioHighlight;
    public GameObject bookHighlight;
    public GameObject radarHighlight;
    public GameObject tvHighlight;

    [Header("Interactions (Transitions & Buttons)")]
    public Button radioButton;
    public ZoomTransition bookTransition;
    public ZoomTransition radarTransition;
    public ZoomTransition tvTransition;

    [Header("Timing Settings")]
    public float typeSpeed = 0.04f;
    public float msgWaitTime = 4.5f;
    public bool disableTutorialsForTesting = false;

    private bool isRadioClicked = false;
    private bool isBookClicked = false;
    private bool isRadarClicked = false;
    private bool isTvClicked = false;
    private bool skipRequested = false;
    public static int tutorialStep = 0;

    private string msg1 = "Click on the radio to listen to the incoming message.";
    private string msg2 = "Welcome to your first shift, Dispatcher! Let me show you around your new workplace!";
    private string msg3 = "The manual and mandatory requirements for today's shift are in the book located to the left of the radio.\nOpen it to review today's tasks.";
    private string msg4 = "Excellent. Now it's time to manage the airspace.\nClick on the Radar monitor to open it.";
    private string msg5 = "On the left, you'll find a terminal. It plays an important role in your work; use it to view detailed information about the aircraft and to check landing clearances or restrictions. Now go to that terminal.";

    void Awake()
    {
        Instance = this;
        if (disableTutorialsForTesting)
        {
            tutorialStep = 99; 
            RadarTutorialManager.isRadarTutorialCompleted = true; 
            TVTutorialManager.isTvTutorialCompleted = true; 
        }
#if !UNITY_EDITOR
        disableTutorialsForTesting = false;
#endif
    }

    void Start()
    {
        SetAllInteractions(false);
        subtitlePanel.SetActive(false);

        if (radioHighlight) radioHighlight.SetActive(false);
        if (bookHighlight) bookHighlight.SetActive(false);
        if (radarHighlight) radarHighlight.SetActive(false);
        if (tvHighlight) tvHighlight.SetActive(false);
        subtitleText.text = "";

        if (tutorialStep == 0) StartCoroutine(Part1_RadioAndBook());
        else if (tutorialStep == 1) StartCoroutine(Part2_Radar());
        else if (tutorialStep == 2) StartCoroutine(Part3_TV());
        else if (tutorialStep == 4) StartCoroutine(Part4_BackToRadar());
        else SetAllInteractions(true);
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
        if (radioHighlight) radioHighlight.SetActive(true);
        if (radioButton) radioButton.interactable = true;
        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg1));
        yield return new WaitUntil(() => isRadioClicked);

        if (radioHighlight) radioHighlight.SetActive(false);
        if (radioButton) radioButton.interactable = false;
        subtitleText.text = "";

        yield return new WaitUntil(() => isRadioClicked);

        if (radioHighlight) radioHighlight.SetActive(false);
        if (radioButton) radioButton.interactable = false;
        subtitleText.text = "";

        yield return StartCoroutine(TypeText(msg2));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        yield return new WaitForSecondsRealtime(0.5f);

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg3));

        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (bookTransition) bookTransition.canClick = true;
        if (bookHighlight) bookHighlight.SetActive(true);

        yield return new WaitUntil(() => isBookClicked);
    }

    IEnumerator Part2_Radar()
    {
        SetAllInteractions(false);
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg4));

        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (radarTransition) radarTransition.canClick = true;
        if (radarHighlight) radarHighlight.SetActive(true);

        yield return new WaitUntil(() => isRadarClicked);
    }

    IEnumerator Part3_TV()
    {
        SetAllInteractions(false);
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg5));

        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (tvTransition) tvTransition.canClick = true;
        if (tvHighlight) tvHighlight.SetActive(true);

        yield return new WaitUntil(() => isTvClicked);
    }

    IEnumerator Part4_BackToRadar()
    {
        SetAllInteractions(false);
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText("Click on the Radar again. Let's finish this."));

        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (radarTransition) radarTransition.canClick = true;
        if (radarHighlight) radarHighlight.SetActive(true);

        yield return new WaitUntil(() => isRadarClicked);
    }

    public void PlayerClickedRadio()
    {
        isRadioClicked = true;
    }

    public void PlayerClickedBook()
    {
        isBookClicked = true;
        tutorialStep = 1;
        Time.timeScale = 1f;
        subtitlePanel.SetActive(false); 
    }

    public void PlayerClickedRadar()
    {
        isRadarClicked = true;
        if (tutorialStep == 1) tutorialStep = 2;
        else if (tutorialStep == 4) tutorialStep = 5;
        Time.timeScale = 1f;
        subtitlePanel.SetActive(false); 
    }

    public void PlayerClickedTV()
    {
        isTvClicked = true;
        tutorialStep = 3;
        Time.timeScale = 1f;
        subtitlePanel.SetActive(false); 
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