using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadarTutorialManager : MonoBehaviour
{
    public static RadarTutorialManager Instance;

    [Header("UI References")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;
    public Button returnButton;
    public Transform radarContentTransform;

    [Header("Mentor Settings")]
    public Color mentorNormalColor = Color.green;
    public Color mentorAngryColor = Color.red;
    public float shakeMagnitude = 5f;
    public float zoomThreshold = 1.3f;

    [Header("Timing Settings")]
    public float typeSpeed = 0.04f;
    public float msgWaitTime = 4f;

    public static bool didFirstPlanesCrash = false;
    private bool skipRequested = false;
    private bool isEmergencyMessageActive = false;
    public static bool isRadarTutorialCompleted = false;

    private string msgIntro = "Welcome to the Radar screen. This is your main tool for managing the airspace.";
    private string msgPlanesSpawned = "Attention! Two flights have just entered your sector. Do you see them?";
    private string msgZoomTutorial = "It's hard to read their data from this height. Zoom in until you can see their callsigns.";

    private string msgStep1 = "Good. now click a plane to SELECT it and reveal its flight path. Click again to DESELECT.";
    private string msgStep2 = "Click anywhere on the radar to create a WAYPOINT. And most importantly, if flight paths cross, planes will COLLIDE!";
    private string msgStep3 = "Need to fix a route? Click an existing waypoint to REMOVE it.";

    private string msgDanger = "Stop messing around! Icons turned ORANGE! A collision is imminent!";
    private string msgEmergency = "You have seconds to act! Divert one of the planes NOW!";
    private string msgSuccess = "Crisis averted. Safe distance maintained.";
    private string msgProactiveSuccess = "Excellent foresight, Dispatcher. You diverted them early. Good job.";

    private string msgFinalStep1 = "A new flight appeared. SELECT it and check the terminal on the left.";
    private string msgFinalStep2 = "Status is 'PENDING', prefix is 'KO'. Remember the rules in your book?";
    private string msgFinalStep3 = "'KO' flights are strictly FORBIDDEN today. They cannot land.";
    private string msgFinalTask = "Click 'Return', go back to your desk, and DENY their entry.";

    private string[] angryResponses = {
        "<size=120%>WHAT HAVE YOU DONE?!</size>\nYou just let two planes collide! This is gross negligence!",
        "<size=120%>DISASTER!</size>\nYou were supposed to separate them, not help them meet in mid-air!"
    };

    private Vector2 originalTextPos;

    void Awake() { Instance = this; }

    void Start()
    {
        if (subtitleText != null)
            originalTextPos = subtitleText.rectTransform.anchoredPosition;

        if (isRadarTutorialCompleted)
        {
            if (returnButton != null) returnButton.interactable = true;
            subtitlePanel.SetActive(false);
            return;
        }

        if (returnButton != null) returnButton.interactable = false;
        subtitlePanel.SetActive(false);

        if (DeskTutorialManager.tutorialStep == 5)
        {
            StartCoroutine(HoldingPatternTutorial());
        }
        else
        {
            StartCoroutine(RadarTutorialSequence());
        }
    }

    public void NotifyEmergencyCollision()
    {
        if (isEmergencyMessageActive || isRadarTutorialCompleted) return;

        didFirstPlanesCrash = true;
        StartCoroutine(ShowAngryEmergencyPopup());
    }

    IEnumerator ShowAngryEmergencyPopup()
    {
        isEmergencyMessageActive = true;
        string randomAngryMsg = angryResponses[Random.Range(0, angryResponses.Length)];
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorAngryColor;
        skipRequested = false;
        yield return StartCoroutine(TypeText(randomAngryMsg, true));
        yield return new WaitUntil(() => skipRequested);
        subtitlePanel.SetActive(false);
        Time.timeScale = oldTimeScale;
        isEmergencyMessageActive = false;
    }

    IEnumerator RadarTutorialSequence()
    {
        yield return new WaitForSeconds(1f);
        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;
        yield return StartCoroutine(TypeText(msgIntro, false));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));
        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        yield return new WaitUntil(() => TutorialManager.tutorialStep == 1);
        yield return new WaitForSeconds(4f);

        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);
        yield return StartCoroutine(TypeText(msgPlanesSpawned, false));
        yield return StartCoroutine(WaitWithSkip(3f));

        yield return StartCoroutine(TypeText(msgZoomTutorial, false));
        yield return new WaitUntil(() => radarContentTransform != null && radarContentTransform.localScale.x >= zoomThreshold);

        yield return StartCoroutine(TypeText(msgStep1, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);
        yield return StartCoroutine(TypeText(msgStep2, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);
        yield return StartCoroutine(TypeText(msgStep3, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        bool dangerTriggered = false;
        bool proactiveSuccess = false;

        while (true)
        {
            List<UIAirplane> currentPlanes = GetActiveTutorialPlanes();

            if (currentPlanes.Count < 2) break; 

            float dist = Vector2.Distance(currentPlanes[0].GetLogicalPosition(), currentPlanes[1].GetLogicalPosition());

            if (dist > 450f)
            {
                proactiveSuccess = true;
                break;
            }

            Color orangeDangerColor = new Color(1f, 0.5f, 0f);
            if (currentPlanes[0].callsignText.color == orangeDangerColor ||
                currentPlanes[1].callsignText.color == orangeDangerColor)
            {
                dangerTriggered = true;
                break;
            }

            yield return null;
        }

        if (proactiveSuccess)
        {
            Time.timeScale = 0f;
            subtitlePanel.SetActive(true);
            subtitleText.color = mentorNormalColor;
            yield return StartCoroutine(TypeText(msgProactiveSuccess, false));
            yield return StartCoroutine(WaitWithSkip(msgWaitTime));
            subtitlePanel.SetActive(false);
            Time.timeScale = 1f;
        }
        else if (dangerTriggered)
        {
            List<UIAirplane> alive = GetActiveTutorialPlanes();
            if (alive.Count >= 2)
            {
                Time.timeScale = 0f;
                subtitlePanel.SetActive(true);
                subtitleText.color = mentorAngryColor;
                yield return StartCoroutine(TypeText(msgDanger, true));
                yield return StartCoroutine(WaitWithSkip(msgWaitTime));
                yield return StartCoroutine(TypeText(msgEmergency, true));
                skipRequested = false;
                yield return new WaitUntil(() => skipRequested);
                subtitlePanel.SetActive(false);
                Time.timeScale = 1f;

                while (true)
                {
                    List<UIAirplane> checking = GetActiveTutorialPlanes();
                    if (checking.Count < 2)
                    {
                        break;
                    }

                    if (Vector2.Distance(checking[0].GetLogicalPosition(), checking[1].GetLogicalPosition()) > 280f)
                    {
                        Time.timeScale = 0f;
                        subtitlePanel.SetActive(true);
                        subtitleText.color = mentorNormalColor;
                        yield return StartCoroutine(TypeText(msgSuccess, false));
                        yield return StartCoroutine(WaitWithSkip(msgWaitTime));
                        subtitlePanel.SetActive(false);
                        Time.timeScale = 1f;
                        break;
                    }
                    yield return null;
                }
            }
        }

        yield return new WaitUntil(() => TutorialManager.tutorialStep == 2);

        yield return new WaitUntil(() =>
        {
            UIAirplane[] allPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
            foreach (var p in allPlanes)
            {
                if (p != null && p.callsignText != null && p.callsignText.text == "KO-677")
                {
                    CanvasGroup cg = p.GetComponent<CanvasGroup>();
                    if (cg != null && cg.alpha > 0.1f) return true;
                }
            }
            return false;
        });

        yield return new WaitForSeconds(0.5f);
        Time.timeScale = 1f;
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;
        yield return StartCoroutine(TypeText(msgFinalStep1, false));

        yield return new WaitUntil(() =>
            RadarScreenClicker.selectedPlane != null &&
            RadarScreenClicker.selectedPlane.callsignText.text == "KO-677"
        );

        yield return new WaitForSeconds(3f);
        Time.timeScale = 0f;

        yield return StartCoroutine(TypeText(msgFinalStep2, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        yield return StartCoroutine(TypeText(msgFinalStep3, false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        yield return StartCoroutine(TypeText(msgFinalTask, false));

        if (returnButton != null) returnButton.interactable = true;
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
    }

    IEnumerator HoldingPatternTutorial()
    {
        yield return new WaitForSeconds(1f);
        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;

        yield return StartCoroutine(TypeText("Planes reaching the center without clearance enter a HOLDING PATTERN.", false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        if (didFirstPlanesCrash)
        {
            subtitleText.color = mentorAngryColor;
            yield return StartCoroutine(TypeText("Since YOU crashed the first ones, I have ordered a new flight to approach. Watch it closely.", false));
            skipRequested = false;
            yield return new WaitUntil(() => skipRequested);

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SpawnSpecificPlane(
                    new Vector2(-600, -600),
                    Vector2.zero,
                    "LX-677",
                    radarContentTransform
                );
            }
        }

        subtitleText.color = mentorNormalColor;
        yield return StartCoroutine(TypeText("They circle until you ALLOW or DENY them. If fuel runs out, they divert.", false));
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        yield return StartCoroutine(TypeText("Your training is officially over. From here on out, you're on your own. Good luck.", false));
        if (returnButton != null) returnButton.interactable = true;
        skipRequested = false;
        yield return new WaitUntil(() => skipRequested);

        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;
        isRadarTutorialCompleted = true;
    }

    List<UIAirplane> GetActiveTutorialPlanes()
    {
        UIAirplane[] allPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        List<UIAirplane> result = new List<UIAirplane>();
        foreach (var p in allPlanes)
        {
            if (p != null && p.callsignText != null && (p.callsignText.text == "GE-672" || p.callsignText.text == "QY-467"))
                result.Add(p);
        }
        return result;
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
            if (shake)
            {
                rt.anchoredPosition = originalTextPos + new Vector2(Random.Range(-shakeMagnitude, shakeMagnitude), Random.Range(-shakeMagnitude, shakeMagnitude));
            }
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        rt.anchoredPosition = originalTextPos;
        skipRequested = false;
    }
}