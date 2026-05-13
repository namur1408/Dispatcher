using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RadioTutorialManager : MonoBehaviour
{
    public static RadioTutorialManager Instance;

    [Header("UI References")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;
    public Button endCommsButton;
    public Button confrontButton;
    public Button askButton;

    [Header("Mentor Settings")]
    public Color mentorNormalColor = Color.green;
    public Color mentorAngryColor = Color.red;
    public float typeSpeed = 0.04f;
    public float msgWaitTime = 4f;
    public float shakeMagnitude = 5f;

    private bool skipRequested = false;
    public static bool isRadioTutorialCompleted = false;

    private bool hasClickedFirstDocument = false;
    private bool hasClickedTwoFacts = false;
    private bool hasAskedQuestion = false;
    private bool hasFoundContradiction = false;

    private string msgIntro = "Welcome to the Communications Station. Let's interrogate this pilot.";
    private string msgDocuments = "The Radar Sensor couldn't detect the cargo. Click the yellow 'SENSOR' link to ask the pilot.";
    private string msgAskAbout = "Good! Now click the button on the left to ASK ABOUT CARGO.";
    private string msgQuestionAsked = "The pilot claims they are carrying FOOD. But the Flight Manifest says PEOPLE!";
    private string msgContradiction = "Click on the yellow 'PEOPLE' fact in the Manifest, and the yellow 'FOOD' fact in the Pilot Statement to compare them.";
    private string msgConfront = "They lied! The facts turned RED. Click the CONFRONT button to demand an explanation.";
    private string msgComplete = "Well done! They are smuggling FOOD. During your actual shift, the final decision will still be yours, but for now, this will be enough for us to send them a rejection. Click 'RETURN' to return to your desk";

    private Vector2 originalTextPos;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (subtitleText != null)
            originalTextPos = subtitleText.rectTransform.anchoredPosition;

        if (isRadioTutorialCompleted || DeskTutorialManager.tutorialStep < 1)
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            return;
        }

        if (endCommsButton != null) endCommsButton.interactable = false;
        if (confrontButton != null) confrontButton.interactable = false;
        if (askButton != null) askButton.interactable = false;

        subtitlePanel.SetActive(false);
        StartCoroutine(RadioTutorialSequence());
    }

    IEnumerator RadioTutorialSequence()
    {
        yield return new WaitForSeconds(1f);

        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;

        yield return StartCoroutine(TypeText(msgIntro, false));
        yield return new WaitUntil(() => skipRequested);

        yield return StartCoroutine(TypeText(msgDocuments, false));
        yield return new WaitUntil(() => hasClickedFirstDocument);

        yield return StartCoroutine(TypeText(msgAskAbout, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        if (askButton != null) askButton.interactable = true;

        yield return new WaitUntil(() => hasAskedQuestion);

        yield return new WaitForSeconds(3.5f); 
        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);

        yield return StartCoroutine(TypeText(msgQuestionAsked, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        yield return StartCoroutine(TypeText(msgContradiction, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        yield return new WaitUntil(() => hasFoundContradiction);

        yield return new WaitForSeconds(1f);
        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);

        yield return StartCoroutine(TypeText(msgConfront, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        if (confrontButton != null) confrontButton.interactable = true;

        yield return new WaitUntil(() => confrontButton != null && !confrontButton.gameObject.activeSelf);

        yield return new WaitForSeconds(3.5f); 
        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);

        yield return StartCoroutine(TypeText(msgComplete, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        if (endCommsButton != null) endCommsButton.interactable = true;

        isRadioTutorialCompleted = true;
    }

    public void NotifyDocumentClicked()
    {
        hasClickedFirstDocument = true;
    }

    public void NotifyFactsCompared()
    {
        hasClickedTwoFacts = true;
    }

    public void NotifyQuestionAsked()
    {
        hasAskedQuestion = true;
    }

    public void NotifyContradictionFound()
    {
        hasFoundContradiction = true;
    }

    public void OnDialogueClicked()
    {
        skipRequested = true;
    }

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
            if (skipRequested)
            {
                subtitleText.maxVisibleCharacters = totalVisibleCharacters;
                break;
            }
            subtitleText.maxVisibleCharacters = i;
            if (shake)
            {
                rt.anchoredPosition = originalTextPos + new Vector2(
                    Random.Range(-shakeMagnitude, shakeMagnitude),
                    Random.Range(-shakeMagnitude, shakeMagnitude)
                );
            }
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        rt.anchoredPosition = originalTextPos;
        skipRequested = false;
    }
}
