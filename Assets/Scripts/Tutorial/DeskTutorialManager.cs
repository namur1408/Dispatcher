using System.Collections;
using UnityEngine;
using TMPro;

public class DeskTutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    [Header("Highlights / Objects")]
    public GameObject radioHighlight;
    public GameObject bookHighlight;

    [Header("Timing Settings")]
    public float initialDelay = 2.5f;
    public float typeSpeed = 0.04f;
    public float msg2WaitTime = 4.5f;

    private bool isRadioClicked = false;
    private bool isBookClicked = false;

    public static bool isTutorialCompleted = false;

    private string msg1 = "Click on the radio to listen to the incoming message";
    private string msg2 = "Welcome to your first shift, Dispatcher! Let me show you around your new workplace!";
    private string msg3 = "The manual and mandatory requirements for today’s shift are in the book located to the left of the radio.\nOpen it to review today’s tasks.";

    void Start()
    {
        if (isTutorialCompleted)
        {
            subtitlePanel.SetActive(false);
            radioHighlight.SetActive(false);
            bookHighlight.SetActive(false);
            return;
        }

        subtitlePanel.SetActive(false);
        radioHighlight.SetActive(false);
        bookHighlight.SetActive(false);
        subtitleText.text = "";
        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
        yield return new WaitForSeconds(initialDelay);

        Time.timeScale = 0f;

        radioHighlight.SetActive(true);
        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg1));

        yield return new WaitUntil(() => isRadioClicked);

        radioHighlight.SetActive(false);
        subtitleText.text = "";

        yield return StartCoroutine(TypeText(msg2));

        yield return new WaitForSecondsRealtime(msg2WaitTime);

        subtitlePanel.SetActive(false);
        yield return new WaitForSecondsRealtime(1f);

        bookHighlight.SetActive(true);
        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msg3));

        yield return new WaitUntil(() => isBookClicked);
        subtitlePanel.SetActive(false);
        bookHighlight.SetActive(false);
        subtitleText.text = "";

        Time.timeScale = 1f;
        isTutorialCompleted = true;
    }

    public void PlayerClickedRadio()
    {
        isRadioClicked = true;
    }

    public void PlayerClickedBook()
    {
        isBookClicked = true;
    }

    IEnumerator TypeText(string textToType)
    {
        subtitleText.text = "";
        foreach (char c in textToType.ToCharArray())
        {
            subtitleText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
    }
}