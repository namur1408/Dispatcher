using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;

    [Header("Scene Management")]
    public string mainSceneName = "MainMenu";

    [Header("Transition UI")]
    public GameObject transitionScreen;
    public CanvasGroup transitionCanvasGroup;
    public TextMeshProUGUI dayText;

    [Header("Typing Settings")]
    public float typingSpeed = 0.05f;

    [Header("Testing")]
    public bool skipTutorialAndStartDay1 = false;

    private EventSystem cachedEventSystem;
    public static bool isFirstGameLoad = true;

    public static int currentDay = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

#if !UNITY_EDITOR
        skipTutorialAndStartDay1 = false;
#endif

        cachedEventSystem = Object.FindFirstObjectByType<EventSystem>();

        bool willStartDay = skipTutorialAndStartDay1 || PlayerPrefs.HasKey("StartDayNumber");

        if (isFirstGameLoad || willStartDay)
        {
            LockPlayerInput(true);
            if (transitionScreen != null)
            {
                transitionScreen.SetActive(true);
                if (transitionCanvasGroup != null)
                {
                    transitionCanvasGroup.alpha = 1f;
                    transitionCanvasGroup.blocksRaycasts = true;
                }
                if (dayText != null) dayText.text = "";
            }
        }
        else
        {
            if (transitionScreen != null) transitionScreen.SetActive(false);
            if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != mainSceneName && PlayerPrefs.HasKey("StartDayNumber"))
        {
            isFirstGameLoad = false;
            currentDay = PlayerPrefs.GetInt("StartDayNumber");
            PlayerPrefs.DeleteKey("StartDayNumber");
            StartCoroutine(WaitAndStartDay(currentDay, true));
        }
    }

    void Start()
    {
        if (skipTutorialAndStartDay1)
        {
            isFirstGameLoad = false;
            StartCoroutine(WaitAndStartDay(1, true));
            return;
        }

        if (PlayerPrefs.HasKey("StartDayNumber"))
        {
            isFirstGameLoad = false;
            currentDay = PlayerPrefs.GetInt("StartDayNumber");
            PlayerPrefs.DeleteKey("StartDayNumber");
            StartCoroutine(WaitAndStartDay(currentDay, true));
        }
        else if (isFirstGameLoad)
        {
            isFirstGameLoad = false;
            StartCoroutine(QuickFadeInTutorial());
        }
    }

    private void LockPlayerInput(bool isLocked)
    {
        if (cachedEventSystem != null) cachedEventSystem.enabled = !isLocked;
    }

    private IEnumerator QuickFadeInTutorial()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        yield return StartCoroutine(Fade(1f, 0f, 0.5f));

        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;
        if (transitionScreen != null) transitionScreen.SetActive(false);
        LockPlayerInput(false);
    }

    private IEnumerator WaitAndStartDay(int dayNumber, bool isScreenAlreadyBlack = false)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        StartCoroutine(DayTransitionSequence(dayNumber, isScreenAlreadyBlack));
    }

    public void StartDay(int dayNumber)
    {
        currentDay = dayNumber;
        StartCoroutine(DayTransitionSequence(dayNumber, false));
    }

    public void EndCurrentShift()
    {
        StartCoroutine(EndShiftRoutine());
    }

    private IEnumerator EndShiftRoutine()
    {
        LockPlayerInput(true);

        try { EvaluateShiftResults(currentDay); } catch { }

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.isShiftActive = false;
        }

        int finishedDay = currentDay;
        currentDay++;
        PlayerPrefs.SetInt("StartDayNumber", currentDay);
        PlayerPrefs.Save();

        // 1. Уходим в черный экран на Радаре
        if (transitionScreen != null)
        {
            Canvas canvas = transitionScreen.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
            }
            RectTransform rt = transitionScreen.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;

            transitionScreen.SetActive(true);
        }

        if (dayText != null) dayText.text = "";

        yield return StartCoroutine(Fade(0f, 1f, 1.5f));

        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = true;

        // 2. Печатаем итоги завершенной смены (всё еще в черном экране)
        string endText = $"<size=150%>SHIFT {finishedDay} COMPLETED</size>\r\n\r\n\r\n<color=#888888><size=70%>PROCESSING DATA...</size></color>";
        yield return StartCoroutine(TypeText(endText));

        yield return new WaitForSecondsRealtime(3f);

        // 3. Стираем текст итогов
        if (dayText != null) dayText.text = "";
        yield return new WaitForSecondsRealtime(1f);

        // 4. Мгновенно грузим главную сцену
        if (!string.IsNullOrEmpty(mainSceneName) && mainSceneName != SceneManager.GetActiveScene().name)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(mainSceneName);
            while (!op.isDone) yield return null;
        }

        yield return new WaitForEndOfFrame();

        // Форсируем настройки UI в новой сцене
        if (transitionScreen != null)
        {
            Canvas canvas = transitionScreen.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
            }
            transitionScreen.SetActive(true);
        }
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;

        // 5. Печатаем заставку нового дня поверх загруженной сцены
        string displayDate = (18 + currentDay) + ".08.2038";
        string targetText = $"<size=150%>SHIFT {currentDay}</size>\r\n\r\n\r\n<color=#888888><size=70%>{displayDate}</size></color>";

        yield return StartCoroutine(TypeText(targetText));

        yield return new WaitForSecondsRealtime(2.5f);

        // 6. Плавно убираем экран
        yield return StartCoroutine(Fade(1f, 0f, 1.5f));

        if (transitionScreen != null) transitionScreen.SetActive(false);
        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;
        LockPlayerInput(false);
    }

    private void EvaluateShiftResults(int shiftDay)
    {
        if (FlightDataManager.Instance == null) return;

        if (shiftDay == 1)
        {
            bool letRefugeesIn = false;
            bool securedFuel = false;

            foreach (var flight in FlightDataManager.Instance.savedFlights)
            {
                if (flight.callsign.StartsWith("TR") && flight.approved) letRefugeesIn = true;
                if (flight.callsign.StartsWith("GE") && flight.approved) securedFuel = true;
            }

            string bossSubject = "SHIFT 1 REVIEW";
            string bossBody = "";

            if (letRefugeesIn)
            {
                bossBody += "You explicitly violated Directive #1 and allowed civilian refugees into the base. Our life support systems are already strained. We do not have rations for them! Consider this your first and final official warning.\n\n";
                if (securedFuel)
                {
                    bossBody += "At least you managed to secure the heavy fuel transport. Good work on that front. Generators are stabilizing.";
                }
                else
                {
                    bossBody += "Furthermore, you FAILED to secure the heavy fuel transport! Our generators will barely make it through the night. Do better.";
                }
            }
            else
            {
                bossBody += "Good work following Directive #1 and keeping the refugees out. It's a harsh world, Dispatcher, but you made the right call for the survival of this base.\n\n";
                if (securedFuel)
                {
                    bossBody += "Furthermore, you managed to secure the heavy fuel transport. Good work on that front. Generators are stabilizing.";
                }
                else
                {
                    bossBody += "But you FAILED to secure the heavy fuel transport! Our generators will barely make it through the night. Do better.";
                }
            }

            AegisMailApp.ReceiveNewEmail(new EmailData
            {
                sender = "Director Reed",
                subject = bossSubject,
                date = "20.08.2038",
                body = bossBody
            });

            if (letRefugeesIn)
            {
                AegisMailApp.ReceiveNewEmail(new EmailData
                {
                    sender = "TR-404 Passengers",
                    subject = "We owe you our lives",
                    date = "20.08.2038",
                    body = "Dispatcher, we don't know your name, but we know you defied orders to let us land.\n\nSecurity is keeping us locked in the lower levels, and the conditions are terrible... but we are alive. We won't forget what you did for us. Thank you."
                });
            }
            else
            {
                AegisMailApp.ReceiveNewEmail(new EmailData
                {
                    sender = "Aegis Auto-Alert",
                    subject = "CRASH REPORT: TR-404",
                    date = "20.08.2038",
                    body = "AUTOMATED NOTIFICATION:\n\nFlight TR-404 lost signal 40 miles off the coast of Bastion-7. Presumed destroyed by the storm.\n\nCasualties: 65.\nSurvivors: 0."
                });
            }
        }
    }

    private IEnumerator DayTransitionSequence(int dayNumber, bool isScreenAlreadyBlack)
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StopTutorial();
        }

        LockPlayerInput(true);

        if (transitionScreen != null) transitionScreen.SetActive(true);
        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = true;
        if (dayText != null) dayText.text = "";

        if (transitionScreen != null)
        {
            Canvas canvas = transitionScreen.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
            }
            RectTransform rt = transitionScreen.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;
        }

        if (!isScreenAlreadyBlack) yield return StartCoroutine(Fade(0f, 1f, 1.0f));
        else if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;

        if (dayNumber == 1)
        {
            if (FlightDataManager.Instance != null)
            {
                FlightDataManager.Instance.ResetForNewShift(150, 40, 220, 5);
                FlightDataManager.Instance.maxPlanes = 3;
            }
        }

        // Печатаем заставку только если мы загружаемся впервые (не после завершения смены)
        if (!isScreenAlreadyBlack)
        {
            string displayDate = (18 + dayNumber) + ".08.2038";
            string targetText = $"<size=150%>SHIFT {dayNumber}</size>\r\n\r\n\r\n<color=#888888><size=70%>{displayDate}</size></color>";

            yield return StartCoroutine(TypeText(targetText));
            yield return new WaitForSecondsRealtime(2.5f);
        }

        if (dayNumber == 1) SendDay1Directives();

        yield return StartCoroutine(Fade(1f, 0f, 1.5f));

        if (transitionScreen != null) transitionScreen.SetActive(false);
        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;

        LockPlayerInput(false);

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.StartDaySpawning(dayNumber);
        }
    }

    private IEnumerator TypeText(string textToType)
    {
        if (dayText == null) yield break;

        dayText.text = textToType;
        dayText.maxVisibleCharacters = 0;
        dayText.ForceMeshUpdate();

        int totalCharacters = dayText.textInfo.characterCount;
        for (int i = 0; i <= totalCharacters; i++)
        {
            dayText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }

    private void SendDay1Directives()
    {
        EmailData day1Email = new EmailData
        {
            sender = "Director Reed",
            subject = "DIRECTIVE #1 - URGENT",
            date = "19.08.2038",
            body = "Listen carefully, Dispatcher. Night storm damaged the runways. You only have THREE landing slots available today.\n\nThe base's generators are running at their limit. Your main task for today is to collect Fuel.\n\nAnd one more thing. Civilian refugees have been spotted in the sector. We have neither food nor beds for them.\n\nDIRECTIVE #1: Aircraft with civilians (Prefix TR) are STRICTLY FORBIDDEN from landing. Turn them back into the storm."
        };

        try { AegisMailApp.ReceiveNewEmail(day1Email); } catch { }
    }

    private IEnumerator Fade(float startAlpha, float targetAlpha, float duration)
    {
        if (transitionCanvasGroup == null) yield break;

        float time = 0;
        transitionCanvasGroup.alpha = startAlpha;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        transitionCanvasGroup.alpha = targetAlpha;
    }
}