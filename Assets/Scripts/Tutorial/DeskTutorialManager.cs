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

    [Header("Tutorial Lights")]
    public GameObject[] radioLights;
    public GameObject radarLight;
    public GameObject tvLight;

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

    public static bool tutorialWasSkipped = false;

    private string msg1 = "Click on the radio to listen to the incoming message. You'll use it to interrogate pilots.";
    private string msg2 = "Welcome to your first shift, Dispatcher! Let me show you around your new workplace!";
    private string msgBook = "See that book on the desk? It's your manual. It contains descriptions of all equipment and information about planes. Open it to take a quick look.";
    private string msgRadar = "Excellent. Now it's time to manage the airspace.\nClick on the Radar monitor to open it.";
    private string msgTV = "On the left, you'll see the terminal. It plays a key role in your work: basically, it's a powerful tool for managing landing clearances, resources, and aircraft. Open it now.";

    void Awake()
    {
        Instance = this;

        bool skipFromMenu = PlayerPrefs.GetInt("SkipTutorial", 0) == 1;

        if (disableTutorialsForTesting || skipFromMenu || tutorialWasSkipped)
        {
            tutorialStep = 99;
            RadarTutorialManager.isRadarTutorialCompleted = true;
            TVTutorialManager.isTvTutorialCompleted = true;

            TutorialManager.isTutorialActive = false;

            if (skipFromMenu && !tutorialWasSkipped)
            {
                PlayerPrefs.SetInt("StartDayNumber", 1);
                tutorialWasSkipped = true;
                PlayerPrefs.DeleteKey("SkipTutorial");
            }
        }
        else if (!PlayerPrefs.HasKey("SkipTutorial") && tutorialStep == 0)
        {
            PlayerPrefs.DeleteKey("SkipTutorial");
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

        SetRadioLights(false);
        if (radarLight) radarLight.SetActive(false);
        if (tvLight) tvLight.SetActive(false);

        subtitleText.text = "";

        if (tutorialStep == 0) StartCoroutine(Part1_RadioAndBook());
        else if (tutorialStep == 1) StartCoroutine(Part2_Radar());
        else if (tutorialStep == 2) StartCoroutine(Part2_5_Radio());
        else if (tutorialStep == 3) StartCoroutine(Part3_TV());
        else if (tutorialStep == 5) StartCoroutine(Part4_BackToRadar());
        else SetAllInteractions(true);
    }

    void SetAllInteractions(bool state)
    {
        if (radioButton) radioButton.interactable = state;
        if (bookTransition) bookTransition.canClick = state;
        if (radarTransition) radarTransition.canClick = state;
        if (tvTransition) tvTransition.canClick = state;
    }

    void SetRadioLights(bool state)
    {
        if (radioLights != null)
        {
            foreach (GameObject lightObj in radioLights)
            {
                if (lightObj != null) lightObj.SetActive(state);
            }
        }
    }

    IEnumerator Part1_RadioAndBook()
    {
        Time.timeScale = 0f;

        if (radioHighlight) radioHighlight.SetActive(true);
        SetRadioLights(true);
        if (radioButton) radioButton.interactable = true;

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg1));
        yield return new WaitUntil(() => isRadioClicked);

        if (radioHighlight) radioHighlight.SetActive(false);
        SetRadioLights(false);
        if (radioButton) radioButton.interactable = false;
        subtitleText.text = "";

        yield return StartCoroutine(TypeText(msg2));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        yield return StartCoroutine(TypeText(msgBook));

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
        yield return StartCoroutine(TypeText(msgRadar));

        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (radarTransition) radarTransition.canClick = true;
        if (radarHighlight) radarHighlight.SetActive(true);
        if (radarLight) radarLight.SetActive(true);

        yield return new WaitUntil(() => isRadarClicked);
    }

    IEnumerator Part2_5_Radio()
    {
        SetAllInteractions(false);
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText("Now you need to interrogate the pilot. Click on the blinking radio to open communications."));

        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (radioButton) radioButton.interactable = true;
        if (radioHighlight) radioHighlight.SetActive(true);
        SetRadioLights(true);

        yield return new WaitUntil(() => isRadioClicked);
    }

    IEnumerator Part3_TV()
    {
        SetAllInteractions(false);
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msgTV));

        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (tvTransition) tvTransition.canClick = true;
        if (tvHighlight) tvHighlight.SetActive(true);
        if (tvLight) tvLight.SetActive(true);

        yield return new WaitUntil(() => isTvClicked);
    }

    IEnumerator Part4_BackToRadar()
    {
        SetAllInteractions(false);
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 0f;

        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText("Click on the Radar again. There's just one more thing I need to explain."));

        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (radarTransition) radarTransition.canClick = true;
        if (radarHighlight) radarHighlight.SetActive(true);
        if (radarLight) radarLight.SetActive(true);

        yield return new WaitUntil(() => isRadarClicked);
    }

    public void PlayerClickedRadio()
    {
        isRadioClicked = true;

        // After radio interrogation (step 2), move to TV tutorial (step 3)
        if (tutorialStep == 2)
        {
            tutorialStep = 3;
            Time.timeScale = 1f;
            subtitlePanel.SetActive(false);
            if (radioHighlight) radioHighlight.SetActive(false);
            SetRadioLights(false);
        }
    }

    public void PlayerClickedBook()
    {
        isBookClicked = true;
        tutorialStep = 1;
        Time.timeScale = 1f;
        subtitlePanel.SetActive(false);

        if (bookHighlight) bookHighlight.SetActive(false);
    }

    public void PlayerClickedRadar()
    {
        isRadarClicked = true;

        if (tutorialStep == 1) tutorialStep = 2;
        else if (tutorialStep == 5) tutorialStep = 6;

        Time.timeScale = 1f;
        subtitlePanel.SetActive(false);

        if (radarHighlight) radarHighlight.SetActive(false);
        if (radarLight) radarLight.SetActive(false);
    }

    public void PlayerClickedTV()
    {
        isTvClicked = true;
        tutorialStep = 4;
        Time.timeScale = 1f;
        subtitlePanel.SetActive(false);

        if (tvHighlight) tvHighlight.SetActive(false);
        if (tvLight) tvLight.SetActive(false);
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